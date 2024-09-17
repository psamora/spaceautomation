using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// Bad generator that gets common suffixes/middle/prefixes from real planets and randomize
// them. TODO: make something better one day, the output names are bad hahaha
// Inspired/uses data from https://github.com/sayamqazi/planet-name-generator
public partial class StarSystemNameGenerator {
  private List<string> firstSyllables = new List<string>();
  private List<string> middleSyllables = new List<string>();
  private List<string> lastSyllables = new List<string>();
  private List<int> potentialNumberOfMiddleSyllables
    = new() { 0, 0, 1, 1, 1, 1, 1, 1, 2, 2, 3, 4 };

  public StarSystemNameGenerator() {
    InitializeNameGeneration();
  }

  public string GenerateName(Random seededRandom) {
    StringBuilder sb = new StringBuilder();
    sb.Append(firstSyllables[seededRandom.Next(firstSyllables.Count)]);
    for (int i = 0; i < potentialNumberOfMiddleSyllables[seededRandom.Next(potentialNumberOfMiddleSyllables.Count)]; i++) {
      sb.Append(middleSyllables[seededRandom.Next(middleSyllables.Count)]);
    }
    sb.Append(lastSyllables[seededRandom.Next(lastSyllables.Count)]);
    return sb.ToString().ToPascalCase();
  }

  private void InitializeNameGeneration() {
    var planetNames = Godot.FileAccess.Open(
      "res://Scenes/Space/Imports/planets.txt", Godot.FileAccess.ModeFlags.Read);
    while (!planetNames.EofReached()) {
      string curPlanetName = planetNames.GetLine();
      string[] syllables = curPlanetName.Split("-");
      if (syllables.Length == 0) {
        continue;
      }

      // Repeated syllables may end up in the lists naturally making them more frequent on random
      // pick.
      firstSyllables.Add(syllables.First().Trim());
      lastSyllables.Add(syllables.Last().Trim());

      foreach (string syllable in syllables) {
        if (syllable == syllables.First() || syllable == syllables.Last()) {
          continue;
        }
        middleSyllables.Add(syllable);
      }
    }
  }
}
