using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;

// Displays a resource in a UI that can both be used for the Editor but also in-game, automatically
// handling a lot of the busy work. Can be attached to any Container node (preferably a Container)
// to display a specific Resource.
[Tool]
public partial class CustomResourceViewer : Control {
  // If false, shows them vertically.
  [Export] bool showPropertiesHorizontally;
  [Export] HBoxContainer hBoxContainer;
  [Export] VBoxContainer vBoxContainer;
  [Export] Resource resourceTypeToUse;

  // Include only specified properties. If empty, will include all properties except the ones in
  // propertiesToSkip.
  [Export] Godot.Collections.Array<string> propertiesToInclude = new Godot.Collections.Array<string>();

  // Properties to skip regardless of the state of propertiesToInclude.
  [Export] Godot.Collections.Array<string> propertiesToSkip = new Godot.Collections.Array<string>();

  [Export] Label defaultLabel;
  [Export] HSlider defaultHSlider;
  [Export] HBoxContainer defaultHBoxContainer;

  private HashSet<string> propertiesToSkipSet = new HashSet<string>();
  private Action unsavedChangedCallback;
  private Resource currentResource;

  private Dictionary<Control, Label> controlToLabelMap
    = new Dictionary<Control, Label>();
  private Dictionary<Control, FieldInfo> controlToFieldInfoMap
    = new Dictionary<Control, FieldInfo>();
  private Godot.Vector2 initialCustomMinimumSize;

  public override void _Ready() {
    initialCustomMinimumSize = CustomMinimumSize;
  }

  public void BindResource(Resource resource, Action unsavedChangedCallback) {
    Container containerToUse = showPropertiesHorizontally ? hBoxContainer : vBoxContainer;

    // Reset the viewer to an empty state.
    CustomMinimumSize = initialCustomMinimumSize;
    foreach (Node child in containerToUse.GetChildren()) {
      child.QueueFree();
    }

    this.unsavedChangedCallback = unsavedChangedCallback;
    this.currentResource = resource;

    Type curType = resource.GetType();
    MemberInfo[] myMembers = curType.GetMembers();

    foreach (MemberInfo member in myMembers) {
      if (member.MemberType != MemberTypes.Field) {
        continue;
      }

      if (propertiesToInclude.Count > 0 && !propertiesToInclude.Contains(member.Name)) {
        continue;
      }

      if (propertiesToSkipSet.Contains(member.Name)) {
        continue;
      }

      ExportAttribute exportAttribute = member.GetCustomAttribute<ExportAttribute>();
      DisplayedProperty displayedProperty = member.GetCustomAttribute<DisplayedProperty>();
      if (displayedProperty == null) {
        continue;
      }

      // If we got here, we have a Resource property to display.
      HBoxContainer curHboxContainer = defaultHBoxContainer.Duplicate() as HBoxContainer;
      containerToUse.AddChild(curHboxContainer);

      curHboxContainer.Visible = true;
      CustomMinimumSize = CustomMinimumSize + new Godot.Vector2(0, 30);

      Label propertyDisplayedNameLabel = defaultLabel.Duplicate() as Label;
      propertyDisplayedNameLabel.Text = displayedProperty.displayedPropertyString;
      propertyDisplayedNameLabel.Visible = true;
      curHboxContainer.AddChild(propertyDisplayedNameLabel);

      Label valueLabel = defaultLabel.Duplicate() as Label;
      valueLabel.Visible = true;
      valueLabel.Text =
        curType.GetField(member.Name).GetValue(resource).ToString();
      curHboxContainer.AddChild(valueLabel);

      Label unitLabel = defaultLabel.Duplicate() as Label;
      unitLabel.Visible = true;
      unitLabel.Text = displayedProperty.unitString;
      curHboxContainer.AddChild(unitLabel);

      if (!Engine.IsEditorHint()) {
        continue;
      }

      // If we got here, means we are in an Editor view of the Resource and therefore should
      // add the appropriate edit button/field.
      if (exportAttribute.Hint == PropertyHint.Range) {
        string[] rangeStrSplit = exportAttribute.HintString.Split(",");
        double minValue = 0;
        double maxValue = 0;
        double step = 0;
        HSlider valueSlider = defaultHSlider.Duplicate() as HSlider;
        valueSlider.Visible = true;

        if (rangeStrSplit.Count() > 0) {
          Double.TryParse(rangeStrSplit[0], out minValue);
        }
        if (rangeStrSplit.Count() > 1) {
          Double.TryParse(rangeStrSplit[1], out maxValue);
        }
        if (rangeStrSplit.Count() > 2) {
          Double.TryParse(rangeStrSplit[2], out step);
        }
        valueSlider.MinValue = minValue;
        valueSlider.MaxValue = maxValue;
        valueSlider.Step = step;

        double maybeParsedValue = 0;
        Double.TryParse(
          curType.GetField(member.Name).GetValue(resource).ToString(), out maybeParsedValue);
        valueSlider.Value = maybeParsedValue;

        // Due to Godot https://github.com/godotengine/godot/issues/78513 still happening in 4.3,
        // instead of binding the params in a lambda so we made everything a local variable to the
        // class through dictionaries to achieve the same effect. If we try binding FieldType or
        // Label to this callback the issue happens for some reason.
        valueSlider.ValueChanged +=
          (newValue) => OnNumericalPropertyValueChanged(newValue, valueSlider);
        controlToLabelMap[valueSlider] = valueLabel;
        controlToFieldInfoMap[valueSlider] = curType.GetField(member.Name);

        curHboxContainer.AddChild(valueSlider);
      }
    }
  }

  private void OnNumericalPropertyValueChanged(double newValue, Control control) {
    if (currentResource == null) {
      return;
    }

    FieldInfo fieldToUpdate = controlToFieldInfoMap[control];
    // TODO: looks criminal, see if there's a better way
    if (fieldToUpdate.FieldType == typeof(int)) {
      if ((int) fieldToUpdate.GetValue(currentResource) == (int) newValue) {
        return;
      }
      controlToLabelMap[control].Text = newValue.ToString();
      fieldToUpdate.SetValue(currentResource, (int) newValue);
      unsavedChangedCallback.Invoke();
      return;
    }

    float value = (float) fieldToUpdate.GetValue(currentResource);
    if (value == (float) newValue) {
      return;
    }

    controlToLabelMap[control].Text = newValue.ToString();
    fieldToUpdate.SetValue(currentResource, (float) newValue);
    unsavedChangedCallback.Invoke();
  }
}
