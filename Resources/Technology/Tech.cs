using Godot;
using System;

public partial class Tech : Resource {
  [Export]
  public string techId;

  [Export]
  public string techName;

  [Export]
  public Godot.Collections.Array<string> preconditionTechIds;

  [Export]
  public Godot.Collections.Array<string> unlockedRecipeIds;

  // cost or unlock condition
}
