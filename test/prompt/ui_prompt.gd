extends Node3D
class_name UIPromptRenderer

@export var key_text: String = "E" : 
	set(value):
		key_text = value
		if is_inside_tree(): update_prompt(key_text, action_text, object_text)

@export var action_text: String = "Open" :
	set(value):
		action_text = value
		if is_inside_tree(): update_prompt(key_text, action_text, object_text)

@export var object_text: String = "Chest" :
	set(value):
		object_text = value
		if is_inside_tree(): update_prompt(key_text, action_text, object_text)

@export var prompt_font: Font :
	set(value):
		prompt_font = value
		if is_inside_tree(): _apply_font()

@export var omni_dir: bool = true:
	set(value):
		omni_dir = value
		if is_inside_tree(): _update_billboard_mode()

@export var promptVisibilityState: PromptVisibilityState = PromptVisibilityState.PROMPT_ENABLED

enum PromptVisibilityState { PROMPT_ENABLED, PROMPT_DISABLED, PROMPT_KEY_DISABLED }

@export var background_color: Color = Color(0.07, 0.07, 0.07, 0.8)
const cornerRadius: int = 4

const outerXMargin: float = 4.0
const outerYMargin: float = 4.0

const actionTextSize: int = 18
const objectTextSize: int = 10
const textVerticalSpacing: float = -2 # Higher = farther

var sub_viewport: SubViewport
var sprite_3d: Sprite3D
var main_panel: PanelContainer
var key_label: Label
var action_label: Label
var object_label: Label

func _ready() -> void:
	# Clear out old procedural nodes from previous tool runs to prevent duplication
	for child in get_children():
		child.queue_free()
		
	_build_scene_tree()
	update_prompt(key_text, action_text, object_text)
	_update_billboard_mode()

func _build_scene_tree() -> void:
	sub_viewport = SubViewport.new()
	sub_viewport.transparent_bg = true
	sub_viewport.size = Vector2i(200, 60)
	sub_viewport.render_target_update_mode = SubViewport.UPDATE_ALWAYS
	add_child(sub_viewport)

	sprite_3d = Sprite3D.new()
	sprite_3d.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	add_child(sprite_3d)

	var viewport_texture: ViewportTexture = sub_viewport.get_texture()
	sprite_3d.texture = viewport_texture

	# Renders it ontop of everything else in the game world
	var material: StandardMaterial3D = StandardMaterial3D.new()
	material.no_depth_test = true
	material.render_priority = 1
	material.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
	material.albedo_texture = viewport_texture
	
	sprite_3d.material_override = material

	# Main outer panel (Rounded Background)
	main_panel = PanelContainer.new()
	main_panel.custom_minimum_size = Vector2.ZERO
	main_panel.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	main_panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	
	var main_style: StyleBoxFlat = StyleBoxFlat.new()
	main_style.bg_color = background_color
	main_style.set_corner_radius_all(cornerRadius)
	main_panel.add_theme_stylebox_override("panel", main_style)
	sub_viewport.add_child(main_panel)

	var outer_margin: MarginContainer = MarginContainer.new()
	outer_margin.add_theme_constant_override("margin_left", int(outerXMargin))
	outer_margin.add_theme_constant_override("margin_right", int(outerXMargin))
	outer_margin.add_theme_constant_override("margin_top", int(outerYMargin))
	outer_margin.add_theme_constant_override("margin_bottom", int(outerYMargin))
	main_panel.add_child(outer_margin)

	# Row organizer
	var hbox: HBoxContainer = HBoxContainer.new()
	# Reduced separation for tighter gap between key and text
	hbox.add_theme_constant_override("separation", 6)
	outer_margin.add_child(hbox)

	# Key box background
	var key_panel: PanelContainer = PanelContainer.new()
	key_panel.custom_minimum_size = Vector2(36, 36)
	key_panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN # Stops the vertical stretching
	var key_style: StyleBoxFlat = StyleBoxFlat.new()
	key_style.bg_color = Color.WHITE
	key_style.set_corner_radius_all(4)
	key_panel.add_theme_stylebox_override("panel", key_style)
	hbox.add_child(key_panel)

	# Key inner padding
	var key_margin: MarginContainer = MarginContainer.new()
	key_margin.add_theme_constant_override("margin_left", 6)
	key_margin.add_theme_constant_override("margin_right", 6)
	key_panel.add_child(key_margin)

	# Key text label
	key_label = Label.new()
	key_label.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	key_label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	key_label.add_theme_color_override("font_color", Color.BLACK)
	key_margin.add_child(key_label)

	# Raw canvas control isolates text nodes from container constraints
	var text_canvas: Control = Control.new()
	text_canvas.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	hbox.add_child(text_canvas)

	# Action label (e.g. "Open")
	action_label = Label.new()
	action_label.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	action_label.add_theme_font_size_override("font_size", actionTextSize)
	text_canvas.add_child(action_label)

	# Object label (e.g. "Cabinet")
	object_label = Label.new()
	object_label.vertical_alignment = VERTICAL_ALIGNMENT_TOP
	object_label.add_theme_font_size_override("font_size", objectTextSize)
	object_label.add_theme_color_override("font_color", Color(0.7, 0.7, 0.7))
	text_canvas.add_child(object_label)

	main_panel.minimum_size_changed.connect(_on_ui_layout_changed)
	_apply_font()

## Applies font overrides to all text fields safely
func _apply_font() -> void:
	if key_label == null or action_label == null or object_label == null:
		return
		
	if prompt_font != null:
		key_label.add_theme_font_override("font", prompt_font)
		action_label.add_theme_font_override("font", prompt_font)
		object_label.add_theme_font_override("font", prompt_font)
	else:
		# If cleared, revert to default theme font
		key_label.remove_theme_font_override("font")
		action_label.remove_theme_font_override("font")
		object_label.remove_theme_font_override("font")

	# Force high-quality text rasterization settings
	key_label.text_overrun_behavior = TextServer.OVERRUN_NO_TRIMMING
	action_label.text_overrun_behavior = TextServer.OVERRUN_NO_TRIMMING
	object_label.text_overrun_behavior = TextServer.OVERRUN_NO_TRIMMING
		
	_on_ui_layout_changed()

## Call this via script to dynamically change text options on the fly
func update_prompt(new_key: String, new_action: String, new_object: String) -> void:
	if key_label == null or action_label == null or object_label == null:
		return
		
	key_label.text = new_key
	
	action_label.text = new_action
	action_label.visible = (new_action != "")
	
	object_label.text = new_object
	object_label.visible = (new_object != "")
	
	_on_ui_layout_changed()

## Sizing Engine (High-Density Vector Pass)
func _on_ui_layout_changed() -> void:
	if not is_inside_tree() or main_panel == null or sub_viewport == null:
		return
		
	# Internal asset scaling factor (e.g., 4.0 for 4x crispness)
	var hd_scale: float = 4.0

	action_label.add_theme_font_size_override("font_size", int(actionTextSize * hd_scale))
	object_label.add_theme_font_size_override("font_size", int(objectTextSize * hd_scale))
	key_label.add_theme_font_size_override("font_size", int(20 * hd_scale))

	# Strip hidden line height buffers
	action_label.add_theme_constant_override("line_spacing", 0)
	object_label.add_theme_constant_override("line_spacing", 0)
	
	# Update outer padding
	var outer_margin: MarginContainer = main_panel.get_child(0) as MarginContainer
	if outer_margin:
		outer_margin.add_theme_constant_override("margin_left", int(outerXMargin * hd_scale))
		outer_margin.add_theme_constant_override("margin_right", int(outerXMargin * hd_scale))
		outer_margin.add_theme_constant_override("margin_top", int(outerYMargin * hd_scale))
		outer_margin.add_theme_constant_override("margin_bottom", int(outerYMargin * hd_scale))
		
		var hbox: HBoxContainer = outer_margin.get_child(0) as HBoxContainer
		if hbox:
			hbox.add_theme_constant_override("separation", int(6 * hd_scale)) # Tighter horizontal spacing
			
			# Update key panel container sizing
			var key_panel: PanelContainer = hbox.get_child(0) as PanelContainer
			if key_panel:
				key_panel.custom_minimum_size = Vector2(36 * hd_scale, 36 * hd_scale)
				
				# Dynamic StyleBox scaling fix for the key background
				var key_style: StyleBoxFlat = key_panel.get_theme_stylebox("panel") as StyleBoxFlat
				if key_style:
					# Scale the original 4px radius by the resolution multiplier
					key_style.set_corner_radius_all(int(4 * hd_scale))
				
				var key_margin: MarginContainer = key_panel.get_child(0) as MarginContainer
				if key_margin:
					key_margin.add_theme_constant_override("margin_left", int(6 * hd_scale))
					key_margin.add_theme_constant_override("margin_right", int(6 * hd_scale))

			# Position control canvas relative to the parent bounding frame
			var text_canvas: Control = hbox.get_child(1) as Control
			if text_canvas:
				var key_size: float = 36.0 * hd_scale
				var action_h: float = action_label.get_combined_minimum_size().y
				
				# 1. Check if we are centering just one line
				if not object_label.visible or object_label.text == "":
					var center_y: float = (key_size - action_h) / 2.0
					# Add +1.0 scale to counter invisible bottom font padding
					action_label.position = Vector2(0, center_y + (1.0 * hd_scale))
					text_canvas.custom_minimum_size = Vector2(action_label.get_combined_minimum_size().x, key_size)
				
				# 2. Group both strings into a block and center them dynamically
				else:
					var obj_h: float = object_label.get_combined_minimum_size().y
					
					# Gap calculation based on the new constant
					var line_overlap: float = textVerticalSpacing * hd_scale 
					
					# Calculate total unified height of both lines
					var block_h: float = action_h + obj_h + line_overlap
					
					# Find perfect center of the key block relative to unified text block
					var start_y: float = (key_size - block_h) / 2.0
					
					# Slight visual bump to counter descender padding
					var global_visual_offset: float = -1.0 * hd_scale 
					
					action_label.position = Vector2(0, start_y + global_visual_offset)
					object_label.position = Vector2(0, start_y + action_h + line_overlap + global_visual_offset)
					
					var max_w: float = max(action_label.get_combined_minimum_size().x, object_label.get_combined_minimum_size().x)
					text_canvas.custom_minimum_size = Vector2(max_w, key_size)

	# Update background stylebox rounded corners
	var main_style: StyleBoxFlat = main_panel.get_theme_stylebox("panel") as StyleBoxFlat
	if main_style:
		main_style.set_corner_radius_all(int(cornerRadius * hd_scale))

	# Reevaluate layout boundaries with scaled dimensions
	main_panel.queue_sort()
	await RenderingServer.frame_pre_draw
	
	if is_inside_tree() and main_panel != null and sub_viewport != null:
		var calculated_size: Vector2 = main_panel.get_combined_minimum_size()
		
		var width: int = max(32, ceil(calculated_size.x))
		var height: int = max(32, ceil(calculated_size.y))
		
		# Match viewport precisely to the expanded layout bounds
		sub_viewport.size = Vector2i(width, height)
		
		# Compress the high-res layout back to standard 3D bounds
		sprite_3d.pixel_size = 0.01 / hd_scale

## Toggles between 3D billboard behavior and flat plane transformation
func _update_billboard_mode() -> void:
	if sprite_3d == null:
		return
		
	var mat: StandardMaterial3D = sprite_3d.material_override as StandardMaterial3D
	if mat:
		if omni_dir:
			mat.billboard_mode = BaseMaterial3D.BILLBOARD_ENABLED
		else:
			mat.billboard_mode = BaseMaterial3D.BILLBOARD_DISABLED