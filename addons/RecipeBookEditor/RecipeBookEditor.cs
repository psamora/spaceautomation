#if TOOLS
using Godot;
using System;

[Tool]
public partial class RecipeBookEditor : EditorPlugin {
  PackedScene MainPanel = ResourceLoader.Load<PackedScene>("res://Scenes/UI/Browsers/RecipeBook.tscn");
  Control MainPanelInstance;

  public override void _EnterTree() {
    MainPanelInstance = (Control) MainPanel.Instantiate();
    // Add the main panel to the editor's main viewport.
    EditorInterface.Singleton.GetEditorMainScreen().AddChild(MainPanelInstance);
    // Hide the main panel. Very much required.
    _MakeVisible(false);
  }

  public override void _ExitTree() {
    if (MainPanelInstance != null) {
      MainPanelInstance.QueueFree();
    }
  }

  public override bool _HasMainScreen() {
    return true;
  }

  public override void _MakeVisible(bool visible) {
    if (MainPanelInstance != null) {
      MainPanelInstance.Visible = visible;
    }
  }

  public override string _GetPluginName() {
    return "Recipe Book";
  }

  public override Texture2D _GetPluginIcon() {
    return EditorInterface.Singleton.GetEditorTheme().GetIcon("Search", "EditorIcons");
  }
}
#endif
