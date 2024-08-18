using Godot;
using System;
using System.Diagnostics;
using System.Linq;

public partial class Inserter : PlacedEntity {
  // TODO: replace with item system
  [Export] private Texture2D coalOreTexture;
  [Export] private Texture2D ironOreTexture;
  [Export] private Texture2D copperOreTexture;

  public int xPos;
  public int yPos;
  public Direction direction;
  private AnimationPlayer animationPlayer;
  private Sprite2D heldItemSprite;
  private int maxNumItemsToGrab = 2;

  // TODO: what about drop on ground?
  private PlacedEntity sourceEntity;
  private PlacedEntity destinationEntity;

  private TempItem heldItem = TempItem.NONE;
  private int numOfHeldItems;
  private bool isHoldingItem;
  private bool isHeldItemWaitingForOutput;


  // Called when the node enters the scene tree for the first time.
  public override void _Ready() {
    ProcessPriority = 1;
    animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    animationPlayer.AnimationFinished += OnAnimationFinished;
    heldItemSprite = GetNode<Sprite2D>("%HeldItem");
  }

  // Called every frame. 'delta' is the elapsed time since the previous frame.

  private int frameCount = 0;
  public override void _Process(double delta) {
    if (frameCount % 4 == 0) {
      if (!isHoldingItem) {
        TryToGrabItem();
      }

      if (isHoldingItem && isHeldItemWaitingForOutput) {
        TryToPlaceItem();
      }
    }
    frameCount++;
  }

  public void UpdateDirection(Direction direction, NeighboringEntities<PlacedEntity> placedEntities) {
    animationPlayer.CurrentAnimation = "idle_" + direction.ToString().ToLower();
    this.direction = direction;
    SetUpdatedNeighbors(placedEntities);
  }

  public void Initialize(int xPos, int yPos,
                         Direction direction, NeighboringEntities<PlacedEntity> placedEntities) {
    UpdateDirection(direction, placedEntities);
  }

  public void SetUpdatedNeighbors(NeighboringEntities<PlacedEntity> placedEntities) {
    switch (direction) {
      case Direction.UP:
        sourceEntity = placedEntities.topEntity;
        destinationEntity = placedEntities.bottomEntity;
        break;
      case Direction.DOWN:
        sourceEntity = placedEntities.bottomEntity;
        destinationEntity = placedEntities.topEntity;
        break;
      case Direction.LEFT:
        sourceEntity = placedEntities.leftEntity;
        destinationEntity = placedEntities.rightEntity;
        break;
      case Direction.RIGHT:
        sourceEntity = placedEntities.rightEntity;
        destinationEntity = placedEntities.leftEntity;
        break;
    }
  }

  private void TryToGrabItem() {
    if (sourceEntity == null || destinationEntity == null) {
      // TODO: ground pick up?
      return;
    }

    TempItem itemToGrab = sourceEntity.GetAvailableOutput().Keys.FirstOrDefault<TempItem>(
      key => destinationEntity.GetRequestedItems().ContainsKey(key) ||
      destinationEntity.GetRequestedItems().ContainsKey(TempItem.ALL));
    int numItemsGrabbed = sourceEntity.TakeItems(itemToGrab, maxNumItemsToGrab);
    if (itemToGrab == TempItem.NONE || numItemsGrabbed == 0) {
      return;
    }

    isHoldingItem = true;
    heldItem = itemToGrab;
    numOfHeldItems = numItemsGrabbed;
    animationPlayer.CurrentAnimation = "swing_up";

    heldItemSprite.Texture =
      itemToGrab == TempItem.IRON ? ironOreTexture :
      itemToGrab == TempItem.COPPER ? copperOreTexture :
      coalOreTexture;
    heldItemSprite.Visible = true;
  }

  private void TryToPlaceItem() {
    if (destinationEntity == null) {
      // TODO: ground drop?
      return;
    }

    bool useTopLine = true;
    if (destinationEntity as Belt != null) {
      useTopLine = (destinationEntity as Belt).GetFarthestTransportLineComingFromDirection(
        direction).isTopTransportLine;
    }
    bool itemDropped = destinationEntity.PlaceItems(heldItem, numOfHeldItems, useTopLine);
    if (!itemDropped) {
      return;
    }
    isHoldingItem = false;
    isHeldItemWaitingForOutput = false;
    heldItem = TempItem.NONE;
    animationPlayer.CurrentAnimation = "idle_" + direction.ToString().ToLower();
    heldItemSprite.Texture = null;
  }

  private void OnAnimationFinished(StringName animName) {
    if (isHoldingItem && !isHeldItemWaitingForOutput) {
      isHeldItemWaitingForOutput = true;
    }
  }
}
