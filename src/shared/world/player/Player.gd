extends CharacterBody3D
class_name Player

@export var player := 1:
	set(id):
		player = id

func getCharacter() -> CharacterBody3D:
	return self
