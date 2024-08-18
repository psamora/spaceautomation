using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public partial class Belt : PlacedEntity {
  [Export] PackedScene beltMetadataScene;
  // todo: refactor
  private static Dictionary<BeltOrientation, BeltMetadata> orientationToBeltMetadata =
    new Dictionary<BeltOrientation, BeltMetadata>();

  // TODO: revisit visibility/getts
  public int xPos;
  public int yPos;
  public Direction placedDirection;
  public BeltOrientation beltOrientation;
  private TransportLine topTransportLine;
  private TransportLine bottomTransportLine;

  private Vector2 beltCornerWorldPosition;
  private Vector2[] topBeltSlotWorldPositions;
  private Vector2[] bottomBeltSlotWorldPositions;
  private AnimationPlayer animationPlayer;
  private Timer beltSyncTimer;

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

    horizontalDebugLine = GetNode<Line2D>("HorizontalDebugLine");
    verticalDebugLine = GetNode<Line2D>("VerticalDebugLine");
    diagonalDebugLineAnchor = GetNode<Node2D>("DiagonalDebugLineAnchor");
    diagonalDebugLine = GetNode<Line2D>("DiagonalDebugLineAnchor/DiagonalDebugLine");
  }

  public void Place(int xPos, int yPos) {
    this.xPos = xPos;
    this.yPos = yPos;
  }

  public TransportLine GetTransportLine(bool getTopTransportLine) {
    if (getTopTransportLine) {
      return topTransportLine;
    }
    return bottomTransportLine;
  }

  public TransportLine GetNearestTransportLineComingFromDirection(Direction otherDirection) {
    switch (otherDirection) {
      case Direction.DOWN:
        if (beltOrientation == BeltOrientation.LEFT) {
          return bottomTransportLine;
        } else {
          return topTransportLine;
        }
      case Direction.UP:
        if (beltOrientation == BeltOrientation.RIGHT) {
          return bottomTransportLine;
        } else {
          return topTransportLine;
        }
      case Direction.LEFT:
        if (beltOrientation == BeltOrientation.UP) {
          return bottomTransportLine;
        } else {
          return topTransportLine;
        }
      case Direction.RIGHT:
        if (beltOrientation == BeltOrientation.DOWN) {
          return bottomTransportLine;
        } else {
          return topTransportLine;
        }
      default:
        return topTransportLine;
    }
  }

  public TransportLine GetFarthestTransportLineComingFromDirection(Direction otherDirection) {
    if (GetNearestTransportLineComingFromDirection(otherDirection) == topTransportLine) {
      return bottomTransportLine;
    }
    return topTransportLine;
  }

  public void SetTransportLine(TransportLine transportLine, bool isTopTransportLine) {
    if (isTopTransportLine) {
      topTransportLine = transportLine;
      horizontalDebugLine.DefaultColor = topTransportLine.GetDebugColor();
      verticalDebugLine.DefaultColor = topTransportLine.GetDebugColor();
      diagonalDebugLine.DefaultColor = topTransportLine.GetDebugColor();
    } else {
      bottomTransportLine = transportLine;
    }
  }

  public override Dictionary<TempItem, int> GetRequestedItems() {
    return new Dictionary<TempItem, int>() { { TempItem.ALL, 4 } };
  }

  public override Dictionary<TempItem, int> GetAvailableOutput() {
    return new Dictionary<TempItem, int> { };
  }

  public override bool PlaceItems(TempItem itemType, int amount, bool topIfApplicable) {
    if (topIfApplicable) {
      return topTransportLine.MaybePlaceItemInBelt(itemType, this);
    }
    return bottomTransportLine.MaybePlaceItemInBelt(itemType, this);
  }

  public override int TakeItems(TempItem itemType, int maxAmount) {
    return 1;
  }

  public void UpdateBeltOrientation(BeltOrientation beltOrientation) {
    this.beltOrientation = beltOrientation;
    StartBeltAnimation();
    PopulateBeltSlotWorldPositions();
    DrawDebugLine();
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
    BeltMetadata beltMetadata = orientationToBeltMetadata.GetOrNull(beltOrientation);
    if (beltMetadata == null) {
      beltMetadata = beltMetadataScene.Instantiate<BeltMetadata>();
      beltMetadata.Initialize(beltOrientation);
      GetParent().AddChild(beltMetadata);
      orientationToBeltMetadata[beltOrientation] = beltMetadata;
    }
    topBeltSlotWorldPositions = beltMetadata.GetTopBeltWorldPositions(GlobalPosition);
    bottomBeltSlotWorldPositions = beltMetadata.GetBottomBeltWorldPositions(GlobalPosition);
  }

  public override string ToString() {
    return $"[{xPos}, {yPos}]";
  }


  private void DrawDebugLine() {
    horizontalDebugLine.Visible = false;
    verticalDebugLine.Visible = false;
    diagonalDebugLine.Visible = false;

    Line2D debugLineToUse;
    switch (beltOrientation) {
      case BeltOrientation.RIGHT:
      case BeltOrientation.LEFT:
        debugLineToUse = horizontalDebugLine;
        break;
      case BeltOrientation.UP:
      case BeltOrientation.DOWN:
        debugLineToUse = verticalDebugLine;
        break;
      default:
        debugLineToUse = diagonalDebugLine;
        break;
    }
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

    if (debugLineToUse == diagonalDebugLine) {
      diagonalDebugLineAnchor.Rotation = 0;
      diagonalDebugLineAnchor.Rotate(Mathf.DegToRad(rotationAngle));
    }
  }
}
