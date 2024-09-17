using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

public partial class Universe : Node2D {
  [Export] private Godot.Collections.Array<StarSystemDef> solarSystemDefs;
  [Export] private Godot.Collections.Array<PlanetDef> planetDefs;

  [Export] private PackedScene starSystemScene;
  [Export] private PackedScene edgeScene;
  [Export] private Node edgesContainer;


  private const int STAR_SYSTEMS_PER_SECTOR = 128;
  private const int MIN_DISTANCE_BETWEEN_SECTORS = 200;
  private const int SECTOR_WIDTH = 15360;
  private const int SECTOR_HEIGHT = 8640;

  private const int CENTER_CLUSTER_RADIUS = 400;
  private const int CENTER_CLUSTER_MIN_PLANETS = 4;
  private const int CENTER_CLUSTER_MAX_PLANETS = 5;

  private const int MARGIN_WIDTH = 100;
  private const int MARGIN_HEIGHT = 60;
  private const int SYSTEM_MIN_BORDER_RADIUS = 200;

  // 100 pixels = 1 light year maybe
  // TODO: figure it out!
  private const int STARTING_TRAVEL_DISTANCE_IN_PIXELS = 1000;

  private Dictionary<string, StarSystem> starSystemsById;
  private StarSystemNameGenerator nameGen;

  public override void _Ready() {
    nameGen = new StarSystemNameGenerator();
    starSystemsById = new Dictionary<string, StarSystem>();
    GenerateSector("rafa", new Vector2());
  }

  // TODO: refactor
  public void GenerateSector(string strSeed, Vector2 sectorOffset) {
    int seed = strSeed.GetStableHashCode();
    Random seededRandom = new Random(seed);
    List<StarSystemDef> pickedStarSystems = PickStarSystems(seededRandom);
    //pickedStarSystems.Shuffle(seededRandom);
    List<string> generatedSystemNames = GenerateUniqueNames(seededRandom).ToList();

    // Generate the StarSystems without planets.
    foreach ((StarSystemDef def, string systemName) in pickedStarSystems.Zip(generatedSystemNames)) {
      StarSystem system = starSystemScene.Instantiate() as StarSystem;
      system.Initialize(def, systemName, seededRandom);
      AddChild(system);
      starSystemsById.Add(system.GetId(), system);
    }

    // Place the planets one at a time. We avoid overlaps by just checking every system we add
    // against all previously added systems. Not at big deal a O(100), all other options are not
    // worth it.
    List<Rectangle> placedAreas = new List<Rectangle>();
    Godot.Collections.Array<Vector2> randomPlacements = new Godot.Collections.Array<Vector2>();
    foreach (StarSystem system in starSystemsById.Values) {
      while (true) {
        bool validPlacement = true;
        Vector2I randomPlacement = new Vector2I(
          seededRandom.Next(MARGIN_WIDTH, SECTOR_WIDTH - MARGIN_WIDTH),
          seededRandom.Next(MARGIN_HEIGHT, SECTOR_HEIGHT - MARGIN_HEIGHT));
        foreach (Rectangle rectangle in placedAreas) {
          if (rectangle.Contains(randomPlacement.X, randomPlacement.Y)) {
            validPlacement = false;
          }
        }

        if (validPlacement) {
          placedAreas.Add(
            new Rectangle(
              randomPlacement.X - SYSTEM_MIN_BORDER_RADIUS,
              randomPlacement.Y - SYSTEM_MIN_BORDER_RADIUS,
              SYSTEM_MIN_BORDER_RADIUS * 2,
              SYSTEM_MIN_BORDER_RADIUS * 2));
          system.PlaceSolarSystem(randomPlacement);
          break;
        }
      }
    }

    // Connect the galaxies with a MST which will indicate possible starting paths. In the
    // process, also compute the largest cluster which will be marked as the starting system
    List<Edge<StarSystem>> systemEdges =
      Edge<StarSystem>.GenerateMinimumSpanningTree(starSystemsById.Values);
    foreach (Edge<StarSystem> edge in systemEdges) {
      Line2D line2D = edgeScene.Instantiate() as Line2D;
      line2D.AddPoint(edge.src.Position);
      line2D.AddPoint(edge.dst.Position);
      edgesContainer.AddChild(line2D);
    }
  }

  private List<StarSystemDef> PickStarSystems(Random seededRandom) {
    List<StarSystemDef> pickedStarSystems = new List<StarSystemDef>(STAR_SYSTEMS_PER_SECTOR);
    List<StarSystemDef> sortedSystemByProbabilityAsc =
      solarSystemDefs.OrderBy(def => def.probability).ToList();

    int numStarSystemsPicked = 0;

    // First, create the minimum number of each star system as specified by the resource.
    // Unlikely, but it's possible this gets us over the STAR_SYSTEMS_PER_SECTOR. Not a big deal.
    foreach (StarSystemDef def in sortedSystemByProbabilityAsc) {
      for (int i = 0; i < def.minNumberToGenerate; i++) {
        pickedStarSystems.Add(def);
        numStarSystemsPicked++;
      }
    }

    // Then, while we don't have the total number of planets, generate a random number and
    // incrementally iterate system types (starting from low probability ones) to see if
    // we generated the rarest planets first.
    while (numStarSystemsPicked < STAR_SYSTEMS_PER_SECTOR) {
      float randomThreshold = seededRandom.NextSingle();
      float cumulativeThreshold = 0f;

      StarSystemDef pickedDef = null;
      foreach (StarSystemDef def in sortedSystemByProbabilityAsc) {
        if (cumulativeThreshold + def.probability > randomThreshold) {
          pickedDef = def;
          continue;
        }
        cumulativeThreshold += def.probability;
      }
      // If somehow we got here without a pick, pick the highest prob one.
      if (pickedDef == null) {
        pickedDef = sortedSystemByProbabilityAsc.Last();
      }
      pickedStarSystems.Add(pickedDef);
      numStarSystemsPicked++;
    }
    return pickedStarSystems;
  }

  private HashSet<string> GenerateUniqueNames(Random seededRandom) {
    HashSet<string> starSystemNames = new HashSet<string>(STAR_SYSTEMS_PER_SECTOR);
    while (starSystemNames.Count < STAR_SYSTEMS_PER_SECTOR) {
      starSystemNames.Add(nameGen.GenerateName(seededRandom));
    }
    return starSystemNames;
  }

  private void PopulatePlanets(StarSystem startingSystem, Random seededRandom) {

  }

  // TODO: move elsewhere
  private class Edge<T> where T : Node2D {
    public T src;
    public T dst;

    private Edge(T src, T dst) {
      this.src = src;
      this.dst = dst;
    }

    public static List<Edge<T>> GenerateMinimumSpanningTree(IEnumerable<T> vertices) {
      if (vertices.Count() <= 1) {
        return new List<Edge<T>>();
      }
      List<Edge<T>> result = new List<Edge<T>>();
      PriorityQueue<Edge<T>, float> edgeQueue = new PriorityQueue<Edge<T>, float>();
      HashSet<T> connectedVertices = new HashSet<T>();
      T thisVertex = vertices.First();

      while (connectedVertices.Count() < vertices.Count()) {
        foreach (T thatVertex in vertices) {
          if (thatVertex == thisVertex || connectedVertices.Contains(thatVertex)) {
            continue;
          }
          float distance = (thisVertex.Position - thatVertex.Position).LengthSquared();
          edgeQueue.Enqueue(new Edge<T>(thisVertex, thatVertex), distance);
        }

        while (edgeQueue.Count > 0) {
          Edge<T> nextEdge = edgeQueue.Dequeue();
          if (connectedVertices.Contains(nextEdge.dst)) {
            continue;
          }
          result.Add(nextEdge);
          connectedVertices.Add(nextEdge.dst);
          thisVertex = nextEdge.dst;
          break;
        }
      }
      return result;
    }
  }
}
