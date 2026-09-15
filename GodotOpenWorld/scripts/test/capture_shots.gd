extends Node
## main.tscn を動かして各ステートの場面を PNG に保存する (ドキュメント用)。
##   godot --rendering-driver opengl3 --resolution 1280x720 --fixed-fps 60 --path GodotOpenWorld res://scenes/test/capture_shots.tscn -- --out=/path/to/dir

const MAIN_SCENE := preload("res://scenes/main.tscn")

var _player: Player
var _rig: CameraRig
var _out_dir := "user://shots"


func _ready() -> void:
	for arg in OS.get_cmdline_user_args():
		if arg.begins_with("--out="):
			_out_dir = arg.trim_prefix("--out=")
	DirAccess.make_dir_recursive_absolute(_out_dir)
	var main := MAIN_SCENE.instantiate()
	add_child(main)
	_player = main.get_node("Player")
	_rig = main.get_node("CameraRig")
	_rig.follow_speed = 0.0
	_run()


func _run() -> void:
	# 1. スポーン直後の全景 (塔を正面に)
	await _wait(30)
	_look(0.0, -20.0)
	await _wait(5)
	await _shot("01_overview")

	# 2. 走り
	Input.action_press("move_forward")
	Input.action_press("sprint")
	await _wait(45)
	await _shot("02_run")
	Input.action_release("sprint")
	Input.action_release("move_forward")

	# 3. 登攀 (塔の中腹)
	_teleport(Vector3(0.0, 0.1, -26.0))
	await _wait(20)
	Input.action_press("move_forward")
	await _wait_until(func() -> bool: return _state() == &"Climb", 180)
	await _wait(150)
	_look(35.0, -10.0)
	await _wait(5)
	await _shot("03_climb")
	await _wait_until(func() -> bool: return _state() == &"Idle", 900)
	Input.action_release("move_forward")
	await _wait(10)

	# 4. 塔の頂上
	_look(180.0, -25.0)
	await _wait(5)
	await _shot("04_tower_top")

	# 5. 滑空 (頂上から飛び出す)
	_look(0.0, -15.0)
	Input.action_press("move_forward")
	await _wait_until(func() -> bool: return _state() == &"Fall", 120)
	await _wait(10)
	Input.action_press("jump")
	await _wait(2)
	Input.action_release("jump")
	await _wait(60)
	_look(-40.0, -20.0)
	await _wait(5)
	await _shot("05_glide")
	Input.action_release("move_forward")
	Input.action_press("jump")
	await _wait(2)
	Input.action_release("jump")
	await _wait(60)

	# 6. 泳ぎ
	_teleport(Vector3(30.0, 0.5, 30.0))
	await _wait_until(func() -> bool: return _state() == &"Swim", 120)
	await _wait(60)
	Input.action_press("move_forward")
	await _wait(40)
	_look(30.0, -25.0)
	await _wait(5)
	await _shot("06_swim")
	Input.action_release("move_forward")

	# 7. 段差と壁 (右手前エリア)
	_teleport(Vector3(12.0, 0.1, 4.0))
	await _wait(30)
	_look(0.0, -25.0)
	await _wait(5)
	await _shot("07_walls")

	# 8. 坂 3 枚
	_teleport(Vector3(-42.0, 0.1, 8.0))
	await _wait(30)
	_look(0.0, -15.0)
	await _wait(5)
	await _shot("08_slopes")

	print("captured to " + _out_dir)
	get_tree().quit(0)


func _look(yaw_deg: float, pitch_deg: float) -> void:
	_rig.yaw.rotation.y = deg_to_rad(yaw_deg)
	_rig.pitch.rotation.x = deg_to_rad(pitch_deg)


func _shot(name: String) -> void:
	await RenderingServer.frame_post_draw
	var img := get_viewport().get_texture().get_image()
	var path := _out_dir.path_join(name + ".png")
	var err := img.save_png(path)
	print("shot %s (%s) -> %s" % [name, _state(), error_string(err)])


func _state() -> StringName:
	return _player.current_state_name()


func _teleport(pos: Vector3) -> void:
	_player.global_position = pos
	_player.velocity = Vector3.ZERO
	_player.state_machine.transition_to(&"Fall")


func _wait(frames: int) -> void:
	for i in frames:
		await get_tree().physics_frame


func _wait_until(pred: Callable, max_frames: int) -> bool:
	for i in max_frames:
		if pred.call():
			return true
		await get_tree().physics_frame
	return pred.call()
