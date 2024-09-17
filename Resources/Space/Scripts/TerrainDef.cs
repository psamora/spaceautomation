using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;

[Tool]
[GlobalClass]
public partial class TerrainDef : Resource {

  [Export] public string name;

  // TODO: TileSet

  [Export] public bool isPassable;
  [Export] public bool isConstructable;
  [Export] public TerrainType terrainType;
  [Export] public float movementModifierPercent;
}

public enum TerrainType {
  LIQUID,
  SOLID,
  GAS,
  PLASMA
}
