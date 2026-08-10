class_name Crosshair
extends CanvasLayer

@export var line_length: float = 10.0
@export var line_width: float = 2.0
@export var center_gap: float = 0.0

func _ready() -> void:
	layer = 100

	var back_buffer := BackBufferCopy.new()
	back_buffer.copy_mode = BackBufferCopy.COPY_MODE_VIEWPORT
	add_child(back_buffer)

	var root := Control.new()
	root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	add_child(root)

	var center := Control.new()
	center.set_anchors_preset(Control.PRESET_CENTER)
	center.mouse_filter = Control.MOUSE_FILTER_IGNORE
	root.add_child(center)

	var shader := Shader.new()
	shader.code = """
		shader_type canvas_item;
		uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

		void fragment() {
			vec4 bg = texture(screen_texture, SCREEN_UV);
			COLOR = vec4(1.0 - bg.rgb, 1.0);
		}
	"""
	var shader_material := ShaderMaterial.new()
	shader_material.shader = shader

	var rects: Array[Rect2] = [
		Rect2(-line_width / 2.0, -center_gap - line_length, line_width, line_length),
		Rect2(-line_width / 2.0, center_gap, line_width, line_length),
		Rect2(-center_gap - line_length, -line_width / 2.0, line_length, line_width),
		Rect2(center_gap, -line_width / 2.0, line_length, line_width)
	]

	for r: Rect2 in rects:
		var rect_node := ColorRect.new()
		rect_node.position = r.position
		rect_node.size = r.size
		rect_node.material = shader_material
		rect_node.mouse_filter = Control.MOUSE_FILTER_IGNORE
		center.add_child(rect_node)
