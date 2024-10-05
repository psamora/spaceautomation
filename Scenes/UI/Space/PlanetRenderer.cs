using Godot;
using System;

[Tool]
// Render capable of display both a PlanetDef or an actual generated Planet
public partial class PlanetRenderer : Control {
  [Export] private PlanetLayerRenderer terrainLayerRenderer;
  [Export] private PlanetLayerRenderer cloudsLayerRenderer;

  public void DisplayPlanetDef(PlanetDef planetDef) {
    terrainLayerRenderer.DisplayPlanetLayerDef(planetDef.terrainLayer);
    cloudsLayerRenderer.DisplayPlanetLayerDef(planetDef.cloudLayer);
  }

  public override void _Ready() {
  }

  public override void _Process(double delta) {
  }
}
