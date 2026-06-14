extends CharacterBody3D
class_name CharacterController

const SPEED = 5.0
const JUMP_VELOCITY = 10.2
var coyote_timer := 0.0
const COYOTE_TIME_LIMIT := 0.15 # Seconds
const JUMP_VELOCITY_MIN := 0.6  # For short hops

@export var jumping := false

@onready var camera_pivot: CameraController = $CameraPivot

func _enter_tree() -> void:
	# This is a terrible way to do this.
	# But I have no choice since we can't bridge CS to GDScript.
	var peer_id: int = name.to_int()
	set_multiplayer_authority(peer_id)

	for child in get_children():
		if child is MultiplayerSynchronizer:
			child.set_multiplayer_authority(peer_id)

func _ready() -> void:
	var is_mine: bool = get_multiplayer_authority() == multiplayer.get_unique_id()
	set_process(is_mine)

func _physics_process(delta: float) -> void:
	if not is_multiplayer_authority():
		return

	if is_on_floor():
		coyote_timer = COYOTE_TIME_LIMIT
	else:
		coyote_timer -= delta

	if not is_on_floor():
		velocity += get_gravity() * delta

	if Input.is_action_just_pressed("jump") and coyote_timer > 0:
		velocity.y = JUMP_VELOCITY
		jump.rpc()
		coyote_timer = 0 # Prevent double jumping

	# variable Jump Height: if player releases jump button while still going up
	if Input.is_action_just_released("jump") and velocity.y > JUMP_VELOCITY_MIN:
		velocity.y = JUMP_VELOCITY_MIN

	var input_dir := Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	# calculate direction relative to Camera Rotation instead of Character self-transform
	var camera_basis: Basis = camera_pivot.global_transform.basis
	var forward := Vector3(camera_basis.z.x, 0, camera_basis.z.z).normalized()
	var right := Vector3(camera_basis.x.x, 0, camera_basis.x.z).normalized()
	
	# roblox lookvector is negative Z, so move_back pushes us along positive Z, move_left along negative X
	var direction := (forward * input_dir.y + right * input_dir.x).normalized()

	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
		
		# Only turn character body if NOT aiming or in 1st person
		if not camera_pivot.is_first_person() and not Input.is_action_pressed("zoom_aim"):
			var target_angle: float = atan2(-direction.x, -direction.z)
			rotation.y = rotation.y + angle_difference(rotation.y, target_angle) * 0.15
	else:
		velocity.x = move_toward(velocity.x, 0, SPEED)
		velocity.z = move_toward(velocity.z, 0, SPEED)

	# Force face along camera if in First Person or Aiming (Roblox CharacterController behavior)
	if camera_pivot.is_first_person() or Input.is_action_pressed("zoom_aim"):
		var camera_y_rot: float = camera_pivot.rot_y
		rotation.y = camera_y_rot

	move_and_slide()
	
@rpc("call_local")
func jump() -> void:
	jumping = true
