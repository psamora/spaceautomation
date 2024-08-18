using Godot;
using System;

[Tool]
[GlobalClass]
public partial class RequiredItemBehavior : Resource, IComparable<RequiredItemBehavior> {
  [Export]
  public int itemAmount;

  [Export]
  public ItemBehaviorType requiredItemBehaviorType;

  public int CompareTo(RequiredItemBehavior other) {
    return requiredItemBehaviorType.CompareTo(other);
  }
}
