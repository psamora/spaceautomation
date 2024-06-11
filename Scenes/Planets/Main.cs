using Godot;
using System;
using System.ComponentModel;

public partial class Main : Node2D {

  [Export]
  private BeltManager beltManager;

  private Direction direction = Direction.UP;

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
  }
}
