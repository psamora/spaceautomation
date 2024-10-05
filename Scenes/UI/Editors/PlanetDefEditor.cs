using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

[Tool]
public partial class PlanetDefEditor : Control {
  private const string PLANET_DEF_FILES_BASE_PATH = "res://Resources/Space/PlanetDefs/";
  private const string UNSAVED_CHANGES =
    "You have unsaved changed on the current Planet. Are you sure you want to proceed?";
  private const string SAVE_NEW_PLANET_MESSAGE =
    "You are creating a new Planet in the following path: {0}. "
    + "If this planet name has changed, remember to delete the old file manually.";
  private const string OVERWRITE_PLANET_MESSAGE =
    "You are overwriting the Planet in the following path: {0}. "
    + "You can not recover the old content so make sure you are right before continuing.";
  private const string CANT_SAVE_PLANET_NO_PLANET_ID =
    "Can't save this planet due to the lack of a planet-id. "
    + "Please generate one by setting Planet Name";

  [Export] PackedScene editorPopupScene;

  // Top Menu
  [Export] Control topBar;
  [Export] Button addNewItemButton;
  [Export] Button backwardsButton;
  [Export] Button forwardsButton;
  [Export] Button exitButton;

  // Planet Render and Editor
  [Export] LineEdit planetName;
  [Export] LineEdit planetId;
  [Export] Control planetRendererView;
  [Export] Control planetEditorView;
  [Export] PlanetRenderer planetRenderer;
  [Export] VBoxContainer planetDefinitionEditor;
  [Export] PlanetLayerTextureEditor terrainTextureEditor;
  [Export] PlanetLayerTextureEditor cloudTextureEditor;
  [Export] Button saveButton;

  // Search Bar
  [Export] LineEdit searchBar;
  [Export] Control searchTab;
  [Export] PackedScene searchResultScene;

  [Export] Label unsavedChangesLabel;

  private Dictionary<string, PlanetDef> planetDefIdToPlanetDefMap = new Dictionary<string, PlanetDef>();
  private PlanetDef currentPlanetDef;
  private bool valueChangedSinceLastSave;


  public override void _Ready() {
    currentPlanetDef = null;

    UpdateAllControlsForCurrentPlanet();
    SetValuesChangedSinceLastSave(false);

    PopulatePlanetDictionary();
    SearchForPlanetWithText("");

    searchBar.TextChanged += OnSearchBarTextChanged;
    addNewItemButton.Pressed += OnAddNewPlanetDefPressed;
    planetName.TextChanged += OnPlanetNameTextChanged;
    saveButton.Pressed += OnSaveButtonPressed;
    terrainTextureEditor.PlanetLayerChanged += OnPlanetLayerChanged;
  }

  private void OnPlanetLayerChanged() {
    planetRenderer.DisplayPlanetDef(currentPlanetDef);
  }


  private void UpdateAllControlsForCurrentPlanet() {
    if (currentPlanetDef == null) {
      planetRendererView.Visible = false;
      planetEditorView.Visible = false;
      return;
    }

    planetRendererView.Visible = true;
    planetEditorView.Visible = true;
    planetRenderer.DisplayPlanetDef(currentPlanetDef);
    terrainTextureEditor.SetLayer(currentPlanetDef.terrainLayer);
    cloudTextureEditor.SetLayer(currentPlanetDef.cloudLayer);
  }

  private void PopulatePlanetDictionary() {
    DirAccess dirAccess = DirAccess.Open(PLANET_DEF_FILES_BASE_PATH);
    string[] files = dirAccess.GetFiles();
    foreach (string fileName in files) {
      if (!fileName.Contains(".tres")) {
        continue;
      }
      PlanetDef loadedPlanet = GD.Load<PlanetDef>(PLANET_DEF_FILES_BASE_PATH + fileName);
      planetDefIdToPlanetDefMap[loadedPlanet.ResourceName] = loadedPlanet;
    }
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
  }

  private void SetValuesChangedSinceLastSave(bool changedSinceLastSave) {
    valueChangedSinceLastSave = changedSinceLastSave;
    unsavedChangesLabel.Visible = changedSinceLastSave;
  }

  private void OnAddNewPlanetDefPressed() {
    currentPlanetDef = new PlanetDef();
    UpdateAllControlsForCurrentPlanet();
  }

  private void OnSearchBarTextChanged(string newText) {
    SearchForPlanetWithText(newText);
  }

  private void SearchForPlanetWithText(string text) {
    PopulatePlanetDictionary();
    foreach (Node child in searchTab.GetChildren()) {
      if (child.Name != "SearchBar") {
        child.QueueFree();
      }
    }
    foreach (PlanetDef planetDef in planetDefIdToPlanetDefMap.Values) {
      if (planetDef.planetTypeName.Contains(text) || planetDef.planetTypeName.Contains(text)) {
        Button searchResult = searchResultScene.Instantiate<Button>();
        searchResult.Text = planetDef.planetTypeName;
        searchResult.Pressed += () => OnSelectSearchResult(planetDef);
        searchTab.AddChild(searchResult);
      }
    }
  }

  private void OnSelectSearchResult(PlanetDef planetDef) {
    if (!valueChangedSinceLastSave) {
      currentPlanetDef = planetDef;
      UpdateAllControlsForCurrentPlanet();
      return;
    }

    Control editorPopup = editorPopupScene.Instantiate<Control>();
    editorPopup.GetNode<Label>("%PopupMessage").Text = UNSAVED_CHANGES;
    editorPopup.GetNode<Button>("%OkButton").Pressed += () => {
      OnSelectSearchResultConfirmed(planetDef);
      editorPopup.QueueFree();
    };
    editorPopup.GetNode<Button>("%CancelButton").Pressed += editorPopup.QueueFree;
    AddChild(editorPopup);
  }

  private void OnSelectSearchResultConfirmed(PlanetDef planetDef) {
    currentPlanetDef = planetDef;
    UpdateAllControlsForCurrentPlanet();
  }

  private void OnSaveButtonPressed() {
    Control editorPopup = editorPopupScene.Instantiate<Control>();
    // TODO: Perform further validation and potentially throw error.

    if (planetId.Text.Length == 0) {
      editorPopup.GetNode<Label>("%PopupMessage").Text = CANT_SAVE_PLANET_NO_PLANET_ID;
      editorPopup.GetNode<Button>("%OkButton").Pressed += editorPopup.QueueFree;
      editorPopup.GetNode<Button>("%CancelButton").Visible = false;
      AddChild(editorPopup);
      return;
    }

    string filePath = PLANET_DEF_FILES_BASE_PATH + planetId.Text + ".tres";
    bool resourceAlreadyExists = ResourceLoader.Exists(filePath);
    editorPopup.GetNode<Label>("%PopupMessage").Text = String.Format(
      resourceAlreadyExists ? OVERWRITE_PLANET_MESSAGE : SAVE_NEW_PLANET_MESSAGE,
      filePath
    );
    editorPopup.GetNode<Button>("%OkButton").Pressed += () => OnSaveConfirmed(editorPopup);
    editorPopup.GetNode<Button>("%CancelButton").Pressed += editorPopup.QueueFree;
    AddChild(editorPopup);
  }

  private void OnSaveConfirmed(Control editorPopup) {
    ResourceSaver.Save(currentPlanetDef, PLANET_DEF_FILES_BASE_PATH + planetId.Text + ".tres");
    SetValuesChangedSinceLastSave(false);
    editorPopup.QueueFree();
  }

  private void OnPlanetNameTextChanged(string newText) {
    if (currentPlanetDef.planetTypeName == newText) {
      return;
    }

    currentPlanetDef.planetTypeName = newText;
    currentPlanetDef.planetTypeId = Regex.Replace(
      newText.ToLower().Replace(" ", "-"), "[^A-Za-z0-9-]", "");
    planetId.Text = currentPlanetDef.planetTypeId;
    SetValuesChangedSinceLastSave(true);
  }
}
