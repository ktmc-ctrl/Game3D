extends PlayerState
## 接地・歩き/走り。Sprint 押下で走り。走りのスタミナ消費は既定 0。

var _running := false


func physics_update(delta: float) -> void:
	var p := player
	var c := p.config

	if _check_water():
		return
	if not p.is_on_floor():
		state_machine.transition_to(&"Fall")
		return

	p.last_safe_position = p.global_position

	if p.move_dir == Vector3.ZERO:
		state_machine.transition_to(&"Idle")
		return

	_running = p.sprint_held and (c.sprint_stamina_per_second <= 0.0 or not p.stamina.is_empty())
	var speed := c.run_speed if _running else c.walk_speed
	if _running and c.sprint_stamina_per_second > 0.0:
		p.stamina.drain_per_second(c.sprint_stamina_per_second, delta)
	else:
		p.stamina.regenerate(delta)

	p.move_horizontal(p.move_dir, speed, c.ground_acceleration, delta)
	p.face_direction(p.move_dir, delta)

	if p.consume_jump():
		state_machine.transition_to(&"Jump")
		return
	# 低い段差は登攀より先に判定する (膝の Ray だけが当たる状況が両方にあるため)
	if _check_mantle():
		return
	if _check_wall_grab():
		return


func debug_info() -> String:
	return "run" if _running else "walk"
