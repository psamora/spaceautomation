using Godot;
using System;
using System.Linq;

[Tool]
public partial class AddItemBehaviorPopup : Control {
  [Export] VBoxContainer behaviorContainer;
  [Export] PackedScene itemBehaviorDisplayScene;
  [Export] Button addItemButton;

  private Action markUnsavedChangesAction;

  public void DisplayEligibleItemBehaviorsToAddToItem(Item currentItem, Action markUnsavedChangesAction) {
    this.markUnsavedChangesAction = markUnsavedChangesAction;
    addItemButton.Pressed += QueueFree;

    // We could commit more reflection crimes in order to make the code below a for loop.
    // Probably not worth it. Probably.
    DisplayItemBehaviorForItemIfPresent<StructuralMaterialInfo>(
      currentItem.structuralMaterialInfo, new StructuralMaterialInfo(),
      (newResource) => currentItem.structuralMaterialInfo = newResource);
    DisplayItemBehaviorForItemIfPresent<FuelInfo>(
      currentItem.fuelInfo, new FuelInfo(),
      (newResource) => currentItem.fuelInfo = newResource);
    DisplayItemBehaviorForItemIfPresent<MagneticMaterialInfo>(
      currentItem.magneticMaterialInfo, new MagneticMaterialInfo(),
      (newResource) => currentItem.magneticMaterialInfo = newResource);
    DisplayItemBehaviorForItemIfPresent<OrganicMaterialInfo>(
      currentItem.organicMaterialInfo, new OrganicMaterialInfo(),
      (newResource) => currentItem.organicMaterialInfo = newResource);
    DisplayItemBehaviorForItemIfPresent<TeleportationMaterialInfo>(
      currentItem.teleportationMaterialInfo, new TeleportationMaterialInfo(),
      (newResource) => currentItem.teleportationMaterialInfo = newResource);
    DisplayItemBehaviorForItemIfPresent<ProcessorInfo>(
      currentItem.processorInfo, new ProcessorInfo(),
      (newResource) => currentItem.processorInfo = newResource);
    DisplayItemBehaviorForItemIfPresent<HeatingMaterialInfo>(
       currentItem.heatingMaterialInfo, new HeatingMaterialInfo(),
       (newResource) => currentItem.heatingMaterialInfo = newResource);
    DisplayItemBehaviorForItemIfPresent<CoolingMaterialInfo>(
       currentItem.coolingMaterialInfo, new CoolingMaterialInfo(),
       (newResource) => currentItem.coolingMaterialInfo = newResource);
    DisplayItemBehaviorForItemIfPresent<PerishableMaterialInfo>(
       currentItem.perishableMaterialInfo, new PerishableMaterialInfo(),
       (newResource) => currentItem.perishableMaterialInfo = newResource);
  }

  public void DisplayEligibleItemBehaviorsToAddToRecipe(Recipe currentRecipe, Action markUnsavedChangesAction) {
    this.markUnsavedChangesAction = markUnsavedChangesAction;
    addItemButton.Pressed += QueueFree;

    // We could commit more reflection crimes in order to make the code below a for loop.
    // Probably not worth it. Probably.
    DisplayItemBehaviorForRecipeIfPresent<StructuralMaterialInfo>(
      currentRecipe, ItemBehaviorType.STRUCTURAL_MATERIAL, new StructuralMaterialInfo());
    DisplayItemBehaviorForRecipeIfPresent<FuelInfo>(
      currentRecipe, ItemBehaviorType.FUEL, new FuelInfo());
    DisplayItemBehaviorForRecipeIfPresent<MagneticMaterialInfo>(
      currentRecipe, ItemBehaviorType.MAGNETIC_MATERIAL, new MagneticMaterialInfo());
    DisplayItemBehaviorForRecipeIfPresent<OrganicMaterialInfo>(
      currentRecipe, ItemBehaviorType.ORGANIC_MATERIAL, new OrganicMaterialInfo());
    DisplayItemBehaviorForRecipeIfPresent<TeleportationMaterialInfo>(
      currentRecipe, ItemBehaviorType.TELEPORTATION_MATERIAL, new TeleportationMaterialInfo());
    DisplayItemBehaviorForRecipeIfPresent<ProcessorInfo>(
      currentRecipe, ItemBehaviorType.PROCESSOR, new ProcessorInfo());
    DisplayItemBehaviorForRecipeIfPresent<HeatingMaterialInfo>(
      currentRecipe, ItemBehaviorType.HEATING_MATERIAL, new HeatingMaterialInfo());
    DisplayItemBehaviorForRecipeIfPresent<CoolingMaterialInfo>(
      currentRecipe, ItemBehaviorType.COOLING_MATERIAL, new CoolingMaterialInfo());
    DisplayItemBehaviorForRecipeIfPresent<PerishableMaterialInfo>(
      currentRecipe, ItemBehaviorType.PERISHABLE_MATERIAL, new PerishableMaterialInfo());
  }

  private void DisplayItemBehaviorForItemIfPresent<T>(
      T existingItemBehavior, T newItemBehavior, Action<T> setBehaviorAction) where T : Resource {
    if (existingItemBehavior != null) {
      return;
    }

    HBoxContainer hBoxContainer = new HBoxContainer();
    ItemBehaviorDisplay info =
      itemBehaviorDisplayScene.Instantiate<ItemBehaviorDisplay>();
    info.ConfigureItemBehaviorDisplayWithIconAndLabelOnly(newItemBehavior);
    hBoxContainer.AddChild(info);

    Button currentAddItemButton = addItemButton.Duplicate() as Button;
    currentAddItemButton.Text = "+";
    currentAddItemButton.Visible = true;
    currentAddItemButton.Pressed += () => {
      setBehaviorAction(newItemBehavior);
      markUnsavedChangesAction.Invoke();
      QueueFree();
    };

    hBoxContainer.AddChild(currentAddItemButton);
    behaviorContainer.AddChild(hBoxContainer);
  }

  private void DisplayItemBehaviorForRecipeIfPresent<T>(
      Recipe currentRecipe, ItemBehaviorType behaviorType, T newItemBehavior) where T : Resource {
    bool alreadyHasBehaviorInput = currentRecipe.requiredItemBehaviors.Any(
      requiredItemBehavior => requiredItemBehavior.requiredItemBehaviorType == behaviorType);
    if (alreadyHasBehaviorInput) {
      return;
    }

    HBoxContainer hBoxContainer = new HBoxContainer();
    ItemBehaviorDisplay info =
      itemBehaviorDisplayScene.Instantiate<ItemBehaviorDisplay>();
    info.ConfigureItemBehaviorDisplayWithIconAndLabelOnly(newItemBehavior);
    hBoxContainer.AddChild(info);

    Button currentAddItemButton = addItemButton.Duplicate() as Button;
    currentAddItemButton.Text = "+";
    currentAddItemButton.Visible = true;
    currentAddItemButton.Pressed += () => {
      RequiredItemBehavior reqItemBehavior = new RequiredItemBehavior();
      reqItemBehavior.requiredItemBehaviorType = behaviorType;
      currentRecipe.requiredItemBehaviors.Add(reqItemBehavior);
      currentRecipe.requiredItemBehaviors.Sort();
      markUnsavedChangesAction.Invoke();
      QueueFree();
    };

    hBoxContainer.AddChild(currentAddItemButton);
    behaviorContainer.AddChild(hBoxContainer);
  }
}
