using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

[Tool]
public partial class ItemBehaviorDisplay : Control {
  [Export] Panel iconBorder;
  [Export] TextureRect itemIcon;
  [Export] Label itemBehaviorLabel;
  [Export] Button expandSectionButton;
  [Export] CustomResourceViewer customResourceViewer;

  // TODO: this is a hack, if we proceed with it make these private
  public bool isExpanded = false;
  public ItemBehaviorType currentItemBehaviorType;
  private Vector2 originalMinimumSize;

  private Dictionary<Type, ItemBehaviorType> baseTypeToItemBehaviorType =
    new Dictionary<Type, ItemBehaviorType>() {
      { new StructuralMaterialInfo().GetType(), ItemBehaviorType.STRUCTURAL_MATERIAL },
      { new FuelInfo().GetType(), ItemBehaviorType.FUEL },
      { new MagneticMaterialInfo().GetType(), ItemBehaviorType.MAGNETIC_MATERIAL},
      { new OrganicMaterialInfo().GetType(), ItemBehaviorType.ORGANIC_MATERIAL },
      { new TeleportationMaterialInfo().GetType(), ItemBehaviorType.TELEPORTATION_MATERIAL },
      { new ProcessorInfo().GetType(), ItemBehaviorType.PROCESSOR },
      { new HeatingMaterialInfo().GetType(), ItemBehaviorType.HEATING_MATERIAL },
      { new CoolingMaterialInfo().GetType(), ItemBehaviorType.COOLING_MATERIAL },
      { new PerishableMaterialInfo().GetType(), ItemBehaviorType.PERISHABLE_MATERIAL}
    };

  private Dictionary<ItemBehaviorType, Resource> itemBehaviorTypeToEmptyResource =
    new Dictionary<ItemBehaviorType, Resource>() {
      { ItemBehaviorType.STRUCTURAL_MATERIAL, new StructuralMaterialInfo()},
      { ItemBehaviorType.FUEL, new FuelInfo()},
      { ItemBehaviorType.MAGNETIC_MATERIAL, new MagneticMaterialInfo()},
      { ItemBehaviorType.ORGANIC_MATERIAL, new OrganicMaterialInfo()},
      { ItemBehaviorType.TELEPORTATION_MATERIAL, new TeleportationMaterialInfo()},
      { ItemBehaviorType.PROCESSOR, new ProcessorInfo()},
      { ItemBehaviorType.HEATING_MATERIAL, new HeatingMaterialInfo()},
      { ItemBehaviorType.COOLING_MATERIAL, new CoolingMaterialInfo()},
      { ItemBehaviorType.PERISHABLE_MATERIAL, new PerishableMaterialInfo()}
    };

  public override void _Ready() {
    expandSectionButton.Pressed += OnExpandSectionPressed;
  }

  public void ExpandSection() {
    OnExpandSectionPressed();
  }

  public void ConfigureItemBehaviorDisplayWithIconOnly(ItemBehaviorType itemBehaviorType) {
    this.currentItemBehaviorType = itemBehaviorType;
    ConfigureBehaviorIconAndLabel(itemBehaviorTypeToEmptyResource[itemBehaviorType]);
    itemBehaviorLabel.Visible = false;
    expandSectionButton.Visible = false;
  }

  public void ConfigureItemBehaviorDisplayWithIconAndLabelOnly(Resource currentResource) {
    this.currentItemBehaviorType = GetItemBehaviorTypeForResource(currentResource);
    ConfigureBehaviorIconAndLabel(currentResource);
    expandSectionButton.Visible = false;
  }

  public void ConfigureItemBehaviorDisplayWithIconAndMetadata(
      Resource currentResource, Action unsavedChangedCallback) {
    this.currentItemBehaviorType = GetItemBehaviorTypeForResource(currentResource);
    ConfigureBehaviorIconAndLabel(currentResource);
    expandSectionButton.Visible = true;

    customResourceViewer.BindResource(currentResource, unsavedChangedCallback);
    customResourceViewer.Visible = false;
    originalMinimumSize = CustomMinimumSize;
  }

  private void ConfigureBehaviorIconAndLabel(Resource currentResource) {
    Type curType = currentResource.GetType();
    foreach (Attribute attribute in curType.GetCustomAttributes(true)) {
      if (attribute is ItemBehaviorDisplayName) {
        itemBehaviorLabel.Text = (attribute as ItemBehaviorDisplayName).displayName;
        continue;
      }

      if (attribute is ItemBehaviorIcon) {
        itemIcon.Texture = (attribute as ItemBehaviorIcon).texture;
        continue;
      }

      if (attribute is ItemBehaviorIconColor) {
        ItemBehaviorIconColor colorAttribute = attribute as ItemBehaviorIconColor;
        StyleBoxFlat adjustedStyleBox =
          iconBorder.GetThemeStylebox("panel", "StyleBoxFlat").Duplicate() as StyleBoxFlat;
        adjustedStyleBox.DrawCenter = true;
        adjustedStyleBox.BgColor = colorAttribute.color;
        adjustedStyleBox.BorderColor = colorAttribute.borderColor;
        iconBorder.AddThemeStyleboxOverride("panel", adjustedStyleBox);
        continue;
      }
    }
  }

  private void OnExpandSectionPressed() {
    if (isExpanded) {
      isExpanded = false;
      customResourceViewer.Visible = isExpanded;
      expandSectionButton.Text = "⌄";
      CustomMinimumSize = originalMinimumSize;
    } else {
      isExpanded = true;
      customResourceViewer.Visible = isExpanded;
      expandSectionButton.Text = "⌃";
      CustomMinimumSize =
        CustomMinimumSize + new Godot.Vector2(0, customResourceViewer.CustomMinimumSize.Y);
    }
  }

  private ItemBehaviorType GetItemBehaviorTypeForResource(Resource resource) {
    if (baseTypeToItemBehaviorType.ContainsKey(resource.GetType())) {
      return baseTypeToItemBehaviorType[resource.GetType()];
    }
    return ItemBehaviorType.NO_ITEM_TYPE;
  }
}
