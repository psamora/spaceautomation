using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class BeltManager : Node {
  private const int TRANSPORT_LINE_MAX_SIZE = 20;

  [Export] private PackedScene beltScene;
  [Export] private PackedScene inserterScene;
  [Export] private PackedScene spawnerBoxScene;
  [Export] private TileMap tileMap;

  private Dictionary<(int, int), PlacedEntity> placedEntitiesGrid;
  private HashSet<TransportLine> transportLines;

  private Dictionary<TempItem, MultiMeshInstance2D> itemToMultiMeshDict =
    new Dictionary<TempItem, MultiMeshInstance2D>();

  private Dictionary<TempItem, (int, int)> itemAndPositionToPaint =
    new Dictionary<TempItem, (int, int)>();

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    ProcessPriority = 10;
    placedEntitiesGrid = new Dictionary<(int, int), PlacedEntity>();
    transportLines = new HashSet<TransportLine>();
    itemToMultiMeshDict.Add(TempItem.COAL, GetNode<MultiMeshInstance2D>("CoalMultiMesh"));
    itemToMultiMeshDict[TempItem.COAL].Multimesh.Mesh =
      GetNode<MeshInstance2D>("CoalOre").Mesh;
    itemToMultiMeshDict.Add(TempItem.COPPER, GetNode<MultiMeshInstance2D>("CopperMultiMesh"));
    itemToMultiMeshDict[TempItem.COPPER].Multimesh.Mesh =
      GetNode<MeshInstance2D>("CopperOre").Mesh;
    itemToMultiMeshDict.Add(TempItem.IRON, GetNode<MultiMeshInstance2D>("IronMultiMesh"));
    itemToMultiMeshDict[TempItem.IRON].Multimesh.Mesh =
      GetNode<MeshInstance2D>("IronOre").Mesh;
  }

  private int frameCount = 0;
  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
    // TODO: This loop likely doesn't have to run every tick, maybe once a second or minute
    MergeAndCleanUpTransportLines();

    // TODO: this loop has to run based on belt speed, whole other beast
    if (frameCount % 4 == 0) {
      TryToOutputTransportLines();
      AdvanceTransportLines();
    }

    PaintDebugTransportLines();
    frameCount++;
  }

  private void MergeAndCleanUpTransportLines() {
    HashSet<TransportLine> linesToCleanUp = new HashSet<TransportLine>();
    foreach (TransportLine transportLine in transportLines) {
      if (transportLine.IsEmpty()) {
        linesToCleanUp.Add(transportLine);
        continue;
      }
      if (transportLine.Size() > TRANSPORT_LINE_MAX_SIZE) {
        continue;
      }

      // TODO: improve namings below
      Belt lastNodeInLine = transportLine.GetLastBeltInLine();
      Belt transportLineFacingBelt =
        GetNeighboringEntities(lastNodeInLine).GetFacingEntity(lastNodeInLine.placedDirection);
      if (transportLineFacingBelt == null) {
        continue;
      }

      bool isTopTransportLine = transportLine.isTopTransportLine;
      // The belt we are facing may be part of our transport line already. If so, stop
      if (transportLine == transportLineFacingBelt.GetTransportLine(isTopTransportLine)) {
        continue;
      }

      // The belt we are facing may not be actually connected to us (e.g. ->|<-). So, we need
      // to make sure merging is consensual.
      Belt previousBeltOfBeltCurrentlyFaced =
        GetNeighboringEntities(transportLineFacingBelt).
        GetPreviousBelt(transportLineFacingBelt.beltOrientation);
      if (previousBeltOfBeltCurrentlyFaced != lastNodeInLine) {
        continue;
      }

      if (transportLine.Size() + transportLineFacingBelt.GetTransportLine(isTopTransportLine).Size()
          <= TRANSPORT_LINE_MAX_SIZE) {
        transportLine.Merge(transportLineFacingBelt.GetTransportLine(isTopTransportLine));
      }
    }
    transportLines.ExceptWith(linesToCleanUp);
  }

  private void TryToOutputTransportLines() {
    foreach (TransportLine transportLine in transportLines) {
      if (transportLine.IsEmpty()) {
        continue;
      }

      // Get any TransportLine that the end of the Transport line may be facing
      Belt lastBeltInLine = transportLine.GetLastBeltInLine();
      NeighboringEntities<Belt> neighboringBelts =
        GetNeighboringEntities<Belt>(lastBeltInLine.xPos, lastBeltInLine.yPos);
      Belt facingBelt =
        neighboringBelts.GetFacingEntity(lastBeltInLine.placedDirection);
      TransportLine outputTransportLine =
        facingBelt?.GetNearestTransportLineComingFromDirection(lastBeltInLine.placedDirection);
      transportLine.TryToOutputToNextTransportLine(outputTransportLine, facingBelt);
    }
  }

  private void AdvanceTransportLines() {
    foreach (TransportLine transportLine in transportLines) {
      if (transportLine.IsEmpty()) {
        continue;
      }

      transportLine.AdvanceTransportLine();
      //Debug.Print($"{transportLine.ToString()}");
    }
  }

  private void PaintDebugTransportLines() {
    itemAndPositionToPaint.Clear();
    Dictionary<TempItem, int> numberVisibleItems = new Dictionary<TempItem, int>();
    foreach (TransportLine transportLine in transportLines) {
      List<TransportLine.ItemToDraw> itemsToDraw = transportLine.GetItemsToDraw(new Rect2());
      foreach (TransportLine.ItemToDraw itemToDraw in itemsToDraw) {
        int numVisible = numberVisibleItems.GetOrDefault(itemToDraw.itemType);
        MultiMeshInstance2D multiMeshInstance = itemToMultiMeshDict[itemToDraw.itemType];
        numberVisibleItems[itemToDraw.itemType] = numVisible + 1;
        multiMeshInstance.Multimesh.Mesh = GetNode<MeshInstance2D>("CoalOre").Mesh;

        Transform2D transform2D = new Transform2D(0f, itemToDraw.worldPosition);
        multiMeshInstance.Multimesh.SetInstanceTransform2D(numVisible, transform2D);

        // TODO: deal somehow with case instances > 1000
      }
    }

    foreach (var itemToMultiMesh in itemToMultiMeshDict) {
      itemToMultiMesh.Value.Multimesh.VisibleInstanceCount
        = Math.Min(1000, numberVisibleItems.GetOrDefault(itemToMultiMesh.Key));
    }
  }

  public bool AddBelt(int xPos, int yPos, Direction newBeltDirection) {
    if (placedEntitiesGrid.GetOrNull((xPos, yPos)) != null) {
      return false;
    }

    Belt newBelt = InstantiateBelt(xPos, yPos);
    newBelt.Place(xPos, yPos);
    placedEntitiesGrid[(xPos, yPos)] = newBelt;

    // When a new belt is added, we need to update the direction/orientation of it
    // and the belt we will be facing.
    newBelt.placedDirection = newBeltDirection;
    NeighboringEntities<Belt> neighboringBelts =
       GetNeighboringEntities<Belt>(newBelt.xPos, newBelt.yPos);
    newBelt.UpdateBeltOrientation(DefineBeltOrientation(newBeltDirection, neighboringBelts));
    CreateOrSplitIntoNewTransportLine(newBelt);

    Belt facingBelt = neighboringBelts.GetFacingEntity(newBeltDirection);
    MaybeUpdateBeltOrientationAndTransportLine(facingBelt);

    return true;
  }

  private bool MaybeUpdateBeltOrientationAndTransportLine(Belt beltToUpdate) {
    if (beltToUpdate == null) {
      return false;
    }
    NeighboringEntities<Belt> facingBeltNeighboringEntities = GetNeighboringEntities<Belt>(
      beltToUpdate.xPos, beltToUpdate.yPos);
    BeltOrientation originalFacingBeltOrientation = beltToUpdate.beltOrientation;
    BeltOrientation newFacingBeltOrientation = DefineBeltOrientation(
      beltToUpdate.placedDirection, facingBeltNeighboringEntities);

    if (originalFacingBeltOrientation == newFacingBeltOrientation) {
      return false;
    }
    beltToUpdate.UpdateBeltOrientation(newFacingBeltOrientation);
    CreateOrSplitIntoNewTransportLine(beltToUpdate);
    return true;
  }

  public bool RotateForward(int xPos, int yPos) {
    // TODO: generalize all of this. currently the focus is getting enough to test some systems
    PlacedEntity entityToRotate = placedEntitiesGrid.GetOrNull((xPos, yPos));

    if (entityToRotate as Inserter != null) {
      Inserter inserter = entityToRotate as Inserter;
      NeighboringEntities<PlacedEntity> neighboringItems =
        GetNeighboringEntities<PlacedEntity>(xPos, yPos);
      inserter.UpdateDirection(inserter.direction.RotateForward(), neighboringItems);
      return true;
    }

    Belt beltToRotate = entityToRotate as Belt;
    if (beltToRotate == null) {
      return false;
    }

    // When a  belt is rotate, we need to update the direction/orientation of it
    // and also both the belt we were facing and the belt will be facing.
    NeighboringEntities<Belt> neighboringBelts =
       GetNeighboringEntities<Belt>(beltToRotate.xPos, beltToRotate.yPos);
    Belt oldFacingBelt = neighboringBelts.GetFacingEntity(beltToRotate.placedDirection);

    Direction newBeltDirection = beltToRotate.placedDirection.RotateForward();
    beltToRotate.placedDirection = newBeltDirection;
    beltToRotate.UpdateBeltOrientation(DefineBeltOrientation(newBeltDirection, neighboringBelts));
    CreateOrSplitIntoNewTransportLine(beltToRotate);

    MaybeUpdateBeltOrientationAndTransportLine(oldFacingBelt);

    Belt newFacingBelt = neighboringBelts.GetFacingEntity(newBeltDirection);
    MaybeUpdateBeltOrientationAndTransportLine(newFacingBelt);

    return true;
  }

  private void CreateOrSplitIntoNewTransportLine(Belt belt) {
    foreach (bool isTopTransportLine in new List<bool>() { true, false }) {
      if (belt.GetTransportLine(isTopTransportLine) == null) {
        transportLines.Add(new TransportLine(belt, isTopTransportLine));
        continue;
      }
      var (beforeLine, curLine, afterLine) =
        TransportLine.SplitTransportLineOnBelt(belt.GetTransportLine(isTopTransportLine), belt);
      transportLines.Add(beforeLine);
      transportLines.Add(curLine);
      transportLines.Add(afterLine);
    }
  }

  public bool AddInserter(int xPos, int yPos, Direction inserterDirection) {
    if (placedEntitiesGrid.GetOrNull((xPos, yPos)) != null) {
      return false;
    }
    NeighboringEntities<PlacedEntity> neighboringEntities =
      GetNeighboringEntities<PlacedEntity>(xPos, yPos);
    InstantiateInserter(xPos, yPos, inserterDirection, neighboringEntities);
    // TODO every entity inserted needs to update all inserters around it (or all entities?)
    return true;
  }

  public bool AddSpawnerChest(int xPos, int yPos, TempItem itemType) {
    if (placedEntitiesGrid.GetOrNull((xPos, yPos)) != null) {
      return false;
    }
    SpawnerBox box = spawnerBoxScene.Instantiate<SpawnerBox>();
    AddChild(box);
    box.spawnItem = itemType;
    box.Position = tileMap.MapToLocal(new Vector2I(xPos, yPos)) + new Vector2(-16, -16);
    placedEntitiesGrid[(xPos, yPos)] = box;
    return true;
  }

  private Belt InstantiateBelt(int xPos, int yPos) {
    Belt newBelt = beltScene.Instantiate<Belt>();
    AddChild(newBelt);
    newBelt.Position = tileMap.MapToLocal(new Vector2I(xPos, yPos)) + new Vector2(-16, -16);
    return newBelt;
  }

  private Inserter InstantiateInserter(
      int xPos, int yPos, Direction inserterDirection,
      NeighboringEntities<PlacedEntity> neighboringEntities) {
    Inserter inserter = inserterScene.Instantiate<Inserter>();
    AddChild(inserter);
    inserter.Position = tileMap.MapToLocal(new Vector2I(xPos, yPos)) + new Vector2(-16, -16);
    inserter.Initialize(xPos, yPos, inserterDirection, neighboringEntities);
    placedEntitiesGrid[(xPos, yPos)] = inserter;
    return inserter;
  }

  private BeltOrientation DefineBeltOrientation(
    Direction placedDirection, NeighboringEntities<Belt> neighboringBelts) {
    bool isTopBeltFacing = neighboringBelts.topEntity?.placedDirection == Direction.DOWN;
    bool isBottomBeltFacing = neighboringBelts.bottomEntity?.placedDirection == Direction.UP;
    bool isLeftBeltFacing = neighboringBelts.leftEntity?.placedDirection == Direction.RIGHT;
    bool isRightBeltFacing = neighboringBelts.rightEntity?.placedDirection == Direction.LEFT;

    switch (placedDirection) {
      case Direction.UP:
        if (isBottomBeltFacing || (isLeftBeltFacing && isRightBeltFacing)) {
          return BeltOrientation.UP;
        } else if (isLeftBeltFacing) {
          return BeltOrientation.RIGHT_UP;
        } else if (isRightBeltFacing) {
          return BeltOrientation.LEFT_UP;
        }
        return BeltOrientation.UP;
      case Direction.DOWN:
        if (isTopBeltFacing || (isLeftBeltFacing && isRightBeltFacing)) {
          return BeltOrientation.DOWN;
        } else if (isLeftBeltFacing) {
          return BeltOrientation.RIGHT_DOWN;
        } else if (isRightBeltFacing) {
          return BeltOrientation.LEFT_DOWN;
        }
        return BeltOrientation.DOWN;
      case Direction.LEFT:
        if (isRightBeltFacing || (isTopBeltFacing && isBottomBeltFacing)) {
          return BeltOrientation.LEFT;
        } else if (isTopBeltFacing) {
          return BeltOrientation.DOWN_LEFT;
        } else if (isBottomBeltFacing) {
          return BeltOrientation.UP_LEFT;
        }
        return BeltOrientation.LEFT;
      case Direction.RIGHT:
        if (isLeftBeltFacing || (isTopBeltFacing && isBottomBeltFacing)) {
          return BeltOrientation.RIGHT;
        } else if (isTopBeltFacing) {
          return BeltOrientation.DOWN_RIGHT;
        } else if (isBottomBeltFacing) {
          return BeltOrientation.UP_RIGHT;
        }
        return BeltOrientation.RIGHT;
    }
    return BeltOrientation.UP;
  }

  private NeighboringEntities<Belt> GetNeighboringEntities(Belt belt) {
    return GetNeighboringEntities<Belt>(belt.xPos, belt.yPos);
  }

  private NeighboringEntities<T> GetNeighboringEntities<T>(int xPos, int yPos) where T : PlacedEntity {
    NeighboringEntities<T> neighboringBelts = new NeighboringEntities<T>();
    neighboringBelts.topEntity = placedEntitiesGrid.GetOrNull((xPos, yPos - 1)) as T;
    neighboringBelts.bottomEntity = placedEntitiesGrid.GetOrNull((xPos, yPos + 1)) as T;
    neighboringBelts.leftEntity = placedEntitiesGrid.GetOrNull((xPos - 1, yPos)) as T;
    neighboringBelts.rightEntity = placedEntitiesGrid.GetOrNull((xPos + 1, yPos)) as T;
    return neighboringBelts;
  }
}
