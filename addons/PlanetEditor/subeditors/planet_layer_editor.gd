class_name PlanetLayerEditor extends MarginContainer

signal changed()
signal size_changed()
signal material_changed()
signal deleted()

@onready var layer_name := $Controls/Header/LayerName as LineEdit
@onready var atmosphere_color := $Controls/Atmosphere/Label/AtmosphereColor as ColorPickerButton
@onready var atmosphere_intensity := $Controls/Atmosphere/AtmosphereIntensity as LabelInputSlider

@onready var specular_color := $Controls/Material/SpecularColorContainer/SpecularColor as ColorPickerButton
@onready var specular_intensity := $Controls/Material/SpecularIntensity as LabelInputSlider
@onready var shininess := $Controls/Material/Shininess as LabelInputSlider

@onready var texture_editor := $Controls/Texture/TextureEditor as TextureEditor
@onready var noise_material_editor := $Controls/Material/NoiseMaterialEditor as NoiseMaterialEditor
@onready var fractal_editor := $Controls/Fractal/FractalEdit as FractalEdit

var noise:FastNoiseLite

var texture:CanvasTexture
var diffuse_texture:NoiseTexture2D
var normal_texture:NoiseTexture2D
var specular_texture:NoiseTexture2D
var textures:Array[NoiseTexture2D] = [diffuse_texture, normal_texture, specular_texture]

@export var layer:PlanetLayer:
	set = set_layer

func set_layer(new_layer:PlanetLayer)->void:
	layer = new_layer
	texture = layer.texture as CanvasTexture
	
	diffuse_texture = texture.diffuse_texture
	normal_texture = texture.normal_texture
	specular_texture = texture.specular_texture
	textures = [diffuse_texture, normal_texture, specular_texture]
	
	noise = diffuse_texture.noise
	
	update()

func _ready()->void:
	update()

func update()->void:
	if !layer: return
	
	if !layer_name: return
	
	layer_name.text = layer.name
	
	atmosphere_color.color = layer.atmosphere_color
	atmosphere_intensity.value = layer.atmosphere_intensity
	
	specular_color.color = layer.specular_color
	specular_intensity.value = layer.specular_intensity
	shininess.value = layer.specular_shininess
	
	texture_editor.layer = layer
	noise_material_editor.texture = texture
	fractal_editor.noise = noise

func _on_text_edit_text_changed(text:String)->void:
	if !layer || layer.name == text: return
	layer.name = text
	name = layer.name
	changed.emit()

func _on_atmosphere_color_color_changed(color:Color)->void:
	if !layer || layer.atmosphere_color == color: return
	layer.atmosphere_color = color
	changed.emit()

func _on_atmosphere_intensity_value_changed(value:float)->void:
	if !layer || layer.atmosphere_intensity == value: return
	layer.atmosphere_intensity = value
	changed.emit()

func _on_specular_color_color_changed(color:Color)->void:
	if !layer || layer.specular_color == color: return
	layer.specular_color = color
	changed.emit()

func _on_specular_intensity_value_changed(value:float)->void:
	if !layer || layer.specular_intensity == value: return
	layer.specular_intensity = value
	changed.emit()

func _on_shininess_value_changed(value:float)->void:
	if !layer || layer.specular_shininess == value: return
	layer.specular_shininess = value
	changed.emit()

func _on_check_button_toggled(button_pressed:bool)->void:
	if !layer || layer.visible == button_pressed: return
	layer.visible = button_pressed

func _on_fractal_edit_changed()->void:
	changed.emit()

func _on_delete_pressed()->void:
	deleted.emit()
