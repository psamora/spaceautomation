@tool
class_name FloatInput extends HBoxContainer

signal value_changed(value:float)

@onready var label_txt := $Label as Label
@onready var line_edit_txt := $LineEdit as LineEdit

@export var label := "":
	set(l):
		label = l
		if label_txt: label_txt.text = label

@export var value := 0.0:
	set(v):
		if value == v: return
		value = v
		if line_edit_txt && !line_edit_txt.has_focus(): line_edit_txt.text = str(value)
		
		value_changed.emit(value)

@export var deferred_mode := false

func _ready():
	if label_txt: label_txt.text = label
	if line_edit_txt && !line_edit_txt.has_focus(): line_edit_txt.text = str(value)

func _on_line_edit_text_changed(text:String):
	if !deferred_mode:
		value = float(text)

func _on_line_edit_text_submitted(text:String):
	line_edit_txt.release_focus()
	value = value

func _on_line_edit_focus_exited():
	value = float(line_edit_txt.text)
