using Godot;
using System;

[Tool]
public partial class PlanetLayerRenderer : TextureRect {
  // Spherical distort and lighting shader
  private Shader planetLayerShader =
    ResourceLoader.Load("res://addons/planet2d/planet.gdshader") as Shader;

  [Export] private bool debugReseedPlanet = false;

  [ExportGroup("Texture")]

  // Scales texture itself in the spherical rendering. At [1.0], texture repetition could be visible at texture boundaries. [0.5] ensures no repetition is visible.
  // This will affect the range of [member texture_offset]. When set to [0.5], the range of [member texture_offset] becomes [0.0 - 2.0].
  [Export] private Vector2 textureScale = new Vector2(0.5f, 0.5f);

  // The amount to scroll the texture by. The value is in UV coordinates, [0.0 - 1.0] is a full texture size scroll, if [member texture_scale] is 1.0.
  [Export] private Vector2 textureOffset = new Vector2(1f, 0f);

  // Scales the effect of [member texture_offset]. A higher scale will result in faster scrolling for the same offset.
  [Export] private float textureOffsetScale = 100f;

  [ExportGroup("Atmosphere")]

  // Color of the atmosphere, primarily at the edges of the sphere at low values. Gives a fog effect.
  [Export] private Color atmosphereColor = Colors.White;

  // Controls the density of the atmosphere. At higher values, the texture itself will become invisible as it is enveloped by the atmosphere.
  [Export] private float atmosphereIntensity = 0f;

  [ExportGroup("Light")]

  // Color of the light affecting this layer.
  [Export] private Color lightColor = Colors.White;

  // The direction to the light source. This vector is normalized in the shader.
  [Export] private Vector3 lightDirection = new Vector3(0.0f, 0.0f, 1.0f);

  // Minimum light level.
  [Export] private float lightMinimum = 0.1f;

  // Maximum light level.
  [Export] private float lightMaximum = 1.0f;

  [ExportGroup("Specular")]

  // Color of the specular effect.
  [Export] private Color specularColor = Colors.White;

  // Specular intensity, or brightness. Reasonable values are from [0.0 - 1.0].
  [Export] private float specularIntensity = 0.2f;

  // The shininess of the material. Lower values will result in very diffused specular highlights (ex: ground). Higher values will have a smaller highlight (ex: water).
  [Export] private float specularShininess = 1f;

  [ExportGroup("Pixelize")]

  // Enable planet rendering pixelization
  [Export] private bool pixelizeEnabled = false;

  // Resolution scale of the render output. 0.5 means each pixel is 2x larger.
  [Export] private float pixelizeScale = 1f;

  private ShaderMaterial shaderMaterial;
  public override void _Ready() {
    shaderMaterial = new ShaderMaterial();
    shaderMaterial.Shader = planetLayerShader;
    this.Material = shaderMaterial;
    UpdateMaterial();
  }

  public void DisplayPlanetLayerDef(PlanetLayerDef planetLayerDef) {
    Texture = planetLayerDef.texture;
    textureScale = planetLayerDef.textureScale;
    textureOffsetScale = planetLayerDef.textureOffsetScale;
    //atmosphere
    //light
    specularColor = planetLayerDef.specularColor;
    specularIntensity = planetLayerDef.specularIntensity;
    specularShininess = planetLayerDef.specularShininess;
    UpdateMaterial();
  }

  public override void _Process(double delta) {
    shaderMaterial.SetShaderParameter("texture_offset", textureOffset * textureOffsetScale);
  }

  private void UpdateMaterial() {
    shaderMaterial.SetShaderParameter("texture_scale", textureScale);
    shaderMaterial.SetShaderParameter("texture_offset", textureOffset * textureOffsetScale);
    shaderMaterial.SetShaderParameter("atmosphere_color", atmosphereColor);
    shaderMaterial.SetShaderParameter("atmosphere_intensity", atmosphereIntensity);
    shaderMaterial.SetShaderParameter("light_color", lightColor);
    shaderMaterial.SetShaderParameter("light_direction", lightDirection);
    shaderMaterial.SetShaderParameter("light_minimum", lightMinimum);
    shaderMaterial.SetShaderParameter("light_maximum", lightMaximum);
    shaderMaterial.SetShaderParameter("specular_color", specularColor);
    shaderMaterial.SetShaderParameter("specular_intensity", specularIntensity);
    shaderMaterial.SetShaderParameter("specular_shininess", specularShininess);
    shaderMaterial.SetShaderParameter("pixelize_enabled", pixelizeEnabled);
    shaderMaterial.SetShaderParameter("pixelize_scale", pixelizeScale);
  }
}
