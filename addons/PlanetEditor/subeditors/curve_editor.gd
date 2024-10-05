@tool
class_name CurveEditor extends Control

@export var curve:Curve1D:
	set(c):
		if curve:
			curve.changed.disconnect(_on_curve_changed)
		curve = c
		if curve:
			curve.changed.connect(_on_curve_changed)
		
		_set_curve_to_lines()
@export var min_value := Vector2()
@export var max_value := Vector2(1.0, 1.0)
@export var lock_x := false
@export var lock_y := false
@export var lock_point_creation := false

var lines:Array[Line2D] = []
var indicators:Array[Line2D] = []

@onready var line_container := $LineContainer as Node2D

var bounds:Vector2:
	get: return max_value - min_value
var scaling:Vector2:
	get: return size / bounds

var _dragging_point := -1
var _dragging_offset := Vector2()

func _create_line()->void:
	var line := Line2D.new()
	line.width = 1.0
	line.antialiased = true
	line.add_point(Vector2())
	line.add_point(Vector2())
	
	lines.push_back(line)
	line_container.add_child(line)

func _pop_line()->void:
	var line := lines.pop_back() as Line2D
	line_container.remove_child(line)

func _create_indicator()->void:
	var line := Line2D.new()
	line.default_color = Color(1.0, 1.0, 1.0, 0.4)
	line.width = 1.0
	line.add_point(Vector2())
	line.add_point(Vector2())
	
	indicators.push_back(line)
	line_container.add_child(line)

func _pop_indicator()->void:
	var indicator := indicators.pop_back() as Line2D
	line_container.remove_child(indicator)

func _create_lines():
	# Synchronize lines and indicators with curve points
	while lines.size() > curve.points.size() + 1: _pop_line()
	while lines.size() < curve.points.size() + 1: _create_line()
	
	while indicators.size() > curve.points.size(): _pop_indicator()
	while indicators.size() < curve.points.size(): _create_indicator()

func _on_curve_changed()->void:
	_set_curve_to_lines()

func _set_curve_to_lines():
	if !curve || curve.points.is_empty(): return
	_create_lines()
	
	line_container.position = -min_value * scaling
	
	var current := _invert_y(curve.points.front()) * scaling
	var line := lines.front() as Line2D
	line.set_point_position(0, Vector2(min_value.x * scaling.x, current.y))
	line.set_point_position(1, current)
	
	for index in range(0, curve.points.size() - 1):
		current = _invert_y(curve.points[index]) * scaling
		var next := _invert_y(curve.points[index + 1]) * scaling
		line = lines[index + 1]
		line.set_point_position(0, current)
		line.set_point_position(1, next)
	
	current = _invert_y(curve.points.back()) * scaling
	line = lines.back() as Line2D
	line.set_point_position(0, current)
	line.set_point_position(1, Vector2(max_value.x * scaling.x, current.y))
	
	for index in curve.points.size():
		current = _invert_y(curve.points[index]) * scaling
		var indicator := indicators[index]
		current.y = min_value.y * scaling.y
		indicator.set_point_position(0, current)
		current.y = max_value.y * scaling.y
		indicator.set_point_position(1, current)

func _mouse_to_curve()->Vector2:
	return _position_to_curve(line_container.get_local_mouse_position())

func _mouse_button(event:InputEventMouseButton)->void:
	if event.button_index == MOUSE_BUTTON_LEFT:
		var mouse_position := _mouse_to_curve()
		
		var select_index = -1
		var min_dist := 32.0 / scaling.length()
		for index in curve.points.size():
			var p := curve.points[index]
			
			# Ignore axis for distance if it's locked
			if lock_x: p.y = mouse_position.y
			if lock_y: p.x = mouse_position.x
			
			var d := p.distance_to(mouse_position)
			if d > min_dist: continue
			min_dist = d
			select_index = index
		
		if event.double_click && !lock_point_creation:
			if select_index > -1:
				curve.remove_point(select_index)
			else:
				curve.add_vector2(mouse_position)
		elif event.pressed:
			_dragging_point = select_index
			
			var p := curve.points[select_index]
			_dragging_offset = p - mouse_position
		else:
			_dragging_point = -1

func _mouse_motion(event:InputEventMouseMotion)->void:
	if _dragging_point > -1:
		var mouse_position := _mouse_to_curve() + _dragging_offset
		
		mouse_position.x = clampf(
			mouse_position.x,
			min_value.x if _dragging_point == 0 else curve.points[_dragging_point - 1].x,
			max_value.x if _dragging_point == curve.points.size() - 1 else curve.points[_dragging_point + 1].x
		)
		mouse_position.y = clampf(
			mouse_position.y,
			min_value.y,
			max_value.y
		)
		
		if !lock_x: curve.points[_dragging_point].x = mouse_position.x
		if !lock_y: curve.points[_dragging_point].y = mouse_position.y
		
		curve.changed.emit()

func _gui_input(event:InputEvent)->void:
	if event is InputEventMouseButton:
		_mouse_button(event)
	elif event is InputEventMouseMotion:
		_mouse_motion(event)

func minv(a:Vector2, b:Vector2)->Vector2:
	return Vector2(minf(a.x, b.x), minf(a.y, b.y))

func maxv(a:Vector2, b:Vector2)->Vector2:
	return Vector2(maxf(a.x, b.x), maxf(a.y, b.y))

func _invert_y(v:Vector2)->Vector2:
	v.y = max_value.y - (v.y - min_value.y)
	return v

func _position_to_curve(v:Vector2)->Vector2:
	return _invert_y(v / scaling)

func _curve_to_position(v:Vector2)->Vector2:
	return _invert_y(v) * scaling

func _on_resized():
	_set_curve_to_lines()
