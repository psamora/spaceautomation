class_name GradientEdit extends Control

signal changed()

signal point_added(offset:float)
signal point_removed(index:int)
signal point_changed(index:int)

signal point_selected(index:int)

@onready var gradient_background := $GradientBackground as TextureRect
@onready var gradient_texture := $GradientTexture as TextureRect
@onready var color_cursor := $ColorCursor as Line2D
@onready var overlay := $Overlay as Control
@onready var color_draggables := $ColorDraggables as Control

@export var is_active := true:
	set = set_is_active

@export var gradient:Gradient:
	set(g):
		gradient = null
		while !points.is_empty(): _remove_point(points.back())
		gradient = g
		
		if gradient:
			for i in gradient.get_point_count():
				_create_point(i, gradient.get_offset(i), gradient.get_color(i))
		if gradient_texture: (gradient_texture.texture as GradientTexture1D).gradient = gradient
		_sync_point_indices()
		
@export var draggable_height := 32.0:
	set(dh):
		draggable_height = dh
		if color_draggables: color_draggables.custom_minimum_size.y = draggable_height

var points:Array[ColorPoint] = []

var _drag_start_mouse_position:Vector2
var _drag_start_offset:float
var _current_dragging_point:ColorPoint
var _selected_point:ColorPoint
var selected_index := -1

class ColorPoint:
	var index := 0
	var offset := 0.0
	var color := Color()
	
	var stylebox_normal:StyleBoxFlat
	var stylebox_hover:StyleBoxFlat
	var stylebox_pressed:StyleBoxFlat
	
	var button := Button.new()
	var indicator := Line2D.new()
	
	func _init(index:int, offset:float, color:Color)->void:
		self.index = index
		self.offset = offset
		self.color = color
		
		button.size = Vector2(8.0, 32.0)
		indicator.visible = false
		indicator.width = 1.0
		indicator.add_point(Vector2(0.0, 0.0))
		indicator.add_point(Vector2(0.0, 32.0))
		
		stylebox_normal = button.get_theme_stylebox("normal").duplicate()
		button.add_theme_stylebox_override("normal", stylebox_normal)
		stylebox_normal.border_color = Color(0.5, 0.5, 0.5, 1.0)
		stylebox_normal.set_border_width_all(2)
		
		stylebox_hover = button.get_theme_stylebox("hover").duplicate()
		button.add_theme_stylebox_override("hover", stylebox_hover)
		stylebox_hover.border_color = Color(0.8, 0.8, 0.8, 1.0)
		stylebox_hover.set_border_width_all(1)
		
		stylebox_pressed = button.get_theme_stylebox("pressed").duplicate()
		button.add_theme_stylebox_override("pressed", stylebox_pressed)
		stylebox_pressed.border_color = Color(0.8, 0.8, 0.8, 1.0)
		stylebox_pressed.set_border_width_all(1)
		
		update_color()
	
	func update_color()->void:
		stylebox_normal.bg_color = color
		stylebox_hover.bg_color = color
		stylebox_pressed.bg_color = color
		indicator.default_color = color.inverted()

func _generate_gradient_background()->ImageTexture:
	var image := Image.create(64, 64, false, Image.FORMAT_RGB8)
	image.fill_rect(Rect2i(0, 0, 64, 64), Color(0.4, 0.4, 0.4))
	image.fill_rect(Rect2i(0, 0, 32, 32), Color(0.3, 0.3, 0.3))
	image.fill_rect(Rect2i(32, 32, 32, 32), Color(0.3, 0.3, 0.3))
	
	return ImageTexture.create_from_image(image)

func _ready():
	color_draggables.visible = is_active
	
	color_draggables.custom_minimum_size.y = draggable_height
	gradient_background.texture = _generate_gradient_background()
	if gradient:
		for i in gradient.get_point_count():
			_create_point(i, gradient.get_offset(i), gradient.get_color(i))
	
	var indicator_size := Vector2(0.0, overlay.size.y)
	color_cursor.set_point_position(1, indicator_size)
	for p in points: _update_size(p)
	
	(gradient_texture.texture as GradientTexture1D).gradient = gradient

func _create_point(index:int, offset:float, color:Color)->ColorPoint:
	if !color_draggables || !overlay: return
	
	var point := ColorPoint.new(index, offset, color)
	point.button.button_down.connect(_on_color_point_down.bind(point))
	point.button.button_up.connect(_on_color_point_up.bind(point))
	point.button.gui_input.connect(_color_point_gui_input.bind(point))
	
	points.push_back(point)
	
	color_draggables.add_child(point.button)
	overlay.add_child(point.indicator)
	
	_update_size(point)
	_update_position(point)
	
	return point

func _add_point(offset:float)->ColorPoint:
	if !gradient: return
	
	var color := gradient.sample(offset)
	var point := _create_point(0, offset, color)
	gradient.add_point(offset, color)
	_sync_point_indices()
	
	point_added.emit(offset)
	
	return point

func _remove_point(point:ColorPoint)->void:
	color_draggables.remove_child(point.button)
	overlay.remove_child(point.indicator)
	if gradient: gradient.remove_point(point.index)
	points.erase(point)

func _color_point_gui_input(event:InputEvent, point:ColorPoint)->void:
	if event is InputEventMouseButton:
		var mb_event := event as InputEventMouseButton
		if mb_event.button_index == MOUSE_BUTTON_RIGHT || mb_event.double_click:
			_remove_point(point)
			point_removed.emit(point.index)
			_sync_point_indices()
			_select_point(null)
			changed.emit()
	elif event is InputEventMouseMotion: _on_mouse_motion(event)

func _gui_input(event):
	if event is InputEventMouseMotion:
		_on_mouse_motion(event)

func _sync_point_indices()->void:
	points.sort_custom(func(a:ColorPoint, b:ColorPoint): return a.offset < b.offset)
	for i in points.size():
		points[i].index = i
		if gradient: gradient.set_color(i, points[i].color)

func _on_mouse_motion(event:InputEventMouseMotion)->void:
	var _current_point := _current_dragging_point
	var _mouse_position := overlay.get_local_mouse_position()
	
	color_cursor.position.x = _mouse_position.x
	var cursor_offset := clampf(color_cursor.position.x / overlay.size.x, 0.0, 1.0)
	if gradient: color_cursor.default_color = gradient.sample(cursor_offset).inverted()
	
	if _current_dragging_point:
		var dm := _mouse_position.x - _drag_start_mouse_position.x
		var dmo := dm / overlay.size.x
		var offset := _drag_start_offset + dmo
		
		var previous_offset := _current_dragging_point.offset
		var current_offset := clampf(offset, 0.0, 1.0)
		
		if event.shift_pressed:
			var min_ratio := current_offset / previous_offset
			var max_ratio := (1.0 - current_offset) / (1.0 - previous_offset)
			
			for point in points:
				if point.offset < previous_offset:
					point.offset = point.offset * min_ratio
				elif point.offset > previous_offset:
					var inv := 1.0 - point.offset
					point.offset = 1.0 - (inv * max_ratio)
				_update_position(point)
				if gradient: gradient.set_offset(point.index, point.offset)
				point_changed.emit(point.index)
		
		_current_dragging_point.offset = current_offset
		_update_position(_current_dragging_point)
		if gradient: gradient.set_offset(_current_dragging_point.index, _current_dragging_point.offset)
		point_changed.emit(_current_dragging_point.index)
		
		_sync_point_indices()
		changed.emit()
	
	if !_current_point:
		var min_d := 0.1
		for p in points:
			var mo := _mouse_position.x / overlay.size.x
			var do := absf(mo - p.offset)
			if do < min_d:
				min_d = do
				_current_point = p
	
	if _current_point: _current_point.button.move_to_front()

func _select_point(point:ColorPoint)->void:
	_selected_point = point
	if _selected_point:
		selected_index = _selected_point.index
		point_selected.emit(_selected_point.index)
	else:
		selected_index = -1

func _update_position(point:ColorPoint)->void:
	if !overlay: return
	var point_x := overlay.size.x * point.offset
	var button_x := point_x - point.button.size.x * 0.5
	point.button.position.x = clampf(button_x, 0.0, overlay.size.x - point.button.size.x)
	point.indicator.position.x = point_x

func _update_size(point:ColorPoint)->void:
	if !color_draggables: return
	point.button.custom_minimum_size.y = color_draggables.size.y
	var indicator_size := Vector2(0.0, overlay.size.y)
	point.indicator.set_point_position(1, indicator_size)

func set_is_active(enabled:bool)->void:
	if is_active == enabled: return
	
	is_active = enabled
	if color_draggables:
		color_draggables.visible = is_active
		color_cursor.visible = is_active

func _on_color_point_down(point:ColorPoint):
	color_cursor.visible = false
	point.indicator.visible = true
	_current_dragging_point = point
	_select_point(point)
	
	_drag_start_mouse_position = overlay.get_local_mouse_position()
	_drag_start_offset = point.offset

func _on_color_point_up(point:ColorPoint):
	color_cursor.visible = true
	point.indicator.visible = false
	_current_dragging_point = null

func set_color(color:Color):
	if !gradient || selected_index == -1: return
	
	var point := points[selected_index]
	if !point || point.color == color: return
	
	point.color = color
	point.update_color()
	gradient.set_color(point.index, color)
	
	changed.emit()

func _on_color_draggables_resized():
	if !color_draggables: return
	for p in points:
		p.button.custom_minimum_size.y = color_draggables.size.y
		_update_position(p)

func _on_overlay_resized():
	if !overlay: return
	
	var indicator_size := Vector2(0.0, overlay.size.y)
	color_cursor.set_point_position(1, indicator_size)
	for p in points: _update_size(p)

func _on_overlay_gui_input(event:InputEvent):
	if !is_active: return
	
	if event is InputEventMouseButton:
		var mb_event := event as InputEventMouseButton
		if mb_event.double_click:
			var _mouse_position := overlay.get_local_mouse_position()
			var _mouse_offset := clampf(_mouse_position.x / overlay.size.x, 0.0, 1.0)
			var point := _add_point(_mouse_offset)
			_select_point(point)
	elif event is InputEventMouseMotion: _on_mouse_motion(event)

func _on_color_draggables_gui_input(event):
	if !is_active: return
	
	if event is InputEventMouseButton:
		var mb_event := event as InputEventMouseButton
		if mb_event.double_click:
			var _mouse_position := overlay.get_local_mouse_position()
			var _mouse_offset := clampf(_mouse_position.x / overlay.size.x, 0.0, 1.0)
			var point := _add_point(_mouse_offset)
			_select_point(point)
	elif event is InputEventMouseMotion: _on_mouse_motion(event)
