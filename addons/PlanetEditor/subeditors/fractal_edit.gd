@tool
class_name FractalEdit extends Container

signal changed()

@onready var noise_type = $ControlGrid/NoiseType as OptionButton
@onready var seed_input := $ControlGrid/Seed as LabelInputSlider
@onready var frequency := $ControlGrid/Frequency as LabelInputSlider
@onready var offset := $ControlGrid/Offset as Vector2Input

@onready var octaves := $ControlGrid/Octaves as LabelInputSlider
@onready var lacunarity := $ControlGrid/Lacunarity as LabelInputSlider
@onready var gain := $ControlGrid/Gain as LabelInputSlider
@onready var weighted_strength := $ControlGrid/WeightedStrength as LabelInputSlider

var layers:Array[PlanetLayer] = []

@export var noise:FastNoiseLite:
	set = set_noise

func set_noise(new_noise:FastNoiseLite)->void:
	if noise:
		noise.changed.disconnect(_on_noise_changed)
	noise = new_noise
	if noise:
		noise.changed.connect(_on_noise_changed)
	update()

func _ready()->void:
	update()

func _on_noise_changed()->void:
	update()

func update()->void:
	if !noise: return
	
	noise_type.selected = noise.noise_type
	seed_input.value = noise.seed
	frequency.value = noise.frequency
	offset.value = Vector2(noise.offset.x, noise.offset.y)
	
	octaves.value = noise.fractal_octaves
	lacunarity.value = noise.fractal_lacunarity
	gain.value = noise.fractal_gain
	weighted_strength.value = noise.fractal_weighted_strength

func _on_noise_type_item_selected(index:int)->void:
	if !noise || noise.noise_type == index: return
	noise.noise_type = index as FastNoiseLite.NoiseType
	changed.emit()

func _on_octaves_value_changed(value:int)->void:
	if !noise || noise.fractal_octaves == value: return
	noise.fractal_octaves = value
	changed.emit()

func _on_lacunarity_value_changed(value:float)->void:
	if !noise || noise.fractal_lacunarity == value: return
	noise.fractal_lacunarity = value
	changed.emit()

func _on_gain_value_changed(value:float)->void:
	if !noise || noise.fractal_gain == value: return
	noise.fractal_gain = value
	changed.emit()

func _on_seed_value_changed(value:int)->void:
	if !noise || noise.seed == value: return
	noise.seed = value
	changed.emit()

func _on_weighted_strength_value_changed(value:float)->void:
	if !noise || noise.fractal_weighted_strength == value: return
	noise.fractal_weighted_strength = value
	changed.emit()

func _on_offset_value_changed(value:Vector2)->void:
	if !noise || Vector2(noise.offset.x, noise.offset.y) == value: return
	noise.offset.x = value.x
	noise.offset.y = value.y
	changed.emit()

func _on_frequency_value_changed(value:float)->void:
	if !noise || noise.frequency == value: return
	noise.frequency = value
	changed.emit()
