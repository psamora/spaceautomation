@tool
class_name CurveTools

static func curve_to_gradient(target:Gradient, curve:Curve1D)->Gradient:
	assert(target.get_point_count() == curve.points.size())
	for index in curve.points.size():
		var point := curve.points[index]
		target.set_color(index, Color(point.y, point.y, point.y))
		target.set_offset(index, point.x)
	return target

static func create_gradient_from_curve(curve:Curve1D)->Gradient:
	var target := Gradient.new()
	for point in curve.points:
		target.add_point(point.x, Color(point.y, point.y, point.y))
	return target
