using Godot;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public partial class UniverseCamera : Camera2D {
  private const float MIN_ZOOM = 0.2f;
  private const float MAX_ZOOM = 2f;
  private const float ZOOM_INCREMENT = 0.1f;
  private const float ZOOM_RATE = 8f;

  private float targetZoom = MAX_ZOOM;
  private Tween tween;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    tween = GetTree().CreateTween();
  }

  public override void _PhysicsProcess(double delta) {
    Zoom = Zoom.Lerp(targetZoom * Vector2.One, ZOOM_RATE * (float) delta);
    SetPhysicsProcess(!Mathf.IsEqualApprox(Zoom.X, targetZoom));
  }

  public override void _UnhandledInput(InputEvent inputEvent) {
    InputEventMouseButton mouseButtonEvent = inputEvent as InputEventMouseButton;
    if (mouseButtonEvent != null && mouseButtonEvent.IsPressed()) {
      if (mouseButtonEvent.ButtonIndex == MouseButton.WheelDown) {
        targetZoom = Mathf.Max(targetZoom - ZOOM_INCREMENT, MIN_ZOOM);
        SetPhysicsProcess(true);
      } else if (mouseButtonEvent.ButtonIndex == MouseButton.WheelUp) {
        targetZoom = Mathf.Min(targetZoom + ZOOM_INCREMENT, MAX_ZOOM);
        SetPhysicsProcess(true);
      } else if (mouseButtonEvent.DoubleClick) {
        FocusPosition(GetGlobalMousePosition());
      }
    }

    InputEventMouseMotion mouseMotionEvent = inputEvent as InputEventMouseMotion;
    if (mouseMotionEvent != null && mouseMotionEvent.ButtonMask == MouseButtonMask.Left) {
      Position -= mouseMotionEvent.Relative / Zoom;
    }
  }

  private void FocusPosition(Vector2 targetPosition) {
    tween = GetTree().CreateTween();
    tween.TweenProperty(this, "position", targetPosition, 0.2f).SetTrans(Tween.TransitionType.Expo);
  }
}
