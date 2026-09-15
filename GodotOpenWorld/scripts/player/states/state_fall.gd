extends PlayerState
## 空中で落下中。コヨーテタイム、滑空展開、壁つかみ、着地を扱う。

var _coyote_left := 0.0


func enter(previous: StringName) -> void:
	# 足場から歩いて落ちた場合だけ猶予ジャンプを許す
	_coyote_left = player.config.coyote_time if previous in [&"Idle", &"Move"] else 0.0


func physics_update(delta: float) -> void:
	var p := player
	var c := p.config

	if _check_water():
		return
	if p.is_on_floor():
		_land()
		return

	_coyote_left -= delta
	p.apply_gravity(delta)
	p.air_move(delta)
	p.face_direction(p.move_dir, delta)

	if p.consume_jump():
		if _coyote_left > 0.0:
			state_machine.transition_to(&"Jump")
			return
		if not p.stamina.is_empty() and p.sensors.ground_distance() >= c.glide_min_height:
			state_machine.transition_to(&"Glide")
			return

	if _check_wall_grab():
		return
