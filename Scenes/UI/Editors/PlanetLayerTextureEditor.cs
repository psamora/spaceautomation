using Godot;
using System;
using System.Diagnostics;

[Tool]
public partial class PlanetLayerTextureEditor : Control {
  [Signal] public delegate void PlanetLayerChangedEventHandler();

  [Export] CanvasTexture curLayerTexture;

  private OptionButton layerFilter;
  private CheckBox sizeToNoiseRatio;

  // Scripts currently in GDScript only
  private GodotObject textureSize;
  private GodotObject seamlessBlendSkirt;
  private GodotObject textureScale;
  private GodotObject textureOffsetScale;
  private ColorPickerButton specularColor;
  private GodotObject specularIntensity;
  private GodotObject specularShininess;
  private GodotObject noiseMaterialEditor;
  private GodotObject fractalEditor;

  private PlanetLayerDef currentPlanetLayer;
  private NoiseTexture2D diffuseTexture;
  private NoiseTexture2D normalTexture;
  private NoiseTexture2D specularTexture;
  private NoiseTexture2D[] subTextures;

  public override void _Ready() {
    layerFilter = GetNode<OptionButton>("%LayerFilter");
    layerFilter.ItemSelected += OnLayerFilterItemSelected;

    sizeToNoiseRatio = GetNode<CheckBox>("%SizeToNoise");

    specularColor = GetNode<ColorPickerButton>("%SpecularColor");
    specularColor.ColorChanged += OnSpecularColorChanged;

    // All of the elements below are in GDScript until migrate (or until never :D)
    textureSize = GetNode<GodotObject>("%TextureSize");
    textureSize.Connect("value_changed", Callable.From<Vector2>(OnTextureSizeValueChanged));

    seamlessBlendSkirt = GetNode<GodotObject>("%SeamlessBlendSkirt");
    seamlessBlendSkirt.Connect("value_changed", Callable.From<float>(OnSeamlessBlendSkirtValueChanged));

    textureScale = GetNode<GodotObject>("%TextureScale");
    textureScale.Connect("value_changed", Callable.From<Vector2>(OnTextureScaleValueChanged));

    textureOffsetScale = GetNode<GodotObject>("%TextureOffsetScale");
    textureOffsetScale.Connect("value_changed", Callable.From<float>(OnTextureOffsetScaleValueChanged));

    specularIntensity = GetNode<GodotObject>("%SpecularIntensity");
    specularIntensity.Connect("value_changed", Callable.From<float>(OnSpecularIntensityChanged));

    specularShininess = GetNode<GodotObject>("%SpecularShininess");
    specularShininess.Connect("value_changed", Callable.From<float>(OnSpecularShininessChanged));

    noiseMaterialEditor = GetNode<GodotObject>("%NoiseMaterialEditor");
    noiseMaterialEditor.Connect("changed", Callable.From(OnNoiseMaterialChanged));

    fractalEditor = GetNode<GodotObject>("%FractalEditor");
    fractalEditor.Connect("changed", Callable.From(OnFractalEditorChanged));
  }

  public void SetLayer(PlanetLayerDef currentPlanetLayer) {
    this.currentPlanetLayer = currentPlanetLayer;
    this.curLayerTexture = currentPlanetLayer.texture;
    this.diffuseTexture = curLayerTexture.DiffuseTexture as NoiseTexture2D;
    this.normalTexture = curLayerTexture.NormalTexture as NoiseTexture2D;
    this.specularTexture = curLayerTexture.SpecularTexture as NoiseTexture2D;
    subTextures = new NoiseTexture2D[3] { diffuseTexture, normalTexture, specularTexture };
    noiseMaterialEditor.Call("set_noise_texture", curLayerTexture);
    fractalEditor.Call("set_noise", diffuseTexture.Noise);
    Update();
  }

  public override void _Process(double delta) {
    //Update();
  }

  private void Update() {
    layerFilter.Selected = (int) curLayerTexture.TextureFilter;
    textureSize.Set("value", new Vector2(diffuseTexture.GetWidth(), diffuseTexture.GetHeight()));
    seamlessBlendSkirt.Set("value", diffuseTexture.SeamlessBlendSkirt);
    textureScale.Set("value", currentPlanetLayer.textureScale);
    textureOffsetScale.Set("value", currentPlanetLayer.textureOffsetScale);
    specularColor.Color = currentPlanetLayer.specularColor;
    specularIntensity.Set("value", currentPlanetLayer.specularIntensity);
    specularShininess.Set("value", currentPlanetLayer.specularShininess);
  }

  private async void OnTextureSizeValueChanged(Vector2 newTextureSize) {
    // TODO: understand what this does
    if (sizeToNoiseRatio.ButtonPressed) {
      float ratio = newTextureSize.X / diffuseTexture.Width;
      FastNoiseLite noise = diffuseTexture.Noise as FastNoiseLite;
      noise.Frequency /= ratio;
    }

    foreach (NoiseTexture2D noiseTexture in subTextures) {
      noiseTexture.Width = (int) newTextureSize.X;
      noiseTexture.Height = (int) newTextureSize.Y;
    }

    await ToSignal(diffuseTexture, "changed");
    EmitSignal(SignalName.PlanetLayerChanged);
  }

  private async void OnSeamlessBlendSkirtValueChanged(float value) {
    foreach (NoiseTexture2D noiseTexture in subTextures) {
      noiseTexture.SeamlessBlendSkirt = value;
    }
    await ToSignal(diffuseTexture, "changed");
    EmitSignal(SignalName.PlanetLayerChanged);
  }

  private void OnLayerFilterItemSelected(long index) {
    currentPlanetLayer.textureFilter = (TextureFilterEnum) index;
    curLayerTexture.TextureFilter = (TextureFilterEnum) index;
  }

  private void OnTextureScaleValueChanged(Vector2 newTextureScale) {
    currentPlanetLayer.textureScale = newTextureScale;
    EmitSignal(SignalName.PlanetLayerChanged);
  }

  private void OnTextureOffsetScaleValueChanged(float newTextureOffsetScale) {
    currentPlanetLayer.textureOffsetScale = newTextureOffsetScale;
    EmitSignal(SignalName.PlanetLayerChanged);
  }


  private void OnSpecularColorChanged(Color color) {
    currentPlanetLayer.specularColor = color;
    EmitSignal(SignalName.PlanetLayerChanged);
  }

  private void OnSpecularIntensityChanged(float newSpecularIntensity) {
    currentPlanetLayer.specularIntensity = newSpecularIntensity;
    EmitSignal(SignalName.PlanetLayerChanged);
  }

  private void OnSpecularShininessChanged(float newSpecularShininess) {
    currentPlanetLayer.specularShininess = newSpecularShininess;
    EmitSignal(SignalName.PlanetLayerChanged);
  }

  private void OnNoiseMaterialChanged() {
    EmitSignal(SignalName.PlanetLayerChanged);
  }

  private void OnFractalEditorChanged() {
    EmitSignal(SignalName.PlanetLayerChanged);
  }
}


