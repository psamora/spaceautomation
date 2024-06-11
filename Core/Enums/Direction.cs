public enum Direction {
  UP,
  RIGHT,
  DOWN,
  LEFT,
}

static class DirectionExtensions {
  public static Direction RotateForward(this Direction direction) {
    if (direction == Direction.LEFT) {
      direction = Direction.UP;
    } else {
      direction++;
    }
    return direction;
  }

  public static Direction RotateBackward(this Direction direction) {
    if (direction == Direction.UP) {
      direction = Direction.LEFT;
    } else {
      direction--;
    }
    return direction;
  }
}
