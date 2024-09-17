using Godot;
using System;

[GlobalClass]
[Tool]
public partial class StarSystemDef : Resource {
  [Export] public string starTypeName;
  [Export] public float maxTemperatureC;
  [Export] public float minTemperatureC;
  [Export] public Color maxTempColor;
  [Export] public Color minTempColor;
  [Export] public float minSizeInSolarR;
  [Export] public float maxSizeInSolarR;
  [Export] public int minNumberToGenerate;
  [Export] public float probability;
  [Export] public int minNumPlanets;
  [Export] public int maxNumPlanets;
}
