extends Node
const PORT = 8080

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.
	
func _on_host_pressed():
	print("host pressed")
	var peer = ENetMultiplayerPeer.new()
	peer.create_server(PORT)
	multiplayer.multiplayer_peer = peer
	start_game()

func start_game():
	$UI.hide()
	if multiplayer.is_server():
		# The MultiplayerSpawner will now handle the replication
		# simply by adding this to the tree
		var level = load("res://scenes/level.tscn").instantiate()
		add_child(level)

func change_level(scene: PackedScene):
	var level = $Level
	for c in level.get_children():
		level.remove_child(c)
		c.queue_free()
		level.add_child(scene.instantiate())

func _on_client_pressed():
	print("client pressed")
	var ip = "127.0.0.1"
	var peer = ENetMultiplayerPeer.new()
	peer.create_client(ip, PORT)
	multiplayer.multiplayer_peer = peer
	start_game()
