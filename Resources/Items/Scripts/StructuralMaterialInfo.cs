using Godot;
using System;

[Tool]
[GlobalClass]
[ItemBehaviorDisplayName("Structural Material")]
[ItemBehaviorIcon("res://Assets/Textures/Items/part-metal-beam-1.png")]
[ItemBehaviorIconColor("Light Slate Gray")]
public partial class StructuralMaterialInfo : Resource {
  [Export(PropertyHint.Range, "-1000, 1000, 5")]
  [DisplayedProperty("Durability Δ:", "du")]
  public float materialDurabilityDelta;

  [Export(PropertyHint.Range, "-100, 100, 5")]
  [DisplayedProperty("Min. Temp. Δ:", "C")]
  public float materialMinTemperatureDelta;

  [Export(PropertyHint.Range, "-100, 100, 5")]
  [DisplayedProperty("Max. Temp. Δ:", "C")]
  public float materialMaxTemperatureDelta;

  [Export(PropertyHint.Range, "0, 100, 1")]
  [DisplayedProperty("Rad. Block:", "rad", true)]
  public float radiationBlockageDelta;
}
