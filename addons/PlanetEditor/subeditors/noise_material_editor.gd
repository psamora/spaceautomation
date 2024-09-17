class_name NoiseMaterialEditor extends Control

signal changed()

@onready var gradient_edit := $Editors/GradientEdit as GradientEdit
@onready var height_editor := $Editors/HeightEditor as CurveEditor
@onready var height_strength := $Editors/HeightStrength as LabelInputSlider
@onready var specular_editor := $Editors/SpecularEditor as CurveEditor
@onready var gradient_color_picker := $GradientColorPicker as ColorPicker

@export var texture:CanvasTexture:
	set = set_texture

var diffuse_texture:NoiseTexture2D
var normal_texture:NoiseTexture2D
var specular_texture:NoiseTexture2D

func set_texture(new_texture:CanvasTexture)->void:
	texture = new_texture
	diffuse_texture = texture.diffuse_texture
	normal_texture = texture.normal_texture
	specular_texture = texture.specular_texture
	
	var height_curve := Curve1D.new()
	var specular_curve := Curve1D.new()
	
	gradient_edit.gradient = diffuse_texture.color_ramp
	for i in diffuse_texture.color_ramp.get_point_count():
		var h := normal_texture.color_ramp.get_color(i).r
		var s := specular_texture.color_ramp.get_color(i).r
		height_curve.add_point(diffuse_texture.color_ramp.get_offset(i), h)
		specular_curve.add_point(diffuse_texture.color_ramp.get_offset(i), s)
	
	# Disconnect previous curves which will be GCed soon anyways
	if height_editor.curve: height_editor.curve.changed.disconnect(_on_height_curve_changed)
	if specular_editor.curve: specular_editor.curve.changed.disconnect(_on_specular_curve_changed)
	height_curve.changed.connect(_on_height_curve_changed)
	specular_curve.changed.connect(_on_specular_curve_changed)
	
	height_editor.curve = height_curve
	height_strength.value = normal_texture.bump_strength
	specular_editor.curve = specular_curve

func _on_gradient_edit_changed()->void:
	changed.emit()

func _on_gradient_edit_point_added(offset):
	if !texture: return
	
	var height := height_editor.curve.sample(offset)
	var specular := specular_editor.curve.sample(offset)
	
	normal_texture.color_ramp.add_point(offset, Color(height, height, height))
	specular_texture.color_ramp.add_point(offset, Color(specular, specular, specular))
	
	height_editor.curve.add_point(offset, height)
	specular_editor.curve.add_point(offset, specular)
	
	changed.emit()

func _on_gradient_edit_point_removed(index):
	normal_texture.color_ramp.remove_point(index)
	specular_texture.color_ramp.remove_point(index)
	
	height_editor.curve.remove_point(index)
	specular_editor.curve.remove_point(index)
	
	changed.emit()

func _on_gradient_edit_point_changed(index:int)->void:
	var ramp := gradient_edit.gradient
	var offset := ramp.get_offset(index)
	
	var height := height_editor.curve.points[index].y
	var specular := specular_editor.curve.points[index].y
	if Input.is_key_pressed(KEY_CTRL):
		height = height_editor.curve.sample(offset)
		specular = specular_editor.curve.sample(offset)
		height_editor.curve.points[index].y = height
		specular_editor.curve.points[index].y = specular
	
	height_editor.curve.points[index].x = ramp.get_offset(index)
	specular_editor.curve.points[index].x = ramp.get_offset(index)
	
	height_editor.curve.update()
	specular_editor.curve.update()

func _on_height_curve_changed()->void:
	if !normal_texture: return
	normal_texture.color_ramp = CurveTools.curve_to_gradient(normal_texture.color_ramp.duplicate(), height_editor.curve)

func _on_specular_curve_changed()->void:
	if !specular_texture: return
	specular_texture.color_ramp = CurveTools.curve_to_gradient(specular_texture.color_ramp.duplicate(), specular_editor.curve)

func _on_gradient_edit_point_selected(index:int)->void:
	gradient_color_picker.color = diffuse_texture.color_ramp.get_color(index)

func _on_height_strength_value_changed(value:float)->void:
	normal_texture.bump_strength = value
