using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public abstract partial class PlacedEntity : Sprite2D {
  public virtual Dictionary<TempItem, int> GetRequestedItems() {
    return new Dictionary<TempItem, int>();
  }

  public virtual Dictionary<TempItem, int> GetAvailableOutput() {
    return new Dictionary<TempItem, int>();
  }

  public virtual bool PlaceItems(TempItem itemType, int amount, bool topIfApplicable) {
    return true;
  }

  public virtual int TakeItems(TempItem itemType, int maxAmount) {
    return 1;
  }
}
