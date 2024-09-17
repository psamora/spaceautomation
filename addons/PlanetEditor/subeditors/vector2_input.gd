@tool
class_name Vector2Input extends HBoxContainer

signal value_changed(value:Vector2)

@onready var label_txt := $Label as Label
@onready var x_txt := $X as FloatInput
@onready var y_txt := $Y as FloatInput
@onready var is_aspect_btn := $IsAspect as BaseButton

@export var label := "":
	set(l):
		label = l
		if label_txt: label_txt.text = label

@export var value := Vector2():
	set(v):
		value = v
		if x_txt: x_txt.value = value.x
		if y_txt: y_txt.value = value.y
		
		value_changed.emit(value)

@export var is_aspect:bool:
	set(ia):
		is_aspect = ia
		if is_aspect_btn: is_aspect_btn.button_pressed = is_aspect

@export var is_aspect_editable:bool:
	set(iae):
		is_aspect_editable = iae
		if is_aspect_btn: is_aspect_btn.visible = is_aspect_editable
@export var deferred_mode := false:
	set(dm):
		deferred_mode = dm
		if x_txt:
			x_txt.deferred_mode = deferred_mode
			y_txt.deferred_mode = deferred_mode

func _ready()->void:
	if label_txt: label_txt.text = label
	if x_txt: x_txt.value = value.x
	if y_txt: y_txt.value = value.y
	
	if x_txt:
		x_txt.deferred_mode = deferred_mode
		y_txt.deferred_mode = deferred_mode
	
	is_aspect_btn.button_pressed = is_aspect
	is_aspect_btn.visible = is_aspect_editable

func _on_x_value_changed(v:float):
	if value.x == v: return
	value.x = v
	if is_aspect_btn.button_pressed: value.y = value.x
	value = value

func _on_y_value_changed(v:float):
	if value.y == v: return
	value.y = v
	if is_aspect_btn.button_pressed: value.x = value.y
	value = value
