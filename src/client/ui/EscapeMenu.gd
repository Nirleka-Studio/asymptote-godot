extends Node

func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel"): # "ui_cancel" is usually mapped to Escape
		if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
			# well keep this for now
			# $CanvasLayer/PauseMenu.show()
			# get_tree().paused = true
		else:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
			# $CanvasLayer/PauseMenu.hide()
			# get_tree().paused = false
