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

  private static Random debugColorRandom = new Random();
  private Color debugColor = new Color(1, 1, 1);

  private HashSet<Belt> currentBelts;
  private Node<Belt> beltLinkedListHead = null;
  private Node<Belt> beltLinkedListTail = null;

  // TODO replace ItemInLine with struct/something else
  private Node<ItemInLine> itemsInLineLinkedListHead = null;
  private Node<ItemInLine> itemsInLineLinkedListTail = null;
  private Node<ItemInLine> lastMovingNode = null;

  private TransportLine() {
    debugColor = new Color(
      (float) debugColorRandom.NextDouble(),
      (float) debugColorRandom.NextDouble(),
      (float) debugColorRandom.NextDouble());

    this.currentBelts = new HashSet<Belt>();
  }

  public TransportLine(Belt initialBelt) : this() {
    AddFirstBelt(initialBelt);
    initialBelt.SetTransportLine(this);
    currentBelts.Add(initialBelt);
  }

  // This method split one TransportLine on a specific belt. All TransportLines will be new,
  // and it will mutates the incoming line, making it empty. If you're using it I hope
  // you're reading this :)
  public static (TransportLine, TransportLine, TransportLine) SplitTransportLineOnBelt(
      TransportLine line, Belt belt) {
    TransportLine before = new TransportLine();
    TransportLine current = new TransportLine(belt);
    TransportLine after = new TransportLine();

    // It's possible to the actual split in O(1) just moving pointers around as long as we keep
    // a Belt->BeltNode mapping. But since we have to iterate the transport line contents
    // it's not really a major performance improvement (TBD).
    Node<Belt> curNode = line.beltLinkedListHead;
    bool passedSplitPoint = false;
    while (curNode != null) {
      if (curNode.Value() == belt) {
        passedSplitPoint = true;
        curNode = curNode.next;
        continue;
      }

      if (passedSplitPoint) {
        after.AddBeltToEnd(curNode.Value());
      } else {
        before.AddBeltToEnd(curNode.Value());
      }
      curNode = curNode.next;
    }

    // Reset incoming line, it's empty now.
    line.currentBelts.Clear();
    line.beltLinkedListHead = null;
    line.beltLinkedListTail = null;
    return (before, current, after);
  }


  // This method doesn't create new TransportLines, it does an inplace merge, emptying the second
  // in the process. Yeah it's inconsistent with the Split API :).
  // TODO: make APIs consistent, or just deal with it.
  public void Merge(TransportLine lineToAppend) {
    if (lineToAppend == this || lineToAppend.IsEmpty()) {
      return;
    }

    // It's possible to the actual merge in O(1) just moving pointers around as long as we keep
    // a Belt->BeltNode mapping. But since we have to iterate the transport line contents
    // it's not really a major performance improvement (TBD).
    Node<Belt> curNode = lineToAppend.beltLinkedListHead;
    while (curNode != null) {
      AddBeltToEnd(curNode.Value());
      curNode = curNode.next;
      lastMovingNode = itemsInLineLinkedListTail;
      if (lastMovingNode != null) {
        lastMovingNode.Value().distanceToNextItem += SLOTS_PER_BELT;
      }
    }

    // TODO: Merge line items too. Right now we assume next belt is always empty which is incorrect

    // Reset second line, it's empty now.
    lineToAppend.currentBelts.Clear();
    lineToAppend.beltLinkedListHead = null;
    lineToAppend.beltLinkedListTail = null;
    return;
  }

  // This method tries to add the given item to the given belt in the transport line.
  // If it return false, it means the belt is full or the belt is not part of this transport line.
  public bool MaybePlaceItemInBelt(TempItem item, Belt belt) {
    if (!currentBelts.Contains(belt)) {
      return false;
    }

    Debug.Print($"\nAdding item to TransportLine {this}");
    int curIndex = 0;
    int cumulativeDistance = 0;
    Node<Belt> curBeltNode = beltLinkedListTail;
    Node<ItemInLine> curItemInLineNode = itemsInLineLinkedListTail;

    // Starting from the end of the transport line, iterate both belts + transport line backward
    // (belts first), until we reach the belt/transport lines for the targetted belt, where
    // we will attempt to fit an item.
    while (curBeltNode != null) {
      int curBeltDistanceWindowMax = (STEPS_PER_BELT * curIndex) + STEPS_PER_BELT;

      int cumulativeDistanceUpToThisBelt = cumulativeDistance;
      bool isTargetBelt = curBeltNode.Value() == belt;
      List<Node<ItemInLine>> lineItemsForThisBelt = new List<Node<ItemInLine>>();
      Node<ItemInLine> maybeNextItemInLineInSeparateBelt = curItemInLineNode?.next;

      // Iterate the item LinkedList until we are out of the current belt and into the previous.
      while (curItemInLineNode != null &&
             cumulativeDistance + curItemInLineNode.Value().distanceToNextItem <= curBeltDistanceWindowMax) {
        cumulativeDistance += curItemInLineNode.Value().distanceToNextItem;
        if (isTargetBelt) {
          lineItemsForThisBelt.Add(curItemInLineNode);
        }
        curItemInLineNode = curItemInLineNode.previous;
      }

      if (isTargetBelt) {
        Debug.Print(
          $"Adding to BeltIndex: {curIndex}, Cumulative distance {cumulativeDistance}");
        Node<ItemInLine> nextItemInLineInSeparateBelt =
          lineItemsForThisBelt.Contains(maybeNextItemInLineInSeparateBelt) ?
          null : maybeNextItemInLineInSeparateBelt;
        bool result = MaybePlaceItemInBeltSegment(
          item, curIndex, cumulativeDistanceUpToThisBelt,
          lineItemsForThisBelt, nextItemInLineInSeparateBelt);
        Debug.Print($"Result to {this}");
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
      int cumulativeDistanceUpToThisBelt,
      List<Node<ItemInLine>> lineItemsForThisBelt,
      Node<ItemInLine> nextItemInLineInSeparateBelt) {
    Debug.Print($"Current line items for Belt {String.Join(", ", lineItemsForThisBelt)}");

    // If there's already the max amount of ItemInLine for this belt stop there.
    if (lineItemsForThisBelt.Count >= SLOTS_PER_BELT) {
      return false;
    }

    // Otherwise, it means we have a item to add and we need to pick
    // 1- what it's distance from the next ItemInLine
    // 2- where in the LinkedList to add it

    // If there's no other item in the whole Transport Line, add it directly in the
    // first slot at the right distance and start the Line of items and we are done.
    int beltDistanceFromTransportLineStart = (STEPS_PER_BELT * beltIndex);
    if (itemsInLineLinkedListHead == null) {
      AddFirstTransportLineItem(item, beltDistanceFromTransportLineStart + STEPS_PER_BELT);
      return true;
    }

    // If we have items in this belt already, we need to figure out where to fit this item.
    // TODO: implement this correctly somehow??? currently not actually correct.
    if (lineItemsForThisBelt.Count > 0) {
      bool[] slotOccupied = new bool[SLOTS_PER_BELT];
      lineItemsForThisBelt.Last().AddBefore(new ItemInLine(item, 1));
      if (lineItemsForThisBelt.Last() == itemsInLineLinkedListHead) {
        itemsInLineLinkedListHead = lineItemsForThisBelt.Last().previous;
      }
      return true;
    }

    // Otherwise, if we are here there's no items in this belt, but there are *some* items
    // in the line.
    // If there's an item in line after this one, we will attach ourselves to it
    if (nextItemInLineInSeparateBelt != null) {
      // Compute the distance from the beginning of this belt to the next item in line.
      int distanceFromNextBeltLine =
        ((STEPS_PER_BELT * beltIndex) + 1) -
        (cumulativeDistanceUpToThisBelt + nextItemInLineInSeparateBelt.Value().distanceToNextItem);
      nextItemInLineInSeparateBelt.AddBefore(new ItemInLine(item, distanceFromNextBeltLine));
      if (nextItemInLineInSeparateBelt == itemsInLineLinkedListHead) {
        itemsInLineLinkedListHead = nextItemInLineInSeparateBelt.previous;
      }
      return true;
    }

    // Otherwise, there must be an item before us in line, which by definition must be the tail.
    // We simply compute distance to end of belt and move on.
    itemsInLineLinkedListTail.AddAfter(new ItemInLine(item, beltDistanceFromTransportLineStart));
    itemsInLineLinkedListTail = itemsInLineLinkedListTail.next;
    return true;
  }

  public Belt GetLastBeltInLine() {
    return beltLinkedListTail.Value();
  }

  public void TryToOutputToNextTransportLine(TransportLine nextTransportLine) {
    if (nextTransportLine == null) {
      return;
    }
  }

  public void AdvanceTransportLine() {
    if (lastMovingNode == null) {
      return;
    }

    if (lastMovingNode.Value().distanceToNextItem > 1) {
      lastMovingNode.Value().distanceToNextItem--;
      return;
    }

    if (lastMovingNode.Value().distanceToNextItem == 1) {
      if (lastMovingNode.previous != null) {
        lastMovingNode = lastMovingNode.previous;
      }
      return;
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
    Node<ItemInLine> curItemInLineNode = itemsInLineLinkedListTail;

    // Iterate both the Belt and ItemInLine linked list together. It likely doesn't matter
    // which one you use, so we iterate the belt one for consistenct with previous code.
    while (curBeltNode != null) {
      int curBeltDistanceWindowMin = (STEPS_PER_BELT * curIndex);
      int curBeltDistanceWindowMax = (STEPS_PER_BELT * curIndex) + STEPS_PER_BELT;

      // If the current item loop is not in the belt, also move the item node backwards so that
      // by the next loop
      // Iterate the items in line until we are either done with the items in line
      while (curItemInLineNode != null &&
             cumulativeDistance + curItemInLineNode.Value().distanceToNextItem <= curBeltDistanceWindowMax) {
        cumulativeDistance += curItemInLineNode.Value().distanceToNextItem;
        itemsToDraw.Add(
          new ItemToDraw(
            curItemInLineNode.Value().itemType,
            curBeltNode.Value().GetWorldPositionForTopSlot(
                curBeltDistanceWindowMax - cumulativeDistance
              )
            ));
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
    belt.SetTransportLine(this);
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
    belt.SetTransportLine(this);
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
    this.itemsInLineLinkedListHead = new Node<ItemInLine>(new ItemInLine(item, distance));
    this.itemsInLineLinkedListTail = itemsInLineLinkedListHead;
    this.lastMovingNode = this.itemsInLineLinkedListHead;
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
    sb.Append("TransportLine belts: [");
    while (curBeltNode != null) {
      sb.Append(curBeltNode.Value().ToString());
      sb.Append(", ");
      curBeltNode = curBeltNode.next;
    }
    sb.Append("null]");

    sb.Append("\n");
    Node<ItemInLine> curItemNode = itemsInLineLinkedListHead;
    sb.Append("TransportLine contents: [");
    while (curItemNode != null) {
      sb.Append(curItemNode.Value().ToString());
      sb.Append(", ");
      curItemNode = curItemNode.next;
    }
    sb.Append("null] ");
    return sb.ToString();
  }
}
