extends Node
## ヘッドレスで main.tscn を動かし、入力を流し込んで各ステートに入れるか確認する。
##   godot --headless --fixed-fps 60 --path GodotOpenWorld res://scenes/test/smoke_test.tscn
## 失敗があれば終了コード 1。

const MAIN_SCENE := preload("res://scenes/main.tscn")

var _player: Player
var _failures: PackedStringArray = []
var _passes := 0


func _ready() -> void:
	var main := MAIN_SCENE.instantiate()
	add_child(main)
	_player = main.get_node("Player")
	_run()


func _run() -> void:
	await _wait(60)
	_expect(_state() == &"Idle", "starts in Idle (got %s)" % _state())

	# 歩き
	Input.action_press("move_forward")
	await _wait(30)
	_expect(_state() == &"Move", "forward input -> Move (got %s)" % _state())
	_expect(_player.horizontal_speed() > 3.0, "walk speed > 3 (got %.2f)" % _player.horizontal_speed())
	Input.action_press("sprint")
	await _wait(30)
	_expect(_player.horizontal_speed() > 6.0, "sprint speed > 6 (got %.2f)" % _player.horizontal_speed())
	Input.action_release("sprint")
	Input.action_release("move_forward")
	await _wait(30)
	_expect(_state() == &"Idle", "release -> Idle (got %s)" % _state())

	# ジャンプ
	var y0 := _player.global_position.y
	Input.action_press("jump")
	await _wait(2)
	Input.action_release("jump")
	_expect(_state() == &"Jump", "jump input -> Jump (got %s)" % _state())
	var apex := y0
	for i in 60:
		await _wait(1)
		apex = maxf(apex, _player.global_position.y)
	_expect(apex - y0 > 1.0, "jump apex > 1m (got %.2f)" % (apex - y0))
	_expect(_state() == &"Idle", "landed -> Idle (got %s)" % _state())

	# 登攀: 塔 (0, *, -30) の正面 z=-28 に向かって前進
	_teleport(Vector3(0.0, 0.1, -26.0))
	await _wait(30)
	var stamina_before := _player.stamina.current
	Input.action_press("move_forward")
	var grabbed := await _wait_until(func() -> bool: return _state() == &"Climb", 180)
	_expect(grabbed, "walking into tower -> Climb (got %s)" % _state())
	var mantled := await _wait_until(func() -> bool: return _state() == &"Mantle", 900)
	_expect(mantled, "climbing up reaches Mantle (got %s, y=%.2f)" % [_state(), _player.global_position.y])
	Input.action_release("move_forward")
	var on_top := await _wait_until(func() -> bool: return _state() == &"Idle", 120)
	_expect(on_top and _player.global_position.y > 14.5, "after mantle on tower top (state %s, y=%.2f)" % [_state(), _player.global_position.y])
	_expect(_player.stamina.current < stamina_before, "climb drained stamina (%.1f -> %.1f)" % [stamina_before, _player.stamina.current])

	# 壁ジャンプ: 崖 (-20, *, -20) の正面 z=-15 から登って、途中でジャンプ
	_teleport(Vector3(-20.0, 0.1, -13.0))
	await _wait(30)
	Input.action_press("move_forward")
	grabbed = await _wait_until(func() -> bool: return _state() == &"Climb", 180)
	_expect(grabbed, "walking into cliff -> Climb (got %s)" % _state())
	Input.action_release("move_forward")
	await _wait(30)
	Input.action_press("jump")
	await _wait(2)
	Input.action_release("jump")
	_expect(_state() == &"Fall", "jump while climbing -> Fall (got %s)" % _state())
	await _wait(120)

	# 低い段差 (0.5m): Step (12, *, -4) の正面 z=-3.5
	_teleport(Vector3(12.0, 0.1, -1.0))
	await _wait(30)
	Input.action_press("move_forward")
	var stepped := await _wait_until(func() -> bool: return _state() == &"Mantle", 180)
	_expect(stepped, "walking into 0.5m step -> Mantle (got %s)" % _state())
	Input.action_release("move_forward")
	await _wait_until(func() -> bool: return _state() == &"Idle", 120)
	_expect(_state() == &"Idle" and _player.global_position.y > 0.45, "after step on top (state %s, y=%.2f)" % [_state(), _player.global_position.y])

	# 段差 (1.2m) のよじ登り: LowWall (12, *, -10) の正面 z=-9.5
	_teleport(Vector3(12.0, 0.1, -7.0))
	await _wait(30)
	Input.action_press("move_forward")
	var vaulted := await _wait_until(func() -> bool: return _state() == &"Mantle", 180)
	_expect(vaulted, "walking into low wall -> Mantle (got %s)" % _state())
	Input.action_release("move_forward")
	await _wait_until(func() -> bool: return _state() == &"Idle", 120)
	_expect(_state() == &"Idle" and _player.global_position.y > 1.1, "after vault on low wall top (state %s, y=%.2f)" % [_state(), _player.global_position.y])

	# 3m の壁: 登攀してから縁でよじ登り
	_teleport(Vector3(12.0, 0.1, -15.0))
	await _wait(30)
	Input.action_press("move_forward")
	grabbed = await _wait_until(func() -> bool: return _state() == &"Climb", 180)
	_expect(grabbed, "walking into mid wall -> Climb (got %s)" % _state())
	mantled = await _wait_until(func() -> bool: return _state() == &"Mantle", 300)
	Input.action_release("move_forward")
	await _wait_until(func() -> bool: return _state() == &"Idle", 120)
	_expect(mantled and _player.global_position.y > 2.9, "mid wall climb -> mantle -> top (state %s, y=%.2f)" % [_state(), _player.global_position.y])

	# no_climb レイヤーの壁には張り付かない
	_teleport(Vector3(22.0, 0.1, -7.0))
	await _wait(30)
	Input.action_press("move_forward")
	var climbed_forbidden := await _wait_until(func() -> bool: return _state() == &"Climb" or _state() == &"Mantle", 120)
	_expect(not climbed_forbidden, "no_climb wall never grabbed (got %s)" % _state())
	_expect(_player.global_position.z > -9.6, "blocked by no_climb wall (z=%.2f)" % _player.global_position.z)
	Input.action_release("move_forward")
	await _wait(30)

	# 50 度の坂は歩けないが登れる
	_teleport(Vector3(-42.0, 0.1, -4.0))
	await _wait(30)
	Input.action_press("move_forward")
	grabbed = await _wait_until(func() -> bool: return _state() == &"Climb", 240)
	_expect(grabbed, "steep slope -> Climb (got %s)" % _state())
	Input.action_release("move_forward")
	await _wait(30)

	# 滑空
	_teleport(Vector3(20.0, 40.0, 20.0))
	await _wait(10)
	_expect(_state() == &"Fall", "teleport in air -> Fall (got %s)" % _state())
	Input.action_press("jump")
	await _wait(2)
	Input.action_release("jump")
	_expect(_state() == &"Glide", "jump in air -> Glide (got %s)" % _state())
	await _wait(30)
	_expect(absf(_player.velocity.y + _player.config.glide_fall_speed) < 0.05, "glide fall speed clamped (vy=%.2f)" % _player.velocity.y)
	Input.action_press("move_forward")
	await _wait(60)
	_expect(_player.horizontal_speed() > 4.0, "glide forward speed (got %.2f)" % _player.horizontal_speed())
	Input.action_release("move_forward")
	Input.action_press("jump")
	await _wait(2)
	Input.action_release("jump")
	_expect(_state() == &"Fall", "jump while gliding -> Fall (got %s)" % _state())

	# 泳ぎ: 池 (30, -0.5, 30)
	_teleport(Vector3(30.0, 0.5, 30.0))
	var swimming := await _wait_until(func() -> bool: return _state() == &"Swim", 120)
	_expect(swimming, "dropping into pond -> Swim (got %s)" % _state())
	await _wait(120)
	var float_y := -0.5 - _player.config.swim_float_depth
	_expect(absf(_player.global_position.y - float_y) < 0.3, "floats near surface (y=%.2f, want %.2f)" % [_player.global_position.y, float_y])
	Input.action_press("move_forward")
	await _wait(60)
	_expect(_player.horizontal_speed() > 2.0, "swim speed (got %.2f)" % _player.horizontal_speed())
	Input.action_release("move_forward")
	var safe := _player.last_safe_position
	_player.stamina.drain(1000.0)
	await _wait(30)
	_expect(_player.global_position.y < float_y - 0.3, "no stamina -> sinking (y=%.2f)" % _player.global_position.y)
	var respawned := await _wait_until(func() -> bool: return _state() == &"Idle", 240)
	_expect(respawned and _player.global_position.distance_to(safe) < 0.5, "drown -> respawn at last safe position (state %s, dist %.2f)" % [_state(), _player.global_position.distance_to(safe)])

	# スタミナ回復
	await _wait(180)
	_expect(_player.stamina.current > _player.config.stamina_max * 0.9, "stamina regenerates on ground (%.1f)" % _player.stamina.current)

	print("--- smoke test: %d passed, %d failed ---" % [_passes, _failures.size()])
	for f in _failures:
		print("FAIL: " + f)
	get_tree().quit(1 if _failures.size() > 0 else 0)


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


func _expect(cond: bool, label: String) -> void:
	if cond:
		_passes += 1
		print("ok   " + label)
	else:
		_failures.append(label)
		print("FAIL " + label)
