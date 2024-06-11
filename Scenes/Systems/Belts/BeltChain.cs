using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;

public enum TempItem {
  NONE, // default
  ALL,  // invalid for individual items. This is used when a receptacle would accept any item.
  IRON,
  COPPER,
  COAL,
}

// public class TransportLine {
//   public int transportLineEnd = 0;
//   public int lastPositiveIndex = 0;
//   public List<(TempItem, int)> itemAndDistanceInLine = new List<(TempItem, int)>();
//   public LinkedList<Belt> beltsInLine = new LinkedList<Belt>();
//   public List<Belt> indexableBeltsInLine = new List<Belt>();

//   public void UpdateTransportLine() {
//     //itemAndDistanceInLine[lastPositiveIndex] -= 1;
//   }
// }

// public class BeltChain2 {
//   public LinkedList<Belt> beltsInLine = new LinkedList<Belt>();
//   public Color debugColor = new Color(1, 1, 1);

//   public BeltChain2() {
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

//   public override string ToString() {
//     return debugColor.ToString();
//   }
// }

// public class BeltChain {
//   // TODO visibilities
//   public Belt beltChainEnd = null;
//   public LinkedList<Belt> beltsInLine = new LinkedList<Belt>();
//   public TransportLine topTransportLine = new TransportLine(); // TODO make private, top from left->right 
//   public TransportLine bottomTransportLine = new TransportLine();
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
//     topTransportLine.UpdateTransportLine();
//     bottomTransportLine.UpdateTransportLine();
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
