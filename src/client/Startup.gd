extends Node
const PORT = 8080

func _ready() -> void:
	return
	
func _on_host_pressed() -> void:
	print("host pressed")
	var peer: ENetMultiplayerPeer = ENetMultiplayerPeer.new()
	peer.create_server(PORT)
	multiplayer.multiplayer_peer = peer
	start_game()
	return

func start_game() -> void:
	($UI as Control).hide()
	if multiplayer.is_server():
		# The MultiplayerSpawner will now handle the replication
		# simply by adding this to the tree
		var level: Node3D = (load("res://scenes/level.tscn") as PackedScene).instantiate()
		add_child(level)
		
	return

func change_level(scene: PackedScene) -> void:
	var level: Node3D = $Level
	for c in level.get_children():
		level.remove_child(c)
		c.queue_free()
		level.add_child(scene.instantiate())
		
	return

func _on_client_pressed() -> void:
	print("client pressed")
	var ip: String = "127.0.0.1"
	var peer: ENetMultiplayerPeer = ENetMultiplayerPeer.new()
	peer.create_client(ip, PORT)
	multiplayer.multiplayer_peer = peer
	start_game()

	return
