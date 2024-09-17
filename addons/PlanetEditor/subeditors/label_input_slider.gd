@tool
class_name LabelInputSlider extends Control

signal value_changed(value:float)

@onready var label_txt := $Label as Label
@onready var slider := $Slider as Slider
@onready var input_txt := $LineEdit as LineEdit

@export var rounded := false:
	set(r):
		rounded = r
		value = value

@export var label := "":
	set(l):
		label = l
		if label_txt: label_txt.text = label

@export var min_value := 0.0:
	set(mv):
		min_value = mv
		value = clampf(value, min_value, max_value)
		if slider: slider.min_value = min_value

@export var max_value := 1.0:
	set(mv):
		max_value = mv
		value = clampf(value, min_value, max_value)
		if slider: slider.max_value = max_value

@export var value := 0.0:
	set(v):
		value = clampf(v, min_value, max_value)
		if rounded: value = round(value)
		if slider: slider.value = value
		if input_txt && !input_txt.has_focus(): input_txt.text = str(value)
		
		value_changed.emit(value)

@export var step := 0.1:
	set(s):
		step = s
		if slider: slider.step = step

func _ready():
	label_txt.text = label
	slider.min_value = min_value
	slider.max_value = max_value
	slider.value = value
	input_txt.text = str(value)
	slider.step = step

func _on_slider_value_changed(value:float):
	if self.value == value: return
	self.value = value

func _on_line_edit_text_changed(text:String):
	if self.value == float(text): return
	value = float(text)

func _on_line_edit_focus_exited():
	if self.value == float(input_txt.text): return
	value = float(input_txt.text)

func _on_line_edit_text_submitted(text:String):
	input_txt.text = str(value)
