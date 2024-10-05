using Godot;
using System;

[Tool]
[GlobalClass]
public partial class PlanetLayerDef : Resource {
  [Export] public CanvasTexture texture;
  [Export] public CanvasItem.TextureFilterEnum textureFilter = CanvasItem.TextureFilterEnum.ParentNode;
  [Export] public Vector2 textureScale = new Vector2(0.5f, 0.5f);
  [Export] public float textureOffsetScale = 1f;

  [Export] public Color specularColor = Colors.White;
  [Export] public float specularIntensity = 0.2f;
  [Export] public float specularShininess = 1f;

  public PlanetLayerDef() {
    texture = new CanvasTexture();
    FastNoiseLite noise = new FastNoiseLite();
    noise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
    noise.Seed = 0;
    noise.FractalOctaves = 5;
    noise.FractalLacunarity = 2;
    noise.FractalGain = 0.5f;
    noise.Frequency = 0.009999f;
    noise.Offset = new Vector3(0, 0, 0);
    noise.FractalWeightedStrength = 0;

    NoiseTexture2D diffuseTexture = new NoiseTexture2D();
    diffuseTexture.ColorRamp = new Gradient();
    diffuseTexture.Noise = noise;
    diffuseTexture.Seamless = true;
    diffuseTexture.SeamlessBlendSkirt = 0.1f;
    diffuseTexture.GenerateMipmaps = true;
    texture.DiffuseTexture = diffuseTexture;

    NoiseTexture2D normalTexture = new NoiseTexture2D();
    normalTexture.ColorRamp = new Gradient();
    normalTexture.Noise = noise;
    normalTexture.Seamless = true;
    normalTexture.SeamlessBlendSkirt = 0.1f;
    normalTexture.AsNormalMap = true;
    normalTexture.BumpStrength = 8;
    normalTexture.GenerateMipmaps = true;
    texture.NormalTexture = normalTexture;

    NoiseTexture2D specularTexture = new NoiseTexture2D();
    specularTexture.ColorRamp = new Gradient();
    specularTexture.Noise = noise;
    specularTexture.Seamless = true;
    specularTexture.SeamlessBlendSkirt = 0.1f;
    specularTexture.GenerateMipmaps = true;
    texture.SpecularTexture = specularTexture;
  }
}
