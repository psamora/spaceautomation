@tool
class_name Curve1D extends Resource

@export var interpolation_mode := Gradient.GRADIENT_INTERPOLATE_LINEAR
@export var points:Array[Vector2] = []

var bounds:Rect2:
	get = get_bounds

func sample(t:float)->float:
	if t <= points.front().x:
		return points.front().y
	if t >= points.back().x:
		return points.back().y
	
	for i in range(1, points.size()):
		var point := points[i]
		if t > point.x: continue
		var previous := points[i - 1]
		var d := point - previous
		var ct := (t - previous.x) / d.x
		match interpolation_mode:
			Gradient.GRADIENT_INTERPOLATE_LINEAR: return lerpf(previous.y, point.y, ct)
			Gradient.GRADIENT_INTERPOLATE_CUBIC:
				var pre := points[i - 2] if i > 1 else previous
				var post := points[i + 1] if i < points.size() - 1 else point
				return cubic_interpolate(previous.y, point.y, pre.y, post.y, ct)
			_: return previous.y
	
	return 0.0 # Should never reach this point

func update()->void:
	points.sort_custom(func (a:Vector2, b:Vector2): return a.x < b.x)
	changed.emit()

func add_vector2(v:Vector2)->void:
	points.push_back(v)
	update()

func add_point(x:float, y:float)->void:
	add_vector2(Vector2(x, y))

func remove_point(index:int)->void:
	points.remove_at(index)
	changed.emit()

func get_bounds()->Rect2:
	if points.is_empty(): return Rect2()
	var min_value := points[0]
	var max_value := points[0]
	for i in range(1, points.size()):
		var point := points[i]
		min_value = _minv(min_value, point)
		max_value = _maxv(max_value, point)
	return Rect2(min_value, max_value - min_value)

func _minv(a:Vector2, b:Vector2)->Vector2:
	return Vector2(minf(a.x, b.x), minf(a.y, b.y))

func _maxv(a:Vector2, b:Vector2)->Vector2:
	return Vector2(maxf(a.x, b.x), maxf(a.y, b.y))
