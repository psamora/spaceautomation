using Godot;
using System;

[Tool]
[GlobalClass]
public partial class OutputItem : Resource {
  [Export]
  public string outputItemId;

  [Export]
  public int itemAmount;

  [Export]
  public int outputProbability;
}
