using Godot;
using System;

public partial class PlanetRenderer : Control {
  [Export] private PlanetLayerRenderer planetRenderer;
  [Export] private PlanetLayerRenderer atmosphereRenderer;

  public override void _Ready() {
  }

  public override void _Process(double delta) {
  }
}
