using Godot;
using System;

[Tool]
[GlobalClass]
public partial class Item : Resource {
  [Export]
  public string itemId;

  [Export]
  public string itemName;

  [Export]
  public Texture2D itemIcon;

  [Export(PropertyHint.Range, "1,200,1")]
  public int maxStackSize;

  [Export]
  public string buildingId = null;

  [Export(PropertyHint.Range, "-273,2000,1")]
  public float meltingTempC = 0f;

  [Export(PropertyHint.Range, "-273,2000,1")]
  public float boilingTempC = 100f;

  [Export(PropertyHint.Enum, "None, Tiny, Small, Medium, High, Very High, Massive")]
  public Amount heatCapacity = Amount.TINY;

  [Export(PropertyHint.Enum, "None, Tiny, Small, Medium, High, Very High, Massive")]
  public Amount thermalConductivity = Amount.TINY;


  [Export]
  public StructuralMaterialInfo structuralMaterialInfo;

  [Export]
  public FuelInfo fuelInfo;

  [Export]
  public MagneticMaterialInfo magneticMaterialInfo;

  [Export]
  public OrganicMaterialInfo organicMaterialInfo;

  [Export]
  public TeleportationMaterialInfo teleportationMaterialInfo;

  [Export]
  public ProcessorInfo processorInfo;

  [Export]
  public HeatingMaterialInfo heatingMaterialInfo;

  [Export]
  public CoolingMaterialInfo coolingMaterialInfo;

  [Export]
  public PerishableMaterialInfo perishableMaterialInfo;
}

public enum Amount {
  NONE,
  TINY,
  SMALL,
  MEDIUM,
  HIGH,
  VERY_HIGH,
  MASSIVE
}

// TODO: this is a bad name :)
public enum ItemBehaviorType {
  NO_ITEM_TYPE,
  STRUCTURAL_MATERIAL,
  FUEL,
  MAGNETIC_MATERIAL,
  ORGANIC_MATERIAL,
  TELEPORTATION_MATERIAL,
  PROCESSOR,
  HEATING_MATERIAL,
  COOLING_MATERIAL,
  PERISHABLE_MATERIAL
}
