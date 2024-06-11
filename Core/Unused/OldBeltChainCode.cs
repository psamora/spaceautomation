public partial class OldBeltChainCode {
  // This was the first attempt at BeltChains, which would be used to generate and manage
  // Transport lines. The chaining logic worked 99% (edge case: loops that stop being loops 
  // required proper management of the linked list) after 2-3 days of work but it was bad
  //
  // Other than messy this was also the wrong approach. 1- it was too rigid, belt chains would 
  // only update when belts were added which is actually a pain 2- didn't actually
  // solve anything wrt to Transport lines which is what really matters.
  // 
  // Ultimately we threw it all away (other than the knowledge gained) and instead we moved to a
  // world that sequences of belts only matter within a Transport line, and we are always creating
  // and merging transport lines in an ongoing basis.
  // 
  // We still keep it around in case there's any useful insights here. Eventually, we can delete.

  // BeltChain

  // public class BeltChain {
  //   // TODO visibilities
  //   public Belt beltChainEnd = null;
  //   public LinkedList<Belt> beltsInLine = new LinkedList<Belt>();
  //   public Color debugColor = new Color(1, 1, 1);
  //   public bool isLoop = false;
  //   public BeltChain() {
  //     Random random = new Random();
  //     debugColor = new Color(
  //       (float) random.NextDouble(),
  //       (float) random.NextDouble(),
  //       (float) random.NextDouble());
  //   }

  //   public void AddBeltToStart(Belt belt) {
  //     beltsInLine.AddFirst(belt);
  //     //belt.UpdateBeltChain(this);
  //   }

  //   public void AddBeltToEnd(Belt belt) {
  //     beltsInLine.AddLast(belt);
  //     //belt.UpdateBeltChain(this);
  //   }

  //   public void UpdateBeltChain() {
  //   }

  //   public void RemoveFirst() {
  //     if (beltsInLine.Count == 0) {
  //       return;
  //     }
  //     Belt beltBeingRemoved = beltsInLine.First.Value;
  //     beltsInLine.RemoveFirst();
  //     //beltBeingRemoved.UpdateBeltChain(new BeltChain());
  //   }

  //   public BeltChain MergeWith(BeltChain nextBeltChain) {
  //     if (nextBeltChain == null) {
  //       return this;
  //     }
  //     if (nextBeltChain == this) {
  //       return this;
  //     }
  //     foreach (Belt belt in nextBeltChain.beltsInLine) {
  //       AddBeltToEnd(belt);
  //     }
  //     return this;
  //   }

  //   // TODO: if split is O(n) what's the point of linkedlists, either get rid of it or make it O(1)
  //   public BeltChain SplitOnBelt(Belt beltToSplitOn, bool inclusiveOnNewChain) {
  //     bool foundBeltInChain = false;
  //     BeltChain newChain = new BeltChain();
  //     LinkedList<Belt> thisChain = new LinkedList<Belt>();
  //     LinkedListNode<Belt> beltNode = beltsInLine.First;
  //     while (beltNode != null) {
  //       if (inclusiveOnNewChain && beltNode.Value == beltToSplitOn) {
  //         foundBeltInChain = true;
  //       }
  //       if (foundBeltInChain) {
  //         newChain.AddBeltToEnd(beltNode.Value);
  //       } else {
  //         thisChain.AddLast(beltNode.Value);
  //       }
  //       if (!inclusiveOnNewChain && beltNode.Value == beltToSplitOn) {
  //         foundBeltInChain = true;
  //       }
  //       beltNode = beltNode.Next;
  //     }
  //     beltsInLine = thisChain;
  //     isLoop = false;
  //     return newChain;
  //   }

  //   public int Count() {
  //     return beltsInLine.Count;
  //   }

  //   public override string ToString() {
  //     return debugColor.ToString();
  //   }
  // }

  // BeltManager

  // public bool UpdateBeltChains(Belt beltToUpdate, BeltOrientation newBeltOrientation,
  //                              bool initialCall) {
  //   Debug.Print("Updating Belt Chain. Total # of chains: " + beltChains.Count);

  //   // Every time a belt changes orientation, it may result in a rechaining.
  //   // TODO: document this better, but it works.
  //   NeighboringEntities<Belt> neighboringBelts =
  //     GetNeighboringEntities<Belt>(beltToUpdate.xPos, beltToUpdate.yPos);

  //   // We need to update the next entity only based on the first call.
  //   // TODO: this is ugly as hell, if we just recurse over all belts it may be so much cleaner
  //   // and cost the same, but I'm too down this path now
  //   Belt maybeNextBelt = GetPotentialNextBelt(newBeltOrientation, neighboringBelts);
  //   if (initialCall && maybeNextBelt != null) {
  //     BeltOrientation nextBeltOrientation = maybeNextBelt.beltOrientation;
  //     NeighboringEntities<Belt> nextBeltNeighboringEntities = GetNeighboringEntities<Belt>(
  //       maybeNextBelt.xPos, maybeNextBelt.yPos);
  //     BeltOrientation nextBeltUpdatedOrientation = DefineBeltOrientation(
  //       maybeNextBelt.placedDirection, nextBeltNeighboringEntities);
  //     if (nextBeltOrientation != nextBeltUpdatedOrientation) {
  //       Debug.Print("Sub-case 1");
  //       maybeNextBelt.UpdateBeltOrientation(nextBeltUpdatedOrientation);
  //       UpdateBeltChains(maybeNextBelt, nextBeltUpdatedOrientation, false);
  //     } else {
  //       Debug.Print("Sub-case 1");
  //     }
  //   }

  //   var (previousBeltChain, nextBeltChain) =
  //     DetermineBeforeAndAfterBeltChain(newBeltOrientation, neighboringBelts);
  //   Debug.Print((previousBeltChain, nextBeltChain).ToString());

  //   // If this belt is currently chain-less (newly placed belt), add to previous/next or merge,
  //   // or create a new chain altogether.
  //   // TODO maybe refactor. Theoretically if the rest of the logic is right, this could be removed
  //   // and made work with a belt with an empty chain (but maybe slighlt less efficient)
  //   if (beltToUpdate.beltChain == null) {
  //     Debug.Print("Case 0");
  //     if (previousBeltChain == null && nextBeltChain == null) {
  //       beltToUpdate.beltChain = new BeltChain();
  //       beltToUpdate.beltChain.AddBeltToStart(beltToUpdate);
  //       beltChains.Add(beltToUpdate.beltChain);
  //       return true;
  //     }
  //     if (previousBeltChain == nextBeltChain) {
  //       previousBeltChain.isLoop = true;
  //       previousBeltChain.AddBeltToEnd(beltToUpdate);
  //       return true;
  //     }
  //     if (previousBeltChain == null) {
  //       nextBeltChain.AddBeltToStart(beltToUpdate);
  //     } else {
  //       previousBeltChain.AddBeltToEnd(beltToUpdate);
  //       previousBeltChain.MergeWith(nextBeltChain);
  //       beltChains.Remove(nextBeltChain);
  //     }
  //     return true;
  //   }

  //   // Now, if the current belt is already in a chain, there'a a number of possible options.

  //   // If the belt is staying in the current chain, but the next belt is now a separate chain,
  //   // split the original chain on the next belt and then merge it to next chain.
  //   if (beltToUpdate.beltChain == previousBeltChain && beltToUpdate.beltChain != nextBeltChain) {
  //     Debug.Print("Case 1");
  //     BeltChain splittedChain =
  //       beltToUpdate.beltChain.SplitOnBelt(beltToUpdate, /* inclusive= */ false);
  //     if (nextBeltChain != null) {
  //       Debug.Print("Case 1.1");
  //       beltToUpdate.beltChain.MergeWith(nextBeltChain);
  //       beltChains.Remove(nextBeltChain);
  //     }
  //     if (splittedChain.Count() > 0) {
  //       beltChains.Add(splittedChain);
  //     }
  //     return true;
  //   }

  //   // If the belt is leaving the current chain, but the next belt is in the same chain,
  //   // split them on the current belt.
  //   if (beltToUpdate.beltChain != previousBeltChain && beltToUpdate.beltChain == nextBeltChain) {
  //     BeltChain oldBeltChain = beltToUpdate.beltChain;
  //     Debug.Print("Case 2");
  //     BeltChain splittedChain =
  //       beltToUpdate.beltChain.SplitOnBelt(beltToUpdate, /* inclusive= */ true);
  //     if (previousBeltChain == null) {
  //       beltChains.Add(splittedChain);
  //       if (oldBeltChain.Count() == 0) {
  //         beltChains.Remove(oldBeltChain);
  //       }
  //     } else {
  //       previousBeltChain.MergeWith(splittedChain);
  //     }
  //     return true;
  //   }

  //   // If we got here, it means the new belt is going to a completely 100% new chain, now
  //   // we need to figure out which one.
  //   // Regardless, we need to do something about the old chain.
  //   if (beltToUpdate.beltChain.Count() > 1) {
  //     Debug.Print("Splitting existing");
  //     BeltChain previousSplittedChain = beltToUpdate.beltChain;
  //     BeltChain nextSplittedChain = beltToUpdate.beltChain.SplitOnBelt(
  //       beltToUpdate, /* inclusive= */ true);
  //     Debug.Print((previousSplittedChain.Count(), nextSplittedChain.Count()).ToString());
  //     nextSplittedChain.RemoveFirst();
  //     if (nextSplittedChain.Count() > 0) {
  //       beltChains.Add(nextSplittedChain);
  //     }

  //     if (previousSplittedChain.Count() == 0) {
  //       beltChains.Remove(previousSplittedChain);
  //     }
  //   } else {
  //     beltChains.Remove(beltToUpdate.beltChain);
  //   }

  //   // If the two chains are not the same, merge them after adding this node and then split the
  //   // chain this belt is coming from *without* this node
  //   if (previousBeltChain != nextBeltChain) {
  //     Debug.Print("Case 3");
  //     // If there's no previous chain, add to front of existing or create a new one
  //     if (previousBeltChain == null) {
  //       Debug.Print("Case 3.1");
  //       BeltChain chainToAdd = nextBeltChain == null ? new BeltChain() : nextBeltChain;
  //       chainToAdd.AddBeltToStart(beltToUpdate);
  //       return true;
  //     }
  //     Debug.Print("Case 3.2");
  //     // Otherwise add to back and merge to the next chain and move on. Underlying methods should
  //     // gracefully merge nulls for us.
  //     previousBeltChain.AddBeltToEnd(beltToUpdate);
  //     previousBeltChain.MergeWith(nextBeltChain);
  //     beltChains.Remove(nextBeltChain);
  //     return true;
  //   }

  //   // If the two chains are the same, it means this is one big loop and our belt is the last
  //   // piece. Just add it to the either the front or back and move on :)
  //   if (previousBeltChain == nextBeltChain && previousBeltChain != null) {
  //     Debug.Print("Case 4");
  //     previousBeltChain.AddBeltToEnd(beltToUpdate);
  //     previousBeltChain.isLoop = true;
  //     return true;
  //   }

  //   // TODO: better logging
  //   Debug.Print("Got somewhere unexpected.");
  //   return false;
  // }

  // private (BeltChain, BeltChain) DetermineBeforeAndAfterBeltChain(
  //     BeltOrientation newBeltOrientation,
  //     NeighboringEntities<Belt> neighboringBelts) {
  //   // Get the previous belt chain if applicable.
  //   Belt previousBelt = GetPreviousBelt(newBeltOrientation, neighboringBelts);
  //   BeltChain previousBeltChain = null;
  //   if (previousBelt != null) {
  //     previousBeltChain = previousBelt.beltChain;
  //   }

  //   Belt maybeNextBelt = GetPotentialNextBelt(newBeltOrientation, neighboringBelts);
  //   BeltChain nextBeltChain = null;

  //   // Adjust the next belt orientation based on the newly placed belt.
  //   // Then compute if, given the new orientation, the next belt is part of this chain or not
  //   if (maybeNextBelt != null) {
  //     NeighboringEntities<Belt> nextBeltNeighboringEntities = GetNeighboringEntities<Belt>(
  //       maybeNextBelt.xPos, maybeNextBelt.yPos);
  //     BeltOrientation nextBeltUpdatedOrientation = DefineBeltOrientation(
  //       maybeNextBelt.placedDirection, nextBeltNeighboringEntities);
  //     if (IsPotentialNextBeltInSameChain(newBeltOrientation, nextBeltUpdatedOrientation)) {
  //       nextBeltChain = maybeNextBelt.beltChain;
  //     }
  //   }

  //   return (previousBeltChain, nextBeltChain);
  // }
  // private bool IsPotentialNextBeltInSameChain(
  //     BeltOrientation newBeltOrientation, BeltOrientation potentialNextBeltOrientation) {
  //   switch (newBeltOrientation) {
  //     case BeltOrientation.UP:
  //     case BeltOrientation.LEFT_UP:
  //     case BeltOrientation.RIGHT_UP:
  //       return potentialNextBeltOrientation == BeltOrientation.UP ||
  //              potentialNextBeltOrientation == BeltOrientation.UP_LEFT ||
  //              potentialNextBeltOrientation == BeltOrientation.UP_RIGHT;
  //     case BeltOrientation.DOWN:
  //     case BeltOrientation.LEFT_DOWN:
  //     case BeltOrientation.RIGHT_DOWN:
  //       return potentialNextBeltOrientation == BeltOrientation.DOWN ||
  //              potentialNextBeltOrientation == BeltOrientation.DOWN_LEFT ||
  //              potentialNextBeltOrientation == BeltOrientation.DOWN_RIGHT;
  //     case BeltOrientation.LEFT:
  //     case BeltOrientation.UP_LEFT:
  //     case BeltOrientation.DOWN_LEFT:
  //       return potentialNextBeltOrientation == BeltOrientation.LEFT ||
  //              potentialNextBeltOrientation == BeltOrientation.LEFT_UP ||
  //              potentialNextBeltOrientation == BeltOrientation.LEFT_DOWN;
  //     case BeltOrientation.RIGHT:
  //     case BeltOrientation.UP_RIGHT:
  //     case BeltOrientation.DOWN_RIGHT:
  //       return potentialNextBeltOrientation == BeltOrientation.RIGHT ||
  //              potentialNextBeltOrientation == BeltOrientation.RIGHT_UP ||
  //              potentialNextBeltOrientation == BeltOrientation.RIGHT_DOWN;
  //     default:
  //       return false;
  //   }

  // }

  // private bool PotentialNextBeltRequiresRerouting(
  //   BeltOrientation newBeltOrientation, Belt potentialNextBelt) {
  //   if (potentialNextBelt == null) {
  //     return false;
  //   }
  //   switch (newBeltOrientation) {
  //     case BeltOrientation.UP:
  //     case BeltOrientation.LEFT_UP:
  //     case BeltOrientation.RIGHT_UP:
  //       return potentialNextBelt.beltOrientation == BeltOrientation.DOWN_LEFT ||
  //              potentialNextBelt.beltOrientation == BeltOrientation.DOWN_RIGHT;
  //     case BeltOrientation.DOWN:
  //     case BeltOrientation.LEFT_DOWN:
  //     case BeltOrientation.RIGHT_DOWN:
  //       return potentialNextBelt.beltOrientation == BeltOrientation.UP_LEFT ||
  //              potentialNextBelt.beltOrientation == BeltOrientation.UP_RIGHT;
  //     case BeltOrientation.LEFT:
  //     case BeltOrientation.UP_LEFT:
  //     case BeltOrientation.DOWN_LEFT:
  //       return potentialNextBelt.beltOrientation == BeltOrientation.RIGHT_UP ||
  //              potentialNextBelt.beltOrientation == BeltOrientation.RIGHT_DOWN;
  //     case BeltOrientation.RIGHT:
  //     case BeltOrientation.UP_RIGHT:
  //     case BeltOrientation.DOWN_RIGHT:
  //       return potentialNextBelt.beltOrientation == BeltOrientation.LEFT_UP ||
  //              potentialNextBelt.beltOrientation == BeltOrientation.LEFT_DOWN;
  //     default:
  //       return false;
  //   }
  // }
  // private Belt GetPreviousBelt(
  //     BeltOrientation beltOrientation, NeighboringEntities<Belt> neighboringBelts) {
  //   bool isTopBeltFacing = neighboringBelts.topEntity?.placedDirection == Direction.DOWN;
  //   bool isBottomBeltFacing = neighboringBelts.bottomEntity?.placedDirection == Direction.UP;
  //   bool isLeftBeltFacing = neighboringBelts.leftEntity?.placedDirection == Direction.RIGHT;
  //   bool isRightBeltFacing = neighboringBelts.rightEntity?.placedDirection == Direction.LEFT;
  //   switch (beltOrientation) {
  //     case BeltOrientation.UP:
  //     case BeltOrientation.UP_LEFT:
  //     case BeltOrientation.UP_RIGHT:
  //       return isBottomBeltFacing ? neighboringBelts.bottomEntity : null;
  //     case BeltOrientation.DOWN:
  //     case BeltOrientation.DOWN_LEFT:
  //     case BeltOrientation.DOWN_RIGHT:
  //       return isTopBeltFacing ? neighboringBelts.topEntity : null;
  //     case BeltOrientation.LEFT:
  //     case BeltOrientation.LEFT_UP:
  //     case BeltOrientation.LEFT_DOWN:
  //       return isRightBeltFacing ? neighboringBelts.rightEntity : null;
  //     case BeltOrientation.RIGHT:
  //     case BeltOrientation.RIGHT_UP:
  //     case BeltOrientation.RIGHT_DOWN:
  //       return isLeftBeltFacing ? neighboringBelts.leftEntity : null;
  //     default:
  //       return null;
  //   }
  // }

  // private Belt GetPotentialNextBelt(
  //     BeltOrientation beltOrientation, NeighboringEntities<Belt> neighboringBelts) {
  //   bool isTopBeltFacing = neighboringBelts.topEntity?.placedDirection == Direction.DOWN;
  //   bool isBottomBeltFacing = neighboringBelts.bottomEntity?.placedDirection == Direction.UP;
  //   bool isLeftBeltFacing = neighboringBelts.leftEntity?.placedDirection == Direction.RIGHT;
  //   bool isRightBeltFacing = neighboringBelts.rightEntity?.placedDirection == Direction.LEFT;
  //   switch (beltOrientation) {
  //     case BeltOrientation.UP:
  //     case BeltOrientation.LEFT_UP:
  //     case BeltOrientation.RIGHT_UP:
  //       return isTopBeltFacing ? null : neighboringBelts.topEntity;
  //     case BeltOrientation.DOWN:
  //     case BeltOrientation.LEFT_DOWN:
  //     case BeltOrientation.RIGHT_DOWN:
  //       return isBottomBeltFacing ? null : neighboringBelts.bottomEntity;
  //     case BeltOrientation.LEFT:
  //     case BeltOrientation.UP_LEFT:
  //     case BeltOrientation.DOWN_LEFT:
  //       return isLeftBeltFacing ? null : neighboringBelts.leftEntity;
  //     case BeltOrientation.RIGHT:
  //     case BeltOrientation.UP_RIGHT:
  //     case BeltOrientation.DOWN_RIGHT:
  //       return isRightBeltFacing ? null : neighboringBelts.rightEntity;
  //     default:
  //       return null;
  //   }
  // }
}
