using Godot;
using System;
using System.Collections.Generic;

[Tool]
[GlobalClass]
public partial class Recipe : Resource {
  [Export]
  public string recipeId;

  [Export]
  public string recipeName;

  [Export]
  public Texture2D recipeIcon;

  [Export(PropertyHint.Range, "1, 180, 1")]
  [DisplayedProperty("Production Time:", "s")]
  public int productionTimeInS;

  [Export(PropertyHint.Range, "1, 1000, 1")]
  [DisplayedProperty("Fuel Cost:", "J")]
  public int fuelCostInJ;

  [Export(PropertyHint.Range, "-10, 10, 1")]
  [DisplayedProperty("Temperature Output:", "C")]
  public float temperatureOutput;

  [Export(PropertyHint.Range, "1, 200, 1")]
  [DisplayedProperty("Noise Output:", "db")]
  public float noiseOutput;

  [Export(PropertyHint.Range, "1, 500, 1")]
  [DisplayedProperty("Pollution Output:", "pu")]
  public float pollutionOutput;

  [Export(PropertyHint.Range, "1, 1000, 5")]
  [DisplayedProperty("Radiation Output:", "rad", true)]
  public float radiationOutput;

  [Export]
  [DisplayedProperty("Can be Handcrafted:", "")]
  public bool canBeHandcrafted;

  [Export]
  [DisplayedProperty("Can be Modulated:", "")]
  public bool canBeModulated;

  [Export]
  public Godot.Collections.Array<string> eligibleProducerBuildingId
    = new Godot.Collections.Array<string>();

  [Export]
  public Godot.Collections.Array<RequiredItem> requiredSpecificItems
    = new Godot.Collections.Array<RequiredItem>();

  [Export]
  public Godot.Collections.Array<RequiredItemBehavior> requiredItemBehaviors
    = new Godot.Collections.Array<RequiredItemBehavior>();

  [Export]
  public Godot.Collections.Array<OutputItem> outputItems
    = new Godot.Collections.Array<OutputItem>();
}
