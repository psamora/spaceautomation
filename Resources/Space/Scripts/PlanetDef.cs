using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
[Tool]
public partial class PlanetDef : Resource {
  [Export]
  public string planetTypeName;

  [Export]
  public PlanetPlacementCategory placementCategory;

  public enum PlanetPlacementCategory {
    // A Planet type that is both gameplay critical due to specific resources but also required
    // to start to meet minimum needs for expansion. Guaranteed to generate at least 1 of this
    // category type in the starting system.
    // After that, PlanetTypes with this category spawn very rarely (~1/100 planets)
    STARTER,

    // A Planet type that provides core gameplay (e.g. introduces new game systems) but not a
    // requirement for early game travel.
    // One PlanetType of this category is guaranteed to spawn every system in a geographically 
    // distributed manner, and otherwise there's a occasional random occurrance of them (~15/100
    // planets).
    CORE,

    // Most planets in the game are barren and while still useful for exploration, combat and
    // specific resource extraction, they are not super important.
    // Guaranteed to generate at least 1 of this type per system (excluding starting ones).
    // After that, PlanetTypes with this category spawn very frequently (~60/100 planets) and with
    // roughly even distribution between barren types.
    BARREN,

    // Some planets in the game are composed mostly of gases or icy gas, with limited utility.
    // Guaranteed to generate at least 1 of this type per systems with more than 7 planets.
    // Otherwise, spawns at ~15/100 planets.
    GASEOUS_OR_ICY,

    // Some planets in the game are composed primarily of oceans of a variety of types
    OCEANIC,

    // This planet type is guaranteed to spawn at least once in a Unique star system. It will
    // not spawn outside Unique star systems.
    UNIQUE
  }

  [Export]
  public Godot.Collections.Array<DistanceFromStar> possibleDistanceFromStar;

  public enum DistanceFromStar {
    // A Planet type that can be placed at any distance from the sun.
    ANY,

    // A Planet type that can only be placed close to the sun (< 1/2 AU)
    INNER_CLOSE,

    // A Planet type that can only be placed between 1/2 AUS and 4AU
    INNER_FAR,

    // A Planet type that can only be placed between 4AU and 10AU
    OUTER_CLOSE,

    // A Planet type that can only be placed between 10AU and 40AU
    OUTER_FAR
  }
}
