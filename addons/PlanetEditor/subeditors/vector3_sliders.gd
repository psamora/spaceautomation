@tool
class_name Vector3Sliders extends VBoxContainer

signal value_changed(vector:Vector3)

@export var label := "":
	set(l):
		label = l
		if label_txt: label_txt.text = label

@export var value := Vector3():
	set(v):
		value = v
		if R:
			R.value = value.x
			G.value = value.y
			B.value = value.z

@export var min_value := Vector3():
	set(m):
		min_value = m
		if R:
			R.min_value = min_value.x
			G.min_value = min_value.y
			B.min_value = min_value.z

@export var max_value := Vector3(1.0, 1.0, 1.0):
	set(m):
		max_value = m
		if R:
			R.max_value = max_value.x
			G.max_value = max_value.y
			B.max_value = max_value.z

@onready var label_txt := $Label as Label
@onready var R := $R as LabelInputSlider
@onready var G := $G as LabelInputSlider
@onready var B := $B as LabelInputSlider

func _ready():
	label_txt.text = label
	
	R.value = value.x
	G.value = value.y
	B.value = value.z
	
	R.min_value = min_value.x
	G.min_value = min_value.y
	B.min_value = min_value.z
	
	R.max_value = max_value.x
	G.max_value = max_value.y
	B.max_value = max_value.z

func _on_r_value_changed(v:float):
	if value.x == v: return
	value.x = v
	value_changed.emit(value)

func _on_g_value_changed(v:float):
	if value.y == v: return
	value.y = v
	value_changed.emit(value)

func _on_b_value_changed(v:float):
	if value.z == v: return
	value.z = v
	value_changed.emit(value)
