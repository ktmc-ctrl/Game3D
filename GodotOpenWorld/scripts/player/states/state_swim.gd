extends PlayerState
## 泳ぎ。水面に浮遊してスタミナ消費。0 で浮力を失って沈み、一定時間後に最後の接地位置へ戻る。

var _sinking := false
var _sink_timer := 0.0
var _moving := false


func enter(_previous: StringName) -> void:
	player.velocity.y = 0.0
	_sinking = false
	_sink_timer = 0.0


func physics_update(delta: float) -> void:
	var p := player
	var c := p.config
	var s := p.sensors

	if not s.in_water():
		state_machine.transition_to(&"Fall")
		return

	var depth := s.water_depth()
	if not _sinking and p.is_on_floor() and depth < c.swim_enter_depth - c.swim_exit_hysteresis:
		_land()
		return

	if p.stamina.is_empty():
		_sinking = true

	if _sinking:
		_sink_timer += delta
		p.velocity = Vector3(0.0, -c.sink_speed, 0.0)
		if _sink_timer >= c.drown_respawn_delay:
			p.respawn_at_safe_position()
			p.stamina.refill(c.drown_stamina_refill_ratio)
			state_machine.transition_to(&"Idle")
		return

	# 浮力: 目標高さとの差をそのまま上下速度にする
	var target_y := s.water_surface_y() - c.swim_float_depth
	p.velocity.y = clampf((target_y - p.global_position.y) * c.swim_buoyancy, -c.swim_speed, c.swim_speed)

	_moving = p.move_dir != Vector3.ZERO
	p.move_horizontal(p.move_dir, c.swim_speed, c.swim_acceleration, delta)
	p.face_direction(p.move_dir, delta)
	var rate := c.swim_stamina_per_second if _moving else c.swim_idle_stamina_per_second
	p.stamina.drain_per_second(rate, delta)

	# 岸の壁に向かって泳げば登れる
	if _check_wall_grab():
		return


func debug_info() -> String:
	if _sinking:
		return "sinking %.1fs" % _sink_timer
	return "swimming" if _moving else "floating"
