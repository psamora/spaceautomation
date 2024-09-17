class_name TextureEditor extends Control

signal changed()

@onready var layer_filter = $TextureSettings/TextureFilter/LayerFilter
@onready var texture_size = $TextureSettings/TextureSize/TextureSize
@onready var size_to_noise := $TextureSettings/TextureSize/SizeToNoise as CheckBox
@onready var seamless_blend_skirt = $TextureSettings/SeamlessBlendSkirt
@onready var texture_scale = $TextureDisplay/TextureScale
@onready var texture_offset_scale = $TextureDisplay/TextureOffsetScale

var texture:CanvasTexture

var diffuse_texture:NoiseTexture2D
var normal_texture:NoiseTexture2D
var specular_texture:NoiseTexture2D
var sub_textures:Array[NoiseTexture2D] = [diffuse_texture, normal_texture, specular_texture]

@export var layer:PlanetLayer:
	set = set_layer

func set_layer(new_layer:PlanetLayer)->void:
	layer = new_layer
	
	texture = layer.texture as CanvasTexture
	
	diffuse_texture = texture.diffuse_texture
	normal_texture = texture.normal_texture
	specular_texture = texture.specular_texture
	sub_textures = [diffuse_texture, normal_texture, specular_texture]
	
	update()

func _ready():
	update()

func update()->void:
	if !layer: return
	
	if layer_filter: layer_filter.selected = layer.texture_filter
	if texture_size: texture_size.value = Vector2(diffuse_texture.width, diffuse_texture.height)
	if seamless_blend_skirt: seamless_blend_skirt.value = diffuse_texture.seamless_blend_skirt
	
	if texture_scale: texture_scale.value = layer.texture_scale
	if texture_offset_scale: texture_offset_scale.value = layer.texture_offset_scale

func _on_texture_size_value_changed(value:Vector2)->void:
	if !texture || Vector2(diffuse_texture.width, diffuse_texture.height) == value: return
	
	if size_to_noise.button_pressed:
		var ratio := value.x / diffuse_texture.width
		var noise := diffuse_texture.noise as FastNoiseLite
		noise.frequency /= ratio
	
	_set_noise_texture_size(diffuse_texture, value)
	_set_noise_texture_size(normal_texture, value)
	_set_noise_texture_size(specular_texture, value)
	
	await diffuse_texture.changed
	
	texture.changed.emit()

func _on_seamless_blend_skirt_value_changed(value:float)->void:
	if !texture || diffuse_texture.seamless_blend_skirt == value: return
	diffuse_texture.seamless_blend_skirt = value
	normal_texture.seamless_blend_skirt = value
	specular_texture.seamless_blend_skirt = value
	
	await diffuse_texture.changed
	
	texture.changed.emit()

func _on_texture_filter_item_selected(index:int)->void:
	if !layer: return
	
	layer.texture_filter = index as TextureFilter
	texture.texture_filter = index as TextureFilter
	
	changed.emit()

func _on_texture_scale_value_changed(value:Vector2)->void:
	if !layer || layer.texture_scale == value: return
	layer.texture_scale = value
	changed.emit()

func _on_texture_offset_scale_value_changed(value:float)->void:
	if !layer || layer.texture_offset_scale == value: return
	layer.texture_offset_scale = value
	changed.emit()

func _set_noise_texture_size(noise_texture:NoiseTexture2D, dimensions:Vector2i)->void:
	noise_texture.width = dimensions.x
	noise_texture.height = dimensions.y
