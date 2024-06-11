using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public partial class SpawnerBox : PlacedEntity {
  [Export]
  public TempItem spawnItem = TempItem.COAL;

  public override Dictionary<TempItem, int> GetAvailableOutput() {
    return new Dictionary<TempItem, int> { { spawnItem, 10 } };
  }
}
