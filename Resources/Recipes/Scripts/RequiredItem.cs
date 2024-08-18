using Godot;
using System;

[Tool]
[GlobalClass]
public partial class RequiredItem : Resource {
  [Export]
  public int itemAmount;

  [Export]
  public string requiredItemId;
}
