using Godot;
using System;
using System.Collections.Generic;

public static class ListExtensions {

  public static void Shuffle<T>(this List<T> ts, Random seededRandom) {
    var count = ts.Count;
    var last = count - 1;
    for (var i = 0; i < last; ++i) {
      var r = seededRandom.Next(i, count);
      var tmp = ts[i];
      ts[i] = ts[r];
      ts[r] = tmp;
    }
  }
}
