using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

// TransportLines tracks a collection of belts and the items currently moving around in such belts.
public class TransportLine {
  private const int STEPS_PER_BELT = 4;
  private const int SLOTS_PER_BELT = 4;

  // TODO see visibility
  public bool isTopTransportLine = true;
  private static Random debugColorRandom = new Random();
  private Color debugColor = new Color(1, 1, 1);

  private HashSet<Belt> currentBelts;
  private Node<Belt> beltLinkedListHead = null;
  private Node<Belt> beltLinkedListTail = null;

  private Node<ItemInLine> itemsInLineHead = null;
  private Node<ItemInLine> itemsInLineTail = null;
  private Node<ItemInLine> lastMovingNode = null;

  private TransportLine(bool isTopTransportLine) {
    this.isTopTransportLine = isTopTransportLine;
    debugColor = new Color(
      (float) debugColorRandom.NextDouble(),
      (float) debugColorRandom.NextDouble(),
      (float) debugColorRandom.NextDouble());

    this.currentBelts = new HashSet<Belt>();
  }

  public TransportLine(Belt initialBelt, bool isTopTransportLine) : this(isTopTransportLine) {
    AddFirstBelt(initialBelt);
    initialBelt.SetTransportLine(this, isTopTransportLine);
    currentBelts.Add(initialBelt);
  }

  // This method split one TransportLine on a specific belt. All TransportLines will be new,
  // and it will mutates the incoming line, making it empty. If you're using it I hope
  // you're reading this :)
  public static (TransportLine, TransportLine, TransportLine) SplitTransportLineOnBelt(
      TransportLine line, Belt belt) {
    TransportLine left = new TransportLine(line.isTopTransportLine);
    TransportLine middle = new TransportLine(belt, line.isTopTransportLine);
    TransportLine right = new TransportLine(line.isTopTransportLine);

    // It's possible to the actual split in O(1) just moving pointers around as long as we keep
    // a Belt->BeltNode mapping. But since we have to iterate the transport line contents
    // it's not really a major performance improvement (TBD).
    Node<Belt> curNode = line.beltLinkedListTail;
    SplitPhase splitPhase = SplitPhase.RIGHT;
    int curBeltIndex = 0;
    int cumulativeDistance = 0;
    int totalBeltDistanceInRight = 0;
    int cumulativeDistanceInRight = 0;
    int totalBeltDistanceInMiddle = 0;
    int cumulativeDistanceInMiddle = 0;
    Node<ItemInLine> curItemInLineNode = null;
    Node<ItemInLine> leftItemInLineHead = line.itemsInLineHead;
    Node<ItemInLine> middleItemInLineHead = null;
    Node<ItemInLine> middleItemInLineTail = null;
    Node<ItemInLine> rightItemInLineTail = line.itemsInLineTail;
    while (curNode != null) {
      int curBeltDistanceWindowMax = (STEPS_PER_BELT * curBeltIndex) + STEPS_PER_BELT;
      if (curNode.Value() == belt && splitPhase == SplitPhase.RIGHT) {
        splitPhase = SplitPhase.MIDDLE;
        middleItemInLineTail = curItemInLineNode;
      } else if (curNode.Value() != belt && splitPhase == SplitPhase.MIDDLE) {
        splitPhase = SplitPhase.LEFT;
        middleItemInLineHead = curItemInLineNode?.next;
      }

      switch (splitPhase) {
        case SplitPhase.LEFT:
          left.AddBeltToStart(curNode.Value());
          break;
        case SplitPhase.MIDDLE:
          totalBeltDistanceInMiddle += STEPS_PER_BELT;
          break;
        case SplitPhase.RIGHT:
          right.AddBeltToStart(curNode.Value());
          totalBeltDistanceInRight += STEPS_PER_BELT;
          break;
      }

      curNode = curNode.previous;
      curBeltIndex++;

      // Iterate the item LinkedList until we are out of the current belt and into the previous.
      while (curItemInLineNode != null &&
             cumulativeDistance + curItemInLineNode.Value().distanceToNextItem <= curBeltDistanceWindowMax) {
        cumulativeDistance += curItemInLineNode.Value().distanceToNextItem;
        switch (splitPhase) {
          case SplitPhase.LEFT:
            // not necessary to compute for splitting
            break;
          case SplitPhase.MIDDLE:
            cumulativeDistanceInMiddle += curItemInLineNode.Value().distanceToNextItem;
            break;
          case SplitPhase.RIGHT:
            cumulativeDistanceInRight += curItemInLineNode.Value().distanceToNextItem;
            break;
        }
        curItemInLineNode = curItemInLineNode.previous;
      }
    }

    // Finally, configure the ItemInLine lists for each of the Transport Lines

    // For the first belt, we need to adjust pointers and the distnace of the tail item
    left.itemsInLineHead = leftItemInLineHead;
    left.itemsInLineTail =
      middleItemInLineHead?.previous == null ? left.itemsInLineHead :
      middleItemInLineHead.previous;
    left.lastMovingNode = left.itemsInLineTail;
    if (left.itemsInLineTail != null) {
      left.itemsInLineTail.Value().distanceToNextItem -=
        totalBeltDistanceInRight + totalBeltDistanceInMiddle - totalBeltDistanceInRight - totalBeltDistanceInMiddle;
    }

    // For the middle belt, we need to adjust the pointers and distance of the tail item
    middle.itemsInLineHead = middleItemInLineHead;
    middle.itemsInLineTail =
      middleItemInLineTail == null ? middle.itemsInLineHead :
      middleItemInLineTail;
    middle.lastMovingNode = middle.itemsInLineTail;
    if (middleItemInLineHead != null) {
      middleItemInLineHead.previous = null;
      middleItemInLineTail.next = null;
    }
    if (middleItemInLineTail != null) {
      middleItemInLineTail.Value().distanceToNextItem -=
        totalBeltDistanceInRight - totalBeltDistanceInRight;
    }

    // Right side doesn't require adjusting any distances, only pointers.
    right.itemsInLineTail = rightItemInLineTail;
    right.itemsInLineHead = middleItemInLineTail?.next;
    right.lastMovingNode = right.itemsInLineTail;

    // Reset incoming line, it's empty now.
    line.currentBelts.Clear();
    line.beltLinkedListHead = null;
    line.beltLinkedListTail = null;
    Debug.Print("Split complete:");
    Debug.Print($"{left.ToString()}");
    Debug.Print($"{middle.ToString()}");
    Debug.Print($"{right.ToString()}");
    return (left, middle, right);
  }


  // This method doesn't create new TransportLines, it does an inplace merge, emptying the second
  // in the process. Yeah it's inconsistent with the Split API :).
  // TODO: we could do something smarter to reduce cost of merges if it's better to merge back
  // to front instead of front to back.
  // TODO: make APIs consistent, or just deal with it.
  // TODO: refactor
  public void Merge(TransportLine lineToAppend) {
    if (lineToAppend == this || lineToAppend.IsEmpty()) {
      return;
    }

    // TODO: It's possible to the actual merge in O(1) just moving pointers around as long as we
    // keep a Belt->BeltNode mapping, but it requires tracking the cumulative distance on a
    // TransportLine which is probably doable (track total distance of items added, reduce when)
    // we move stuff forward.
    int numBeltsAddedOnFront = 0;
    Node<Belt> curNode = lineToAppend.beltLinkedListHead;
    while (curNode != null) {
      AddBeltToEnd(curNode.Value());
      curNode = curNode.next;
      numBeltsAddedOnFront++;
    }

    // Clear up the second line.
    lineToAppend.currentBelts.Clear();
    lineToAppend.beltLinkedListHead = null;
    lineToAppend.beltLinkedListTail = null;

    // Now merge the item in line contents. If there's nothing in the first line, just copy
    // the other line, and distances/etc will be correct since we merge front to back.
    Node<ItemInLine> lastItemInFirstLine = itemsInLineTail;
    if (lastItemInFirstLine == null) {
      itemsInLineHead = lineToAppend.itemsInLineHead;
      itemsInLineTail = lineToAppend.itemsInLineTail;
      lastMovingNode = lineToAppend.lastMovingNode;
      lineToAppend.itemsInLineHead = null;
      lineToAppend.itemsInLineTail = null;
      lineToAppend.lastMovingNode = null;
      return;
    }

    Node<ItemInLine> firstItemInSecondLine = lineToAppend.itemsInLineHead;
    int additionalDistanceToFront = numBeltsAddedOnFront * SLOTS_PER_BELT;
    // If there's nothing in the second line, there's nothing to do here other than up update
    // distance.
    if (firstItemInSecondLine == null) {
      itemsInLineTail.Value().distanceToNextItem += additionalDistanceToFront;
      lastMovingNode = itemsInLineTail;
      lineToAppend.itemsInLineHead = null;
      lineToAppend.itemsInLineTail = null;
      lineToAppend.lastMovingNode = null;
      return;
    }

    // Otherwise, merge the two item lists but it will require updating the distance of the
    // items in the first line.
    int distanceToFirstItemInSecondLine = 0;
    Node<ItemInLine> curItemInSecondLine = lineToAppend.itemsInLineTail;
    while (curItemInSecondLine != null) {
      distanceToFirstItemInSecondLine += curItemInSecondLine.Value().distanceToNextItem;
      curItemInSecondLine = curItemInSecondLine.previous;
    }

    itemsInLineTail.Value().distanceToNextItem +=
      additionalDistanceToFront - distanceToFirstItemInSecondLine;
    itemsInLineTail = lineToAppend.itemsInLineTail;
    lastMovingNode = lineToAppend.lastMovingNode;
    lineToAppend.itemsInLineHead = null;
    lineToAppend.itemsInLineTail = null;
    lineToAppend.lastMovingNode = null;
    return;
  }

  // This method tries to add the given item to the given belt in the transport line.
  // If it return false, it means the belt is full or the belt is not part of this transport line.
  public bool MaybePlaceItemInBelt(TempItem item, Belt belt) {
    if (!currentBelts.Contains(belt)) {
      return false;
    }

    Debug.Print($"\nAdding item to TransportLine");
    int curIndex = 0;
    int cumulativeDistance = 0;
    Node<Belt> curBeltNode = beltLinkedListTail;
    Node<ItemInLine> curItemInLineNode = itemsInLineTail;
    Node<ItemInLine> nextItemInLine = null;

    // Then starting both belts + transport line backward (belts first), until we reach the
    // belt/transport lines for the targeted belt, where we will attempt to add an item.
    while (curBeltNode != null) {
      int curBeltDistanceWindowMax = (STEPS_PER_BELT * curIndex) + STEPS_PER_BELT;
      bool isTargetBelt = curBeltNode.Value() == belt;

      // Iterate the item LinkedList until we are out of the current belt and into the previous.
      while (curItemInLineNode != null &&
             cumulativeDistance + curItemInLineNode.Value().distanceToNextItem <= curBeltDistanceWindowMax) {
        cumulativeDistance += curItemInLineNode.Value().distanceToNextItem;
        nextItemInLine = curItemInLineNode;
        curItemInLineNode = curItemInLineNode.previous;
      }

      if (isTargetBelt) {
        Debug.Print(
          $"Adding to BeltIndex: {curIndex}, Cumulative distance {cumulativeDistance}");
        bool result = MaybePlaceItemInBeltSegment(
          item, curIndex, cumulativeDistance, nextItemInLine);
        // Debug.Print($"{this}");
        return result; // todo inline
      }

      curBeltNode = curBeltNode.previous;
      curIndex++;
    }

    throw new ApplicationException($"Couldn't find expected belt in belt LinkedList");
  }

  private bool MaybePlaceItemInBeltSegment(
      TempItem item,
      int beltIndex,
      int cumulativeDistance,
      Node<ItemInLine> nextItemInLine) {
    //Debug.Print($"Current line items for Belt {String.Join(", ", lineItemsForThisBelt)}");

    int beltFirstSlotDistanceIfEmptyLine = (STEPS_PER_BELT * beltIndex) + STEPS_PER_BELT;

    // If there next item in line has a cumulativeDistance that is equal to the end slot
    // for the item we are trying to place, fail to add.
    if (cumulativeDistance == beltFirstSlotDistanceIfEmptyLine) {
      return false;
    }

    // Otherwise, it means we have a item to add and we need to pick
    // 1- what it's distance from the next ItemInLine
    // 2- where in the LinkedList to add it

    // If there's no other item in the whole Transport Line, add it directly in the
    // first slot at the right distance and start the Line of items and we are done.
    if (itemsInLineHead == null) {
      AddFirstTransportLineItem(item, beltFirstSlotDistanceIfEmptyLine);
      Debug.Print($"Case 1: First item in line {itemsInLineTail}");
      return true;
    }

    // Otherwise, if there's an item in line after this one, we will attach ourselves to it
    if (nextItemInLine != null) {
      // Compute the distance from the beginning of this belt to the next item in line.
      int distanceFromNextItem =
        beltFirstSlotDistanceIfEmptyLine - cumulativeDistance;
      nextItemInLine.AddBefore(new ItemInLine(item, distanceFromNextItem));

      // There may be items after this one that require fixing too. We also make sure the
      // rest of the transport line is functional.
      // TODO: refactor, some duplication with below, better varirables, etc
      if (nextItemInLine.previous.previous != null) {
        nextItemInLine.previous.previous.Value().distanceToNextItem -= distanceFromNextItem;
      }
      if (nextItemInLine == itemsInLineHead) {
        itemsInLineHead = nextItemInLine.previous;
      }
      if (lastMovingNode == null) {
        lastMovingNode = nextItemInLine.previous;
      }
      Debug.Print($"Case 2: Item ahead in line {nextItemInLine.previous}");
      return true;
    }

    // Otherwise, there must be an item before us in line. Because we know there's no item next,
    // and no item in this belt, the item before *has* to be the tail. We update its distance.
    itemsInLineTail.AddAfter(new ItemInLine(item, beltFirstSlotDistanceIfEmptyLine));
    itemsInLineTail.Value().distanceToNextItem -= beltFirstSlotDistanceIfEmptyLine;
    itemsInLineTail = itemsInLineTail.next;
    if (lastMovingNode == null) {
      lastMovingNode = itemsInLineTail;
    }
    Debug.Print($"Case 3: Item before in line {itemsInLineTail}");
    return true;
  }

  public Belt GetLastBeltInLine() {
    return beltLinkedListTail.Value();
  }

  public void TryToOutputToNextTransportLine(TransportLine nextTransportLine, Belt facingBelt) {
    if (nextTransportLine == null || facingBelt == null) {
      return;
    }

    Node<ItemInLine> itemToOutput = itemsInLineTail;
    if (itemToOutput == null || itemToOutput.Value().distanceToNextItem != 1) {
      return;
    }

    bool itemOutputSuccessful = nextTransportLine.MaybePlaceItemInBelt(
      itemToOutput.Value().itemType, facingBelt);
    if (!itemOutputSuccessful) {
      return;
    }

    itemsInLineTail = itemToOutput.previous;
    if (itemsInLineTail != null) {
      itemsInLineTail.Value().distanceToNextItem +=
        itemToOutput.Value().distanceToNextItem;
    } else {
      itemsInLineHead = null;
    }
    Debug.Print("Line result after output " + this.ToString());
  }

  public void AdvanceTransportLine() {
    if (lastMovingNode == null) {
      return;
    }

    // Movable items may have been added after the last moved item, let's make sure we move that
    // one.
    while (lastMovingNode.next != null && lastMovingNode.next.Value().distanceToNextItem > 1) {
      lastMovingNode = lastMovingNode.next;
    }

    // Find the first movable item from the lookup place.
    while (lastMovingNode != null && lastMovingNode.Value().distanceToNextItem == 1) {
      lastMovingNode = lastMovingNode.previous;
    }

    if (lastMovingNode != null) {
      lastMovingNode.Value().distanceToNextItem--;
    }
  }

  public List<ItemToDraw> GetItemsToDraw(Rect2 currentView) {
    // TODO: we can likely optimize this by reutilizing lists/values etc. profiling 
    // also reminder one may be inclined to use a Dict from itemType -> position here but we have
    // to iterate it anyway in the caller so don't bother, probably costs more due to overhead
    List<ItemToDraw> itemsToDraw = new List<ItemToDraw>();
    int curIndex = 0;

    int cumulativeDistance = 0;
    Node<Belt> curBeltNode = beltLinkedListTail;
    Node<ItemInLine> curItemInLineNode = itemsInLineTail;

    // Iterate both the Belt and ItemInLine linked list together. It likely doesn't matter
    // which one you use, so we iterate the belt one for consistenct with previous code.
    while (curBeltNode != null) {
      int curBeltDistanceWindowMax = (STEPS_PER_BELT * curIndex) + STEPS_PER_BELT;

      // If the current item loop is not in the belt, also move the item node backwards so that
      // by the next loop
      // Iterate the items in line until we are either done with the items in line
      while (curItemInLineNode != null &&
             cumulativeDistance + curItemInLineNode.Value().distanceToNextItem <= curBeltDistanceWindowMax) {
        cumulativeDistance += curItemInLineNode.Value().distanceToNextItem;
        int slotForItem = curBeltDistanceWindowMax - cumulativeDistance;
        Godot.Vector2 worldPosition = isTopTransportLine ?
          curBeltNode.Value().GetWorldPositionForTopSlot(slotForItem) :
          curBeltNode.Value().GetWorldPositionForBottomSlot(slotForItem);
        itemsToDraw.Add(
          new ItemToDraw(curItemInLineNode.Value().itemType, worldPosition));
        curItemInLineNode = curItemInLineNode.previous;
      }
      curBeltNode = curBeltNode.previous;
      curIndex++;
    }
    return itemsToDraw;
  }

  public Color GetDebugColor() {
    return debugColor;
  }

  public int Size() {
    return currentBelts.Count;
  }
  public bool IsEmpty() {
    return currentBelts.Count == 0;
  }

  // All adding and removing APIs are private on purpose. Only external operation allowed on
  // transport lines should be creating a new one, splitting one or merging two.
  // If we need to make this public it is possible a bad sign the abstraction is falling apart.
  private void AddBeltToEnd(Belt belt) {
    if (currentBelts.Contains(belt)) {
      return;
    }
    belt.SetTransportLine(this, isTopTransportLine);
    currentBelts.Add(belt);

    if (beltLinkedListTail == null) {
      AddFirstBelt(belt);
    } else {
      beltLinkedListTail = beltLinkedListTail.AddAfter(belt);
    }
  }

  private void AddBeltToStart(Belt belt) {
    if (currentBelts.Contains(belt)) {
      return;
    }
    belt.SetTransportLine(this, isTopTransportLine);
    currentBelts.Add(belt);

    if (beltLinkedListHead == null) {
      AddFirstBelt(belt);
    } else {
      beltLinkedListHead = beltLinkedListHead.AddBefore(belt);
    }
  }

  private void AddFirstBelt(Belt belt) {
    this.beltLinkedListHead = new Node<Belt>(belt);
    this.beltLinkedListTail = beltLinkedListHead;
  }

  private void AddFirstTransportLineItem(TempItem item, int distance) {
    this.itemsInLineHead = new Node<ItemInLine>(new ItemInLine(item, distance));
    this.itemsInLineTail = itemsInLineHead;
    this.lastMovingNode = this.itemsInLineHead;
  }

  public class ItemToDraw {
    public TempItem itemType;
    public Godot.Vector2 worldPosition;

    public ItemToDraw(TempItem itemType, Godot.Vector2 worldPosition) {
      this.itemType = itemType;
      this.worldPosition = worldPosition;
    }

    public override string ToString() {
      return $"<type: {itemType}, worldPosition: {worldPosition}>";
    }
  }

  private class ItemInLine {
    public TempItem itemType;
    public int distanceToNextItem;

    public ItemInLine(TempItem itemType, int distanceToNextItem) {
      this.itemType = itemType;
      this.distanceToNextItem = distanceToNextItem;
    }

    public override string ToString() {
      return $"<type: {itemType}, distance: {distanceToNextItem}>";
    }
  }

  // TODO: Because these LinkedLists will have limited sizes, we could consider an optimization
  // where we preallocate X node slots in an Array so we enjoy caching at the cost of maybe 
  // negligible memory usage. Very late game type of optimization, but adding here for posteriority
  private class Node<T> {
    private T value;
    public Node<T> next;
    public Node<T> previous;

    public Node(T value) {
      this.value = value;
    }

    public T Value() {
      return value;
    }

    public Node<T> AddBefore(T value) {
      Node<T> newNode = new Node<T>(value);
      newNode.previous = this.previous;
      newNode.next = this;
      if (this.previous != null) {
        this.previous.next = newNode;
      }
      this.previous = newNode;
      return newNode;
    }

    public Node<T> AddAfter(T value) {
      Node<T> newNode = new Node<T>(value);
      newNode.previous = this;
      newNode.next = this.next;
      if (this.next != null) {
        this.next.previous = newNode;
      }
      this.next = newNode;
      return newNode;
    }

    public override string ToString() {
      return value.ToString() + " -> ";
    }
  }

  public override string ToString() {
    Node<Belt> curBeltNode = beltLinkedListHead;
    StringBuilder sb = new StringBuilder();
    int numOfSlots = 0;
    while (curBeltNode != null) {
      sb.Append("|<<<<");
      curBeltNode = curBeltNode.next;
      numOfSlots += SLOTS_PER_BELT;
    }
    sb.Append("|eol \n");
    // sb.Append("TransportLine belts: [");
    // while (curBeltNode != null) {
    //   sb.Append(curBeltNode.Value().ToString());
    //   sb.Append(", ");
    //   curBeltNode = curBeltNode.next;
    // }
    // sb.Append("null] ");

    Node<ItemInLine> curItemNode = itemsInLineTail;
    int curDistance = curItemNode == null ? 0 : curItemNode.Value().distanceToNextItem;
    for (int i = 0; i < numOfSlots; i++) {
      if (i % 4 == 0) {
        sb.Append("|");
      }
      curDistance = Math.Max(0, curDistance - 1);
      if (curDistance == 0 && curItemNode != null) {
        sb.Append(curItemNode.Value().itemType.ToString().First());
        curItemNode = curItemNode.previous;
        curDistance = curItemNode == null ? 0 : curItemNode.Value().distanceToNextItem;
      } else {
        sb.Append("X");
      }
    }
    sb.Append("|eol \n");
    return sb.ToString();
  }

  private enum SplitPhase {
    LEFT,
    MIDDLE,
    RIGHT
  }
}
