using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

[Tool]
public partial class ItemBrowser : Control {
  private const string SAVE_NEW_FILE_MESSAGE =
    "You are creating a new Item in the following path: {0}. "
    + "If this item name has changed, remember to delete the old file manually.";
  private const string OVERWRITE_FILE_MESSAGE =
    "You are overwriting the Item in the following path: {0}. "
    + "You can not recover the old content so make sure you are right before continuing.";
  private const string UNSAVED_CHANGES =
    "You have unsaved changed on the current Item. Are you sure you want to proceed?";
  private const string REMOVE_BEHAVIOR_WARNING =
    "You are removing an Item behavior. This is a non-recoverable action and you may lose any "
    + "relevant existing data under this behavior.";
  private const string CANT_SAVE_ITEM_NO_ITEM_ID =
    "Can't save this item due to the lack of a item-id. "
    + "Please generate one by setting Item Name";

  private const string ITEM_FILES_BASE_PATH =
    "res://Resources/Items/";
  private const string RECIPE_FILES_BASE_PATH =
    "res://Resources/Recipes/";

  [Export] PackedScene editorPopupScene;

  // Select item button
  [Export] public Button selectedItemButton;
  [Export] public Button cancelSelectionButton;

  // Top Menu
  [Export] Control topBar;
  [Export] Button addNewItemButton;
  [Export] Button backwardsButton;
  [Export] Button forwardsButton;
  [Export] Button exitButton;

  // Search Bar
  [Export] LineEdit searchBar;
  [Export] Control searchTab;
  [Export] PackedScene searchResultScene;

  // Item Overview
  [Export] Control itemInfoMenu;
  [Export] TextureRect itemTexture;
  [Export] LineEdit itemName;
  [Export] LineEdit itemId;
  [Export] Button generateItemId;
  [Export] Button saveButton;

  // Behaviors
  [Export] GridContainer itemBehaviorsContainer;
  [Export] Button addItemBehaviorButton;
  [Export] PackedScene itemBehaviorDisplayScene;
  [Export] PackedScene itemBehaviorPopupScene;

  [Export] Label unsavedChangesLabel;

  private Dictionary<string, Item> itemIdToItemMap = new Dictionary<string, Item>();
  private Dictionary<string, Recipe> recipeIdToRecipeMap = new Dictionary<string, Recipe>();
  private Item currentItem = null;
  private bool valueChangedSinceLastSave;

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

  private Dictionary<ItemBehaviorType, Func<Item, Resource>> itemBehaviorTypeToGetResourceFn =
    new Dictionary<ItemBehaviorType, Func<Item, Resource>>() {
      { ItemBehaviorType.STRUCTURAL_MATERIAL, (item) => item.structuralMaterialInfo },
      { ItemBehaviorType.FUEL, (item) => item.fuelInfo},
      { ItemBehaviorType.MAGNETIC_MATERIAL, (item) => item.magneticMaterialInfo},
      { ItemBehaviorType.ORGANIC_MATERIAL, (item) => item.organicMaterialInfo },
      { ItemBehaviorType.TELEPORTATION_MATERIAL, (item) => item.teleportationMaterialInfo},
      { ItemBehaviorType.PROCESSOR, (item) => item.processorInfo },
      { ItemBehaviorType.HEATING_MATERIAL, (item) => item.heatingMaterialInfo },
      { ItemBehaviorType.COOLING_MATERIAL, (item) => item.coolingMaterialInfo },
      { ItemBehaviorType.PERISHABLE_MATERIAL, (item) => item.perishableMaterialInfo }
    };

  private Dictionary<ItemBehaviorType, Action<Item>> itemBehaviorTypeToRemoveLambda =
    new Dictionary<ItemBehaviorType, Action<Item>>() {
      { ItemBehaviorType.STRUCTURAL_MATERIAL, (item) => item.structuralMaterialInfo = null},
      { ItemBehaviorType.FUEL, (item) => item.fuelInfo = null},
      { ItemBehaviorType.MAGNETIC_MATERIAL, (item) => item.magneticMaterialInfo = null},
      { ItemBehaviorType.ORGANIC_MATERIAL, (item) => item.organicMaterialInfo = null},
      { ItemBehaviorType.TELEPORTATION_MATERIAL, (item) => item.teleportationMaterialInfo = null},
      { ItemBehaviorType.PROCESSOR, (item) => item.processorInfo = null},
      { ItemBehaviorType.HEATING_MATERIAL, (item) => item.heatingMaterialInfo = null},
      { ItemBehaviorType.COOLING_MATERIAL, (item) => item.coolingMaterialInfo = null},
      { ItemBehaviorType.PERISHABLE_MATERIAL, (item) => item.perishableMaterialInfo = null}
    };

  // Due to Godot https://github.com/godotengine/godot/issues/78513 still happening in 4.3,
  // when we run into it randomly in a new callback instead of binding the params in a lambda we
  // assign it an id and put it in this Dictionary to be accessed as a local variable.
  // It works, even if it's nonsense.
  private Dictionary<string, Action> callbackIdtoAction = new Dictionary<string, Action>();

  public override void _Ready() {
    currentItem = null;

    UpdateAllControlsForCurrentItem();
    SetValuesChangedSinceLastSave(false);

    PopulateRecipeDictionary();
    PopulateItemDictionary();
    SearchForItemWithText("");

    addNewItemButton.Pressed += OnAddNewItemPressed;
    backwardsButton.Pressed += OnBackwardsButtonPressed;
    forwardsButton.Pressed += OnForwardsButtonPressed;
    exitButton.Pressed += OnExitButtonPressed;
    searchBar.TextChanged += OnSearchBarTextChanged;
    // itemName.TextChanged += OnItemNameTextChanged;
    // saveButton.Pressed += OnSaveButtonPressed;

    addItemBehaviorButton.Pressed += OnAddItemBehaviorButton;
  }

  public void SetSelectionModeOnly() {
    topBar.Visible = false;
    saveButton.Visible = false;
    generateItemId.Visible = false;
    selectedItemButton.Visible = true;
    cancelSelectionButton.Visible = true;
  }

  public Item GetCurrentItem() {
    return currentItem;
  }

  private void UpdateAllControlsForCurrentItem() {
    if (currentItem == null) {
      itemInfoMenu.Visible = false;
      return;
    }

    itemInfoMenu.Visible = true;
    itemName.Text = currentItem.itemName;
    itemId.Text = currentItem.itemId;
    itemTexture.Texture = currentItem.itemIcon;
    // TODO: finish

    // The hack below is necessary because recreating the Behaviors means we have to track
    // expandability and if the behavior has slides, recreating it every change makes sliders
    // unusable. Future readers beware do not try getting rid of this :)
    Dictionary<ItemBehaviorType, Node> currentItemBehaviorsToDisplayMap
      = new Dictionary<ItemBehaviorType, Node>();
    foreach (Node child in itemBehaviorsContainer.GetChildren()) {
      if (child is HBoxContainer) {
        ItemBehaviorDisplay display = child.GetChild(0) as ItemBehaviorDisplay;
        currentItemBehaviorsToDisplayMap[display.currentItemBehaviorType] = child;
      }
    }

    foreach (var itemBehaviorTypeToResourceFn in itemBehaviorTypeToGetResourceFn) {
      ItemBehaviorType itemBehaviorType = itemBehaviorTypeToResourceFn.Key;
      Func<Item, Resource> typeToResourceFn = itemBehaviorTypeToResourceFn.Value;

      if (currentItemBehaviorsToDisplayMap.ContainsKey(itemBehaviorType)) {
        // If there's already a view for the ItemBehaviorType, check if the Item still has
        // the behavior. If it does not, delete the behavior view. Regardless, we won't add
        // a new one.
        if (itemBehaviorTypeToResourceFn.Value.Invoke(currentItem) == null) {
          currentItemBehaviorsToDisplayMap[itemBehaviorType].QueueFree();
        }
        continue;
      }

      // Otherwise, there's not a view for the current ItemBehavior and we may or may not
      // create it now.
      DisplayItemBehaviorIfPresent(
        itemBehaviorTypeToResourceFn.Key, itemBehaviorTypeToResourceFn.Value.Invoke(currentItem));
    }
  }

  private void DisplayItemBehaviorIfPresent(
      ItemBehaviorType itemBehaviorType, Resource resource) {
    if (resource == null) {
      return;
    }

    HBoxContainer hBoxContainer = new HBoxContainer();
    ItemBehaviorDisplay info =
      itemBehaviorDisplayScene.Instantiate<ItemBehaviorDisplay>();
    info.ConfigureItemBehaviorDisplayWithIconAndMetadata(
      resource, () => {
        SetValuesChangedSinceLastSave(true);
        UpdateAllControlsForCurrentItem();
      });
    hBoxContainer.AddChild(info);

    Button removeButton = new Button();
    string callbackId = Guid.NewGuid().ToString();
    callbackIdtoAction[callbackId]
      = () => {
        itemBehaviorTypeToRemoveLambda[itemBehaviorType].Invoke(currentItem);
        UpdateAllControlsForCurrentItem();
      };
    removeButton.Text = "X";
    removeButton.Pressed += () => OnRemoveItemBehaviorPressed(callbackId);
    hBoxContainer.AddChild(removeButton);
    itemBehaviorsContainer.AddChild(hBoxContainer);
  }

  private void OnRemoveItemBehaviorPressed(string callbackId) {
    Control editorPopup = editorPopupScene.Instantiate<Control>();
    editorPopup.GetNode<Label>("%PopupMessage").Text = REMOVE_BEHAVIOR_WARNING;
    editorPopup.GetNode<Button>("%OkButton").Pressed += () => {
      callbackIdtoAction[callbackId].Invoke();
      editorPopup.QueueFree();
    };
    editorPopup.GetNode<Button>("%CancelButton").Pressed += editorPopup.QueueFree;
    AddChild(editorPopup);
  }

  private void SetValuesChangedSinceLastSave(bool changedSinceLastSave) {
    valueChangedSinceLastSave = changedSinceLastSave;
    unsavedChangesLabel.Visible = changedSinceLastSave;
  }

  private void PopulateRecipeDictionary() {
    DirAccess dirAccess = DirAccess.Open(RECIPE_FILES_BASE_PATH);
    string[] recipeFiles = dirAccess.GetFiles();
    foreach (string fileName in recipeFiles) {
      if (!fileName.Contains(".tres")) {
        continue;
      }
      Recipe loadedRecipe = GD.Load<Recipe>(RECIPE_FILES_BASE_PATH + fileName);
      recipeIdToRecipeMap[loadedRecipe.recipeId] = loadedRecipe;
    }
  }

  private void PopulateItemDictionary() {
    DirAccess dirAccess = DirAccess.Open(ITEM_FILES_BASE_PATH);
    string[] recipeFiles = dirAccess.GetFiles();
    foreach (string fileName in recipeFiles) {
      if (!fileName.Contains(".tres")) {
        continue;
      }
      Item loadedItem = GD.Load<Item>(ITEM_FILES_BASE_PATH + fileName);
      itemIdToItemMap[loadedItem.itemId] = loadedItem;
    }
  }

  private void OnAddNewItemPressed() {
    currentItem = new Item();
    UpdateAllControlsForCurrentItem();
  }

  private void OnBackwardsButtonPressed() {
    throw new NotImplementedException();
  }

  private void OnForwardsButtonPressed() {
    throw new NotImplementedException();
  }

  private void OnExitButtonPressed() {
    throw new NotImplementedException();
  }

  private void OnSearchBarTextChanged(string newText) {
    SearchForItemWithText(newText);
  }

  private void SearchForItemWithText(string text) {
    PopulateRecipeDictionary();
    foreach (Node child in searchTab.GetChildren()) {
      if (child.Name != "SearchBar") {
        child.QueueFree();
      }
    }
    foreach (Item item in itemIdToItemMap.Values) {
      if (item.itemName.Contains(text) || item.itemId.Contains(text)) {
        Button searchResult = searchResultScene.Instantiate<Button>();
        searchResult.Text = item.itemName;
        searchResult.Pressed += () => OnSelectSearchResult(item);
        searchTab.AddChild(searchResult);
      }
    }
  }

  private void OnSelectSearchResult(Item item) {
    if (!valueChangedSinceLastSave) {
      currentItem = item;
      UpdateAllControlsForCurrentItem();
      return;
    }

    Control editorPopup = editorPopupScene.Instantiate<Control>();
    editorPopup.GetNode<Label>("%PopupMessage").Text = UNSAVED_CHANGES;
    editorPopup.GetNode<Button>("%OkButton").Pressed += () => {
      OnSelectSearchResultConfirmed(item);
      editorPopup.QueueFree();
    };
    editorPopup.GetNode<Button>("%CancelButton").Pressed += editorPopup.QueueFree;
    AddChild(editorPopup);
  }

  private void OnSelectSearchResultConfirmed(Item item) {
    currentItem = item;
    UpdateAllControlsForCurrentItem();
  }

  private void OnAddItemBehaviorButton() {
    AddItemBehaviorPopup popup = itemBehaviorPopupScene.Instantiate<AddItemBehaviorPopup>();
    popup.DisplayEligibleItemBehaviorsToAddToItem(
      currentItem, () => {
        SetValuesChangedSinceLastSave(true);
        UpdateAllControlsForCurrentItem();
      });
    AddChild(popup);
  }
}
