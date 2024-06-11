using System;
using System.Collections;
using System.Collections.Generic;

// TODO: bigger buildings would need more neighbors, but 1x1 support is nice.
// maybe we have two types, etc tbd
// FYI though most of the time I assume you don't need to know all neighbours for big buildings as 
// we can just update the inserters. so maybe not needed
public struct NeighboringEntities<T> : IEnumerable<T> where T : PlacedEntity {
  public T topEntity;
  public T bottomEntity;
  public T leftEntity;
  public T rightEntity;

  public IEnumerator<T> GetEnumerator() {
    return new List<T> { topEntity, bottomEntity, leftEntity, rightEntity }.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator() {
    return new List<T> { topEntity, bottomEntity, leftEntity, rightEntity }.GetEnumerator();
  }

  public T GetFacingEntity(Direction facingDirection) {
    switch (facingDirection) {
      case Direction.UP:
        return topEntity;
      case Direction.DOWN:
        return bottomEntity;
      case Direction.LEFT:
        return leftEntity;
      case Direction.RIGHT:
        return rightEntity;
      default:
        throw new ApplicationException($"Invalid direction: {facingDirection}");
    }
  }

  public Belt GetPreviousBelt(BeltOrientation beltOrientation) {
    switch (beltOrientation) {
      case BeltOrientation.UP:
      case BeltOrientation.UP_RIGHT:
      case BeltOrientation.UP_LEFT:
        return bottomEntity as Belt;
      case BeltOrientation.DOWN:
      case BeltOrientation.DOWN_LEFT:
      case BeltOrientation.DOWN_RIGHT:
        return topEntity as Belt;
      case BeltOrientation.RIGHT:
      case BeltOrientation.RIGHT_DOWN:
      case BeltOrientation.RIGHT_UP:
        return leftEntity as Belt;
      case BeltOrientation.LEFT:
      case BeltOrientation.LEFT_DOWN:
      case BeltOrientation.LEFT_UP:
        return rightEntity as Belt;
      default:
        throw new ApplicationException($"Invalid orientation: {beltOrientation}");
    }
  }
}
