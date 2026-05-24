extends Node3D

func _ready() -> void:
	if not multiplayer.is_server():
		return
		
	multiplayer.peer_connected.connect(add_player)
	multiplayer.peer_disconnected.connect(remove_player)
	

	for id in multiplayer.get_peers():
		add_player(id)

	if not OS.has_feature("dedicated_server"):
		add_player(1)

func add_player(id: int):
	print("adding player:" + str(id))
	
	var character = preload("res://scenes/player.tscn").instantiate()
	character.player = id
	character.name = str(id)
	
	$Players.add_child(character, true)
	
func remove_player(id: int):
	print("removing player:" + str(id))
	if not $Players.has_node(str(id)):
		return
	
	$Players.get_node(str(id)).queue_free()

func _exit_tree() -> void:
	if not multiplayer.is_server():
		return
		
	multiplayer.peer_connected.disconnect(add_player)
	multiplayer.peer_disconnected.disconnect(remove_player)
