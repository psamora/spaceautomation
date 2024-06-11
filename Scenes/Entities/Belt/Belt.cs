using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class Belt : PlacedEntity {
  private const int BELT_SLOTS = 4;

  // TODO: revisit visibility/getts
  public int xPos;
  public int yPos;
  public Direction placedDirection;
  public BeltOrientation beltOrientation;
  private TransportLine curTransportLine;

  private Vector2 beltCornerWorldPosition;
  private Vector2[] topBeltSlotWorldPositions = new Vector2[BELT_SLOTS];
  private Vector2[] bottomBeltSlotWorldPositions = new Vector2[BELT_SLOTS];
  private AnimationPlayer animationPlayer;
  private Timer beltSyncTimer;

  private Node2D rightSlots;
  private Node2D upSlots;
  private Node2D rightDownSlots;

  private Line2D horizontalDebugLine;
  private Line2D verticalDebugLine;
  private Node2D diagonalDebugLineAnchor;
  private Line2D diagonalDebugLine;
  private Dictionary<TempItem, int> BELT_REQUESTED_ITEMS = new Dictionary<TempItem, int>{
    { TempItem.ALL, 4}
  };

  public override void _Ready() {
    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    beltSyncTimer = GetTree().GetFirstNodeInGroup("BeltTimer") as Timer;
    beltCornerWorldPosition = Position + new Vector2(16, 16);

    rightSlots = GetNode<Node2D>("RightSlots");
    upSlots = GetNode<Node2D>("UpSlots");
    rightDownSlots = GetNode<Node2D>("RightDownSlots");

    horizontalDebugLine = GetNode<Line2D>("HorizontalDebugLine");
    verticalDebugLine = GetNode<Line2D>("VerticalDebugLine");
    diagonalDebugLineAnchor = GetNode<Node2D>("DiagonalDebugLineAnchor");
    diagonalDebugLine = GetNode<Line2D>("DiagonalDebugLineAnchor/DiagonalDebugLine");
  }

  public void Place(int xPos, int yPos) {
    this.xPos = xPos;
    this.yPos = yPos;
  }

  public TransportLine GetTransportLine() {
    return curTransportLine;
  }

  public void SetTransportLine(TransportLine transportLine) {
    curTransportLine = transportLine;
    horizontalDebugLine.DefaultColor = curTransportLine.GetDebugColor();
    verticalDebugLine.DefaultColor = curTransportLine.GetDebugColor();
    diagonalDebugLine.DefaultColor = curTransportLine.GetDebugColor();
  }

  public override Dictionary<TempItem, int> GetRequestedItems() {
    return new Dictionary<TempItem, int>() { { TempItem.ALL, 4 } };
  }

  public override Dictionary<TempItem, int> GetAvailableOutput() {
    return new Dictionary<TempItem, int> { };
  }

  public override bool PlaceItems(TempItem itemType, int amount) {
    return curTransportLine.MaybePlaceItemInBelt(itemType, this);
  }

  public override int TakeItems(TempItem itemType, int maxAmount) {
    return 1;
  }

  public void UpdateBeltOrientation(BeltOrientation beltOrientation) {
    this.beltOrientation = beltOrientation;
    StartBeltAnimation();
    // TODO: if we decide the fn below is too costly/storage wasting to run every belt change, we
    // make the position belt positions local which makes it exactly to the same for all
    // belts with the same BeltOrientation. But then we need to compute world positions all the
    // time which may also not be a big deal. Requires profiling as always, my guess is this
    // current behavior is better.
    PopulateBeltSlotWorldPositions();
  }

  public Vector2 GetWorldPositionForTopSlot(int beltSlotIndex) {
    return topBeltSlotWorldPositions[beltSlotIndex];
  }

  public Vector2 GetWorldPositionForBottomSlot(int beltSlotIndex) {
    return bottomBeltSlotWorldPositions[beltSlotIndex];
  }

  private void StartBeltAnimation() {
    animationPlayer.CurrentAnimation = beltOrientation.ToString().ToLower();
    animationPlayer.Seek(beltSyncTimer.WaitTime - beltSyncTimer.TimeLeft);
  }

  private void PopulateBeltSlotWorldPositions() {
    //rightSlots.Visible = false;
    //upSlots.Visible = false;
    //rightDownSlots.Visible = false;
    horizontalDebugLine.Visible = false;
    verticalDebugLine.Visible = false;
    diagonalDebugLine.Visible = false;

    // We have slot location templates for RIGHT, UP and RIGHT_DOWN belt orientations. All others
    // are computed from either rotating the template some amount of degrees and/or by flipping
    // top/bottom and start/end slots before we compute world positions.
    Node2D slotsTemplateToUse;
    Line2D debugLineToUse;
    switch (beltOrientation) {
      case BeltOrientation.RIGHT:
      case BeltOrientation.LEFT:
        slotsTemplateToUse = GetNode<Node2D>("RightSlots");
        debugLineToUse = horizontalDebugLine;
        break;
      case BeltOrientation.UP:
      case BeltOrientation.DOWN:
        slotsTemplateToUse = GetNode<Node2D>("UpSlots");
        debugLineToUse = verticalDebugLine;
        break;
      default:
        slotsTemplateToUse = GetNode<Node2D>("RightDownSlots");
        debugLineToUse = diagonalDebugLine;
        break;
    }

    //slotsTemplateToUse.Visible = true;
    debugLineToUse.Visible = true;

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

    BuildGlobalPositionArrayForSlots(
      slotsTemplateToUse, debugLineToUse, rotationAngle, flipTopBottomStartEnd);
  }

  private void BuildGlobalPositionArrayForSlots(
    Node2D slotsTemplateToUse,
    Line2D debugLineToUse,
    float rotationAngle,
    bool flipTopBottomStartEnd) {
    slotsTemplateToUse.Rotation = 0;
    slotsTemplateToUse.Rotate(Mathf.DegToRad(rotationAngle));
    if (debugLineToUse == diagonalDebugLine) {
      diagonalDebugLineAnchor.Rotation = 0;
      diagonalDebugLineAnchor.Rotate(Mathf.DegToRad(rotationAngle));
    }
    Node2D firstSlotTop = slotsTemplateToUse.GetNode<Node2D>("FirstSlotTop");
    Node2D lastSlotTop = slotsTemplateToUse.GetNode<Node2D>("LastSlotTop");
    Node2D firstSlotBottom = slotsTemplateToUse.GetNode<Node2D>("LastSlotBottom");
    Node2D lastSlotBottom = slotsTemplateToUse.GetNode<Node2D>("FirstSlotBottom");

    if (flipTopBottomStartEnd) {
      SwapLocations(firstSlotTop, lastSlotBottom);
      SwapLocations(lastSlotTop, firstSlotBottom);
    }

    Vector2 topSlotStep = (lastSlotTop.Position - firstSlotTop.Position) / BELT_SLOTS;
    Vector2 bottomSlotStep = (lastSlotBottom.Position - firstSlotBottom.Position) / BELT_SLOTS;
    for (int i = 0; i < BELT_SLOTS; i++) {
      topBeltSlotWorldPositions[i] = firstSlotTop.GlobalPosition + (i * topSlotStep);
      bottomBeltSlotWorldPositions[i] = firstSlotBottom.GlobalPosition + (i * bottomSlotStep);
    }
  }

  private void SwapLocations(Node2D src, Node2D dst) {
    Vector2 temp = src.Position;
    src.Position = dst.Position;
    dst.Position = temp;
  }

  public override string ToString() {
    return $"[{xPos}, {yPos}]";
  }
}
