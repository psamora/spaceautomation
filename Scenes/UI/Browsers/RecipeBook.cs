using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

[Tool]
public partial class RecipeBook : Control {
  private const string SAVE_NEW_FILE_MESSAGE =
    "You are creating a new Recipe in the following path: {0}. "
    + "If this recipe name has changed, remember to delete the old file manually.";
  private const string OVERWRITE_FILE_MESSAGE =
    "You are overwriting the Recipe in the following path: {0}. "
    + "You can not recover the old content so make sure you are right before continuing.";
  private const string UNSAVED_CHANGES =
    "You have unsaved changed on the current Recipe. Are you sure you want to proceed?";
  private const string CANT_SAVE_RECIPE_NO_RECIPE_ID =
    "Can't save this recipe due to the lack of a recipe-id. "
    + "Please generate one by setting Recipe Name";

  private const string ITEM_FILES_BASE_PATH =
    "res://Resources/Items/";
  private const string RECIPE_FILES_BASE_PATH =
    "res://Resources/Recipes/";
  [Export] PackedScene editorPopupScene;
  [Export] PackedScene itemBrowserScene;

  // Top Menu
  [Export] Button addNewRecipeButton;
  [Export] Button backwardsButton;
  [Export] Button forwardsButton;
  [Export] Button exitButton;

  // Search Bar
  [Export] LineEdit searchBar;
  [Export] Control searchTab;
  [Export] PackedScene searchResultScene;

  // Recipe Overview
  [Export] Control recipeInfoMenu;
  [Export] VBoxContainer recipeIconContainer;
  [Export] TextureRect recipeIcon;
  [Export] LineEdit recipeName;
  [Export] LineEdit recipeId;
  [Export] Button generateRecipeId;
  [Export] Button saveButton;
  [Export] CustomResourceViewer overviewPropertiesViewer;
  [Export] CustomResourceViewer sideEffectsProprietiesViewer;
  [Export] HBoxContainer unlockedBy;

  // Recipe Inputs
  [Export] PackedScene recipeInputScene;
  [Export] PackedScene itemBehaviorPopupScene;
  [Export] PackedScene itemBehaviorDisplayScene;
  [Export] HBoxContainer recipeRequiredItemBehaviorsContainer;
  [Export] Button addRequiredItemBehaviorButton;
  [Export] HBoxContainer recipeSpecificRequiredItemsContainer;
  [Export] Button addSpecificRequiredItemButton;

  // Recipe Outputs
  [Export] PackedScene recipeOutputScene;
  [Export] HBoxContainer recipeOutputContainer;
  [Export] Button addRecipeOutputButton;

  // Can be Produced On
  [Export] PackedScene canBeProducedOnScene;
  [Export] HBoxContainer canBeProducedOnContainer;
  [Export] Button addCanBeProducedOnButton;

  [Export] Label unsavedChangesLabel;

  EditorResourcePicker recipeIconPicker = new EditorResourcePicker();

  private Dictionary<string, Item> itemIdToItemMap = new Dictionary<string, Item>();
  private Dictionary<string, Recipe> recipeIdToRecipeMap = new Dictionary<string, Recipe>();
  private Recipe currentRecipe;
  private bool valueChangedSinceLastSave;

  public override void _Ready() {
    currentRecipe = null;

    if (Engine.IsEditorHint()) {
      recipeIconPicker = new EditorResourcePicker();
      recipeIconPicker.BaseType = "Texture2D";
      recipeIconPicker.ResourceChanged += OnIconPickerResourceChanged;
      recipeIconContainer.AddChild(recipeIconPicker);
    }

    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(false);

    PopulateItemDictionary();
    PopulateRecipeDictionary();
    SearchForRecipeWithText("");

    addNewRecipeButton.Pressed += OnAddNewRecipePressed;
    backwardsButton.Pressed += OnBackwardsButtonPressed;
    forwardsButton.Pressed += OnForwardsButtonPressed;
    exitButton.Pressed += OnExitButtonPressed;
    searchBar.TextChanged += OnSearchBarTextChanged;
    recipeName.TextChanged += OnRecipeNameTextChanged;
    saveButton.Pressed += OnSaveButtonPressed;

    addRequiredItemBehaviorButton.Pressed += OnRequiredItemBehaviorButtonPressed;
    addSpecificRequiredItemButton.Pressed += OnAddRecipeInputButtonPressed;
    addRecipeOutputButton.Pressed += OnAddRecipeOutputButtonPressed;
  }

  private void UpdateAllControlsForCurrentRecipe() {
    if (currentRecipe == null) {
      recipeInfoMenu.Visible = false;
      return;
    }

    // Many of the updates below will trigger event listeners, but all listeners should 
    // properly ignore them to avoid setting "unsaved changes" in the wrong time.
    recipeName.Text = currentRecipe.recipeName;
    recipeId.Text = currentRecipe.recipeId;

    // Set-up icon texture and selector.
    recipeIcon.Texture = currentRecipe.recipeIcon;

    overviewPropertiesViewer.BindResource(
      currentRecipe, () => SetValuesChangedSinceLastSave(true));
    sideEffectsProprietiesViewer.BindResource(
      currentRecipe, () => SetValuesChangedSinceLastSave(true));

    if (Engine.IsEditorHint()) {
      recipeIconPicker.EditedResource = currentRecipe.recipeIcon;
    }

    // Set-up recipe inputs for required ItemBehaviors.
    foreach (Node child in recipeRequiredItemBehaviorsContainer.GetChildren()) {
      // TODO: check for specific child scripts
      if (!child.Name.ToString().Contains("Button")) {
        child.QueueFree();
      }
    }

    foreach (RequiredItemBehavior requiredItemBehavior in currentRecipe.requiredItemBehaviors) {
      ItemBehaviorType itemBehaviorType = requiredItemBehavior.requiredItemBehaviorType;
      if (itemBehaviorType == ItemBehaviorType.NO_ITEM_TYPE) {
        continue;
      }

      // TODO: Instead of having to instantiate a ItemBehaviorDisplay to copy a icon+border
      // we could just make static helpers in ItemBehaviorDisplay and reuse the existing icon
      // thingy. Would avoid all the hacky shit of resizing windows etc
      ItemBehaviorDisplay info =
        itemBehaviorDisplayScene.Instantiate<ItemBehaviorDisplay>();
      info.ConfigureItemBehaviorDisplayWithIconOnly(requiredItemBehavior.requiredItemBehaviorType);

      Control recipeInput = recipeInputScene.Instantiate<Control>();
      recipeInput.GetNode<LineEdit>("%ItemAmount").Text = requiredItemBehavior.itemAmount.ToString();
      recipeInput.GetNode<LineEdit>("%ItemAmount").TextSubmitted
        += (newText) => OnRecipeInputItemBehaviorAmountChanged(itemBehaviorType, newText);
      recipeInput.GetNode<Button>("%RemoveButton").Pressed
        += () => OnRemoveRecipeRequiredItemBehaviorButtonPressed(itemBehaviorType);

      recipeInput.GetNode<Control>("%ItemIcon").Visible = false;
      Panel iconBorder = info.GetNode<Panel>("%IconBorder").Duplicate() as Panel;
      iconBorder.CustomMinimumSize = new Vector2(64, 64);
      iconBorder.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
      recipeInput.AddChild(iconBorder);
      recipeRequiredItemBehaviorsContainer.AddChild(recipeInput);
    }

    // Set-up recipe inputs for specific required items.
    foreach (Node child in recipeSpecificRequiredItemsContainer.GetChildren()) {
      // TODO: check for specific child scripts
      if (!child.Name.ToString().Contains("Button")) {
        child.QueueFree();
      }
    }

    foreach (RequiredItem requiredItem in currentRecipe.requiredSpecificItems) {
      if (requiredItem.requiredItemId == null) {
        continue;
      }

      Item item = itemIdToItemMap.GetOrNull(requiredItem.requiredItemId);
      if (item == null) {
        Debug.Print(
          "Recipe book refer to item with id " + requiredItem.requiredItemId
          + " but it was not found");
        continue;
      }

      Control recipeInput = recipeInputScene.Instantiate<Control>();
      recipeInput.GetNode<LineEdit>("%ItemAmount").Text = requiredItem.itemAmount.ToString();
      recipeInput.GetNode<Control>("%ItemIcon").GetNode<Button>("%ItemIconButton").Icon = item.itemIcon;
      recipeInput.GetNode<LineEdit>("%ItemAmount").TextSubmitted += (newText) => OnRecipeInputItemAmountChanged(item.itemId, newText);
      recipeInput.GetNode<Button>("%RemoveButton").Pressed += () => OnRemoveRecipeInputButtonPressed(item.itemId);
      recipeSpecificRequiredItemsContainer.AddChild(recipeInput);
    }

    // Set-up recipe outputs
    foreach (Node child in recipeOutputContainer.GetChildren()) {
      if (child.Name != "AddOutputButton") {
        child.QueueFree();
      }
    }

    foreach (OutputItem outputItem in currentRecipe.outputItems) {
      if (outputItem.outputItemId == null) {
        // TODO: print error
        continue;
      }

      Item item = itemIdToItemMap.GetOrNull(outputItem.outputItemId);
      if (item == null) {
        Debug.Print(
          "Recipe book refer to item with id " + outputItem.outputItemId
          + " but it was not found");
        continue;
      }
      Control recipeOutput = recipeOutputScene.Instantiate<Control>();
      recipeOutput.GetNode<LineEdit>("%ItemAmount").Text = outputItem.itemAmount.ToString();
      recipeOutput.GetNode<LineEdit>("%ItemAmount").TextSubmitted += (newText) => OnRecipeOutputItemAmountChanged(item.itemId, newText);
      recipeOutput.GetNode<Label>("%OutputProbabilityLabel").Text = outputItem.outputProbability + "%";
      recipeOutput.GetNode<HSlider>("%OutputProbability").Value = outputItem.outputProbability;
      recipeOutput.GetNode<HSlider>("%OutputProbability").ValueChanged += (newValue) => OnRecipeOutputProbabilityChanged(item.itemId, newValue, recipeOutput.GetNode<Label>("%OutputProbabilityLabel"));
      recipeOutput.GetNode<Control>("%ItemIcon").GetNode<Button>("%ItemIconButton").Icon = item.itemIcon;
      recipeOutput.GetNode<Button>("%RemoveButton").Pressed += () => OnRemoveRecipeOutputButtonPressed(item.itemId);
      recipeOutputContainer.AddChild(recipeOutput);
    }

    foreach (Node child in canBeProducedOnContainer.GetChildren()) {
      if (child.Name != "AddBuildingButton") {
        child.QueueFree();
      }
    }

    // Recipe buildings
    foreach (string buildingId in currentRecipe.eligibleProducerBuildingId) {
      // TODO: todo
    }
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

  private void SearchForRecipeWithText(string text) {
    PopulateRecipeDictionary();
    foreach (Node child in searchTab.GetChildren()) {
      if (child.Name != "SearchBar") {
        child.QueueFree();
      }
    }
    foreach (Recipe recipe in recipeIdToRecipeMap.Values) {
      if (recipe.recipeName.Contains(text) || recipe.recipeId.Contains(text)) {
        Button searchResult = searchResultScene.Instantiate<Button>();
        searchResult.Text = recipe.recipeName;
        searchResult.Pressed += () => {
          OnSelectSearchResult(recipe);
        };
        searchTab.AddChild(searchResult);
      }
    }
  }

  private void OnSelectSearchResult(Recipe recipe) {
    if (!valueChangedSinceLastSave) {
      currentRecipe = recipe;
      recipeInfoMenu.Visible = true;
      UpdateAllControlsForCurrentRecipe();
      return;
    }

    Control editorPopup = editorPopupScene.Instantiate<Control>();
    editorPopup.GetNode<Label>("%PopupMessage").Text = UNSAVED_CHANGES;
    editorPopup.GetNode<Button>("%OkButton").Pressed
      += () => {
        OnSelectSearchResultConfirmed(recipe);
        editorPopup.QueueFree();
      };
    editorPopup.GetNode<Button>("%CancelButton").Pressed += editorPopup.QueueFree;
    AddChild(editorPopup);
  }

  private void OnSelectSearchResultConfirmed(Recipe recipe) {
    currentRecipe = recipe;
    recipeInfoMenu.Visible = true;
    SetValuesChangedSinceLastSave(false);
    UpdateAllControlsForCurrentRecipe();
  }


  private void OnAddNewRecipePressed() {
    currentRecipe = new Recipe();
    recipeInfoMenu.Visible = true;
    SetValuesChangedSinceLastSave(true);
    UpdateAllControlsForCurrentRecipe();
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
    SearchForRecipeWithText(newText);
  }

  private void OnRecipeNameTextChanged(string newText) {
    if (currentRecipe.recipeName == newText) {
      return;
    }

    currentRecipe.recipeName = newText;
    currentRecipe.recipeId = Regex.Replace(
      newText.ToLower().Replace(" ", "-"), "[^A-Za-z0-9-]", "");
    recipeId.Text = currentRecipe.recipeId;
    SetValuesChangedSinceLastSave(true);
  }

  private void OnRequiredItemBehaviorButtonPressed() {
    AddItemBehaviorPopup popup = itemBehaviorPopupScene.Instantiate<AddItemBehaviorPopup>();
    popup.DisplayEligibleItemBehaviorsToAddToRecipe(
      currentRecipe, () => {
        SetValuesChangedSinceLastSave(true);
        UpdateAllControlsForCurrentRecipe();
      });
    AddChild(popup);
  }

  private void OnAddRecipeInputButtonPressed() {
    ItemBrowser itemBrowser = itemBrowserScene.Instantiate<ItemBrowser>();
    AddChild(itemBrowser);
    itemBrowser.SetSelectionModeOnly();
    itemBrowser.selectedItemButton.Pressed += () => OnRecipeInputSelected(itemBrowser);
    itemBrowser.cancelSelectionButton.Pressed += itemBrowser.QueueFree;
  }

  private void OnRecipeInputSelected(ItemBrowser itemBrowser) {
    Item selectedItem = itemBrowser.GetCurrentItem();
    itemBrowser.QueueFree();
    if (selectedItem == null) {
      return;
    }

    foreach (RequiredItem requiredItem in currentRecipe.requiredSpecificItems) {
      if (requiredItem.requiredItemId == selectedItem.itemId) {
        return;
      }
    }
    RequiredItem newRequiredItem = new RequiredItem();
    newRequiredItem.requiredItemId = selectedItem.itemId;
    currentRecipe.requiredSpecificItems.Add(newRequiredItem);
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnRemoveRecipeRequiredItemBehaviorButtonPressed(ItemBehaviorType itemBehaviorType) {
    for (int i = 0; i < currentRecipe.requiredSpecificItems.Count; i++) {
      if (currentRecipe.requiredItemBehaviors[i].requiredItemBehaviorType == itemBehaviorType) {
        currentRecipe.requiredItemBehaviors.RemoveAt(i);
      }
    }
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnRemoveRecipeInputButtonPressed(string itemId) {
    for (int i = 0; i < currentRecipe.requiredSpecificItems.Count; i++) {
      if (currentRecipe.requiredSpecificItems[i].requiredItemId == itemId) {
        currentRecipe.requiredSpecificItems.RemoveAt(i);
      }
    }
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnRecipeInputItemBehaviorAmountChanged(ItemBehaviorType behaviorType, string newAmount) {
    foreach (RequiredItemBehavior requiredItemBehavior in currentRecipe.requiredItemBehaviors) {
      if (requiredItemBehavior.requiredItemBehaviorType == behaviorType) {
        Int32.TryParse(newAmount, out requiredItemBehavior.itemAmount);
      }
    }
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnRecipeInputItemAmountChanged(string itemId, string newAmount) {
    foreach (RequiredItem requiredItem in currentRecipe.requiredSpecificItems) {
      if (requiredItem.requiredItemId == itemId) {
        Int32.TryParse(newAmount, out requiredItem.itemAmount);
      }
    }
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnAddRecipeOutputButtonPressed() {
    ItemBrowser itemBrowser = itemBrowserScene.Instantiate<ItemBrowser>();
    AddChild(itemBrowser);
    itemBrowser.SetSelectionModeOnly();
    itemBrowser.selectedItemButton.Pressed += () => OnRecipeOutputSelected(itemBrowser);
    itemBrowser.cancelSelectionButton.Pressed += itemBrowser.QueueFree;
  }

  private void OnRecipeOutputSelected(ItemBrowser itemBrowser) {
    Item selectedItem = itemBrowser.GetCurrentItem();
    itemBrowser.QueueFree();
    if (selectedItem == null) {
      return;
    }

    foreach (OutputItem outputItem in currentRecipe.outputItems) {
      if (outputItem.outputItemId == selectedItem.itemId) {
        return;
      }
    }
    OutputItem newOutputItem = new OutputItem();
    newOutputItem.outputItemId = selectedItem.itemId;
    currentRecipe.outputItems.Add(newOutputItem);
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnRemoveRecipeOutputButtonPressed(string itemId) {
    for (int i = 0; i < currentRecipe.outputItems.Count; i++) {
      if (currentRecipe.outputItems[i].outputItemId == itemId) {
        currentRecipe.outputItems.RemoveAt(i);
      }
    }
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnRecipeOutputItemAmountChanged(string itemId, string newAmount) {
    foreach (OutputItem outputItem in currentRecipe.outputItems) {
      if (outputItem.outputItemId == itemId) {
        Int32.TryParse(newAmount, out outputItem.itemAmount);
      }
    }
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnRecipeOutputProbabilityChanged(string itemId, double newProbability, Label label) {
    // We don't call UpdateAllControlsForCurrentRecipe for sliders because otherwise it stops
    // the smooth moving of the slider. So, we manually set the label instead of relying on
    // the broader function, which would be nicer.
    foreach (OutputItem outputItem in currentRecipe.outputItems) {
      if (outputItem.outputItemId == itemId) {
        outputItem.outputProbability = (int) newProbability;
        label.Text = outputItem.outputProbability + "%";
      }
    }
    SetValuesChangedSinceLastSave(true);
  }

  private void OnIconPickerResourceChanged(Resource newResource) {
    if (currentRecipe.recipeIcon == newResource as Texture2D) {
      return;
    }

    currentRecipe.recipeIcon = newResource as Texture2D;
    UpdateAllControlsForCurrentRecipe();
    SetValuesChangedSinceLastSave(true);
  }

  private void OnSaveButtonPressed() {
    Control editorPopup = editorPopupScene.Instantiate<Control>();
    // TODO: Perform further validation and potentially throw error.

    if (recipeId.Text.Length == 0) {
      editorPopup.GetNode<Label>("%PopupMessage").Text = CANT_SAVE_RECIPE_NO_RECIPE_ID;
      editorPopup.GetNode<Button>("%OkButton").Pressed += editorPopup.QueueFree;
      editorPopup.GetNode<Button>("%CancelButton").Visible = false;
      AddChild(editorPopup);
      return;
    }

    string filePath = RECIPE_FILES_BASE_PATH + recipeId.Text + ".tres";
    bool resourceAlreadyExists = ResourceLoader.Exists(filePath);
    editorPopup.GetNode<Label>("%PopupMessage").Text = String.Format(
      resourceAlreadyExists ? OVERWRITE_FILE_MESSAGE : SAVE_NEW_FILE_MESSAGE,
      filePath
    );
    editorPopup.GetNode<Button>("%OkButton").Pressed += () => OnSaveConfirmed(editorPopup);
    editorPopup.GetNode<Button>("%CancelButton").Pressed += editorPopup.QueueFree;
    AddChild(editorPopup);
  }

  private void OnSaveConfirmed(Control editorPopup) {
    ResourceSaver.Save(currentRecipe, RECIPE_FILES_BASE_PATH + recipeId.Text + ".tres");
    SetValuesChangedSinceLastSave(false);
    editorPopup.QueueFree();
  }
}
