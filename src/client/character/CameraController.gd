extends Node3D
class_name CameraController

signal zoom_changed(is_first_person: bool)

@export_category("Camera Settings")
@export var distance: float = 8.0
@export var shoulder_distance: float = 2.5
@export var tween_speed: float = 0.2
@export var zoom_lerp_speed: float = 0.1
@export var camera_radius: float = 0.5
@export var height_offset: float = 1.5

@export_category("FOV Settings")
@export var default_fov: float = 70.0
@export var zoomed_fov_percent: float = 0.60
@export var fov_lerp_speed: float = 0.1

@onready var camera: Camera3D = $Camera3D
@onready var player: CharacterBody3D = get_parent()

var zoom_state: float = 1.0  # 0 = First Person, 1 = Third Person
var current_zoom: float = 1.0
var current_side: float = 1.0
var target_side: float = 1.0
var default_target_side: float = 1.0

var key_held_lean_left: bool = false
var key_held_lean_right: bool = false
var hold_lean: bool = true

var rot_x: float = 0.0
var rot_y: float = 0.0
var target_fov: float = default_fov

class OffsetData:
	var priority: int
	var value: Transform3D
	func _init(p: int, v: Transform3D):
		priority = p
		value = v

var camera_offsets: Dictionary = {}

func _ready() -> void:
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	camera.fov = default_fov
	target_fov = default_fov
	
	# Force the camera's local transform to zero on start to prevent the boom-pole swing
	camera.transform = Transform3D.IDENTITY

func is_first_person() -> bool:
	return zoom_state == 0.0

func set_hold_lean(hold_to_lean: bool) -> void:
	hold_lean = hold_to_lean
	if not hold_to_lean and target_side == 0.0:
		target_side = default_target_side
	elif hold_to_lean and not (key_held_lean_left or key_held_lean_right):
		target_side = 0.0

func set_offset_this_frame(key: String, priority: int, offset: Transform3D) -> void:
	camera_offsets[key] = OffsetData.new(priority, offset)

func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		rot_x -= deg_to_rad(event.relative.y * 0.3)
		rot_y -= deg_to_rad(event.relative.x * 0.3)
		rot_x = clamp(rot_x, deg_to_rad(-80), deg_to_rad(80))

	elif event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP and event.pressed:
			_change_zoom_state(0.0)  # first Person
		elif event.button_index == MOUSE_BUTTON_WHEEL_DOWN and event.pressed:
			_change_zoom_state(1.0) # third Person

func _change_zoom_state(new_state: float) -> void:
	if new_state != zoom_state:
		zoom_state = new_state
		zoom_changed.emit(zoom_state == 0.0)

func _process(delta: float) -> void:
	_handle_input_actions()

	if hold_lean and target_side != 0.0:
		target_side = target_side if (key_held_lean_left or key_held_lean_right) else 0.0

	current_zoom = lerp(current_zoom, zoom_state, delta / zoom_lerp_speed)
	current_side = lerp(current_side, target_side, delta / tween_speed)
	camera.fov = lerp(camera.fov, target_fov, delta / fov_lerp_speed)

	var basis_y := Basis(Vector3.UP, rot_y)
	var basis_x := Basis(Vector3.RIGHT, rot_x)
	var base_basis := basis_y * basis_x

	var pivot_pos := player.global_position + Vector3(0, height_offset, 0)
	var shoulder_offset := base_basis.x * (current_side * shoulder_distance)
	var camera_origin := pivot_pos + shoulder_offset
	
	# For some goddamn reason:
	# +Z is backward in Godot. This correctly pulls the camera away from the head.
	var target_offset := base_basis.z * distance
	var third_person_pos := camera_origin + target_offset

	if current_zoom > 0.1:
		var space_state := get_world_3d().direct_space_state
		var query := PhysicsShapeQueryParameters3D.new()
		var sphere := SphereShape3D.new()
		sphere.radius = camera_radius
		query.shape = sphere
		query.transform = Transform3D(Basis(), camera_origin)
		query.motion = target_offset
		query.exclude = [player.get_rid()]

		var cast_result := space_state.cast_motion(query)
		if cast_result.size() > 0:
			var safe_fraction = max(0.0, cast_result[0] - 0.02)
			third_person_pos = camera_origin + (target_offset * safe_fraction)

	var actual_pos := pivot_pos.lerp(third_person_pos, current_zoom)

	var final_offset := Transform3D.IDENTITY
	if not camera_offsets.is_empty():
		var keys = camera_offsets.keys()
		keys.sort_custom(func(a, b): return camera_offsets[a].priority < camera_offsets[b].priority)
		for key in keys:
			final_offset = final_offset * camera_offsets[key].value
		camera_offsets.clear()

	global_position = actual_pos
	global_transform.basis = base_basis

	# this kills the editor-offset rotation bug entirely
	camera.transform = final_offset

func _handle_input_actions() -> void:
	if Input.is_action_just_pressed("lean_left"):
		target_side = -1.0
		default_target_side = -1.0
		key_held_lean_left = true
	elif Input.is_action_just_released("lean_left"):
		key_held_lean_left = false
		if hold_lean and target_side != 1.0:
			target_side = default_target_side

	if Input.is_action_just_pressed("lean_right"):
		target_side = 1.0
		default_target_side = 1.0
		key_held_lean_right = true
	elif Input.is_action_just_released("lean_right"):
		key_held_lean_right = false
		if hold_lean and target_side != -1.0:
			target_side = default_target_side

	if Input.is_action_just_pressed("zoom_aim"):
		target_fov = default_fov * zoomed_fov_percent
	elif Input.is_action_just_released("zoom_aim"):
		target_fov = default_fov
