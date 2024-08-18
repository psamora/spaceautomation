using Godot;
using System;

[AttributeUsage(AttributeTargets.Field)]
public class DisplayedProperty : Attribute {
  public string displayedPropertyString;
  public string unitString;
  public bool canBeMadePlural = false;

  public DisplayedProperty(string displayedPropertyString, string unitName) {
    this.displayedPropertyString = displayedPropertyString;
    this.unitString = unitName;
  }

  public DisplayedProperty(string displayedPropertyString, string unitName, bool canBeMadePlural) {
    this.displayedPropertyString = displayedPropertyString;
    this.unitString = unitName;
    this.canBeMadePlural = canBeMadePlural;
  }
}

[AttributeUsage(AttributeTargets.Class)]
public class ItemBehaviorDisplayName : Attribute {
  public string displayName;

  public ItemBehaviorDisplayName(String displayName) {
    this.displayName = displayName;
  }
}

[AttributeUsage(AttributeTargets.Class)]
public class ItemBehaviorIcon : Attribute {
  public Texture2D texture;

  public ItemBehaviorIcon(String texturePath) {
    texture = ResourceLoader.Load<Texture2D>(texturePath);
  }
}

[AttributeUsage(AttributeTargets.Class)]
public class ItemBehaviorIconColor : Attribute {
  public Color color;
  public Color borderColor;

  public ItemBehaviorIconColor(string colorName) {
    this.color = new Color(colorName);
    // TODO: make this better, may go over 1, etc
    this.borderColor = new Color(this.color.R + 0.2f, this.color.G + 0.2f, this.color.B + 0.2f);
  }
}
