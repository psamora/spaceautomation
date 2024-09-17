using Godot;
using System;
using System.ComponentModel;
using System.Diagnostics;

public partial class Main : Node2D {

  [Export]
  private BeltManager beltManager;

  [Export]
  private PackedScene recipeBookScene;

  [Export]
  private PackedScene itemBrowserScene;

  private Direction direction = Direction.UP;
  private Control recipeBook = null;
  private Control itemBrowser = null;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(double delta) {
    if (Input.IsActionJustPressed("rotate_clockwise")) {
      direction = direction.RotateForward();
    }
    if (Input.IsActionJustPressed("rotate_counter_clockwise")) {
      direction = direction.RotateBackward();
    }
    if (Input.IsActionJustPressed("temp_rotate_existing")) {
      Vector2 mousePosition = GetViewport().GetMousePosition();
      Vector2I gridPosition = GetNode<TileMap>("TileMap").LocalToMap(mousePosition);
      beltManager.RotateForward(gridPosition.X, gridPosition.Y);
    }
    if (Input.IsActionJustPressed("mouse_left_click")) {
      Vector2 mousePosition = GetViewport().GetMousePosition();
      Vector2I gridPosition = GetNode<TileMap>("TileMap").LocalToMap(mousePosition);
      beltManager.AddBelt(gridPosition.X, gridPosition.Y, direction);
    }
    if (Input.IsActionJustPressed("mouse_right_click")) {
      Vector2 mousePosition = GetViewport().GetMousePosition();
      Vector2I gridPosition = GetNode<TileMap>("TileMap").LocalToMap(mousePosition);
      beltManager.AddInserter(gridPosition.X, gridPosition.Y, direction);
    }
    if (Input.IsActionJustPressed("mouse_wheel_click")) {
      Vector2 mousePosition = GetViewport().GetMousePosition();
      Vector2I gridPosition = GetNode<TileMap>("TileMap").LocalToMap(mousePosition);
      beltManager.AddSpawnerChest(gridPosition.X, gridPosition.Y, TempItem.COAL);
    }
    if (Input.IsActionJustPressed("escape")) {
      if (recipeBook == null) {
        recipeBook = recipeBookScene.Instantiate<Control>();
        GetNode<CanvasLayer>("CanvasLayer").AddChild(recipeBook);
      }
    }
    if (Input.IsActionJustPressed("i")) {
      if (itemBrowser == null) {
        itemBrowser = itemBrowserScene.Instantiate<Control>();
        GetNode<CanvasLayer>("CanvasLayer").AddChild(itemBrowser);
      }
    }
  }
}
