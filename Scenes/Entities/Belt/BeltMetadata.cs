using Godot;
using System;
using System.Linq;

public partial class BeltMetadata : Sprite2D {
  private const int BELT_SLOTS = 4;
  private Vector2[] topBeltSlotLocalPositions = null;
  private Vector2[] bottomBeltSlotLocalPositions = null;

  public void Initialize(BeltOrientation beltOrientation) {
    Node2D slotsTemplateToUse;
    switch (beltOrientation) {
      case BeltOrientation.RIGHT:
      case BeltOrientation.LEFT:
        slotsTemplateToUse = GetNode<Node2D>("RightSlots");
        break;
      case BeltOrientation.UP:
      case BeltOrientation.DOWN:
        slotsTemplateToUse = GetNode<Node2D>("UpSlots");
        break;
      default:
        slotsTemplateToUse = GetNode<Node2D>("RightDownSlots");
        break;
    }

    float rotationAngle;
    switch (beltOrientation) {
      case BeltOrientation.DOWN_LEFT:
      case BeltOrientation.RIGHT_UP:
        rotationAngle = 90f;
        break;
      case BeltOrientation.LEFT_UP:
      case BeltOrientation.DOWN_RIGHT:
        rotationAngle = 180f;
        break;
      case BeltOrientation.UP_RIGHT:
      case BeltOrientation.LEFT_DOWN:
        rotationAngle = 270f;
        break;
      default:
        rotationAngle = 0f;
        break;
    }

    bool flipTopBottomStartEnd;
    switch (beltOrientation) {
      case BeltOrientation.LEFT:
      case BeltOrientation.DOWN:
      case BeltOrientation.RIGHT_UP:
      case BeltOrientation.LEFT_DOWN:
      case BeltOrientation.UP_LEFT:
      case BeltOrientation.DOWN_RIGHT:
        flipTopBottomStartEnd = true;
        break;
      default:
        flipTopBottomStartEnd = false;
        break;
    }

    BuildLocalPositionArrayForSlots(
      slotsTemplateToUse, rotationAngle, flipTopBottomStartEnd);
  }

  public Vector2[] GetTopBeltWorldPositions(Vector2 globalOffset) {
    if (topBeltSlotLocalPositions == null) {
      throw new ApplicationException($"Assert Failed: BeltMetadata not initialized before usage.");
    }
    return topBeltSlotLocalPositions.Select(x => x + globalOffset).ToArray();
  }

  public Vector2[] GetBottomBeltWorldPositions(Vector2 globalOffset) {
    if (bottomBeltSlotLocalPositions == null) {
      throw new ApplicationException($"Assert Failed: BeltMetadata not initialized before usage.");
    }
    return bottomBeltSlotLocalPositions.Select(x => x + globalOffset).ToArray();
  }

  private void BuildLocalPositionArrayForSlots(
    Node2D slotsTemplateToUse,
    float rotationAngle,
    bool flipTopBottomStartEnd) {
    topBeltSlotLocalPositions = new Vector2[BELT_SLOTS];
    bottomBeltSlotLocalPositions = new Vector2[BELT_SLOTS];

    slotsTemplateToUse.Rotation = 0;
    slotsTemplateToUse.Rotate(Mathf.DegToRad(rotationAngle));
    Node2D firstSlotTop = slotsTemplateToUse.GetNode<Node2D>("FirstSlotTop");
    Node2D lastSlotTop = slotsTemplateToUse.GetNode<Node2D>("LastSlotTop");
    Node2D firstSlotBottom = slotsTemplateToUse.GetNode<Node2D>("FirstSlotBottom");
    Node2D lastSlotBottom = slotsTemplateToUse.GetNode<Node2D>("LastSlotBottom");

    if (flipTopBottomStartEnd) {
      SwapLocations(firstSlotTop, lastSlotBottom);
      SwapLocations(lastSlotTop, firstSlotBottom);
    }

    Vector2 topSlotStep = (lastSlotTop.GlobalPosition - firstSlotTop.GlobalPosition) / BELT_SLOTS;
    Vector2 bottomSlotStep = (lastSlotBottom.GlobalPosition - firstSlotBottom.GlobalPosition) / BELT_SLOTS;
    for (int i = 0; i < BELT_SLOTS; i++) {
      topBeltSlotLocalPositions[i] = firstSlotTop.GlobalPosition + (i * topSlotStep);
      bottomBeltSlotLocalPositions[i] = firstSlotBottom.GlobalPosition + (i * bottomSlotStep);
    }
  }

  private void SwapLocations(Node2D src, Node2D dst) {
    Vector2 temp = src.Position;
    src.Position = dst.Position;
    dst.Position = temp;
  }
}
