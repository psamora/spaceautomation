using Godot;
using System;

public partial class StarSystem : Node2D {
  private const float MAX_PLANET_SIZE_SCALE = 1.5f;
  private const float MIN_PLANET_SIZE_SCALE = 0.1f;
  private const float SCALE_RANGE = MAX_PLANET_SIZE_SCALE - MIN_PLANET_SIZE_SCALE;
  private const float MAX_PLANET_SIZE_IN_SOLAR_R = 10f;
  private const float MIN_PLANET_SIZE_IN_SOLAR_R = 0.3f;
  private const float SOLAR_R_RANGE = MAX_PLANET_SIZE_IN_SOLAR_R - MIN_PLANET_SIZE_IN_SOLAR_R;

  private string solarSystemId;
  private string solarSystemName;
  private StarSystemDef def;

  private float starTemperatureC;
  private float starSizeInSolarR;
  private Color starColor;

  public void Initialize(StarSystemDef def, string solarSystemName, Random seededRandom) {
    this.def = def;
    this.solarSystemName = solarSystemName;
    // TODO: guid is not affected by seed, could be a problem w/ saves and regen, change
    this.solarSystemId = Guid.NewGuid().ToString();

    GenerateSolarSystem(seededRandom);
    //GeneratePlanets(seededRandom);
  }

  public void PlaceSolarSystem(Vector2 position) {
    Position = position;
  }

  public string GetId() {
    return solarSystemId;
  }

  public string GetSystemName() {
    return solarSystemName;
  }

  private void GenerateSolarSystem(Random seededRandom) {
    float temperatureRandomPick = seededRandom.NextSingle();
    starTemperatureC = ComputeFinalValue(
      temperatureRandomPick, def.minTemperatureC, def.maxTemperatureC);

    starColor = new Color(
      ComputeFinalValue(temperatureRandomPick, def.minTempColor.R, def.maxTempColor.R),
      ComputeFinalValue(temperatureRandomPick, def.minTempColor.G, def.maxTempColor.G),
      ComputeFinalValue(temperatureRandomPick, def.minTempColor.B, def.maxTempColor.B)
    );
    Modulate = starColor;

    starSizeInSolarR = ComputeFinalValue(
      seededRandom.NextSingle(), def.minSizeInSolarR, def.maxSizeInSolarR);
    float finalScale =
      (((starSizeInSolarR - MIN_PLANET_SIZE_SCALE) * SCALE_RANGE) / SOLAR_R_RANGE) +
      MIN_PLANET_SIZE_IN_SOLAR_R;
    Scale = new Vector2(finalScale, finalScale);
  }

  private float ComputeFinalValue(float randomResult, float min, float max) {
    return ((max - min) * randomResult) + min;
  }
}
