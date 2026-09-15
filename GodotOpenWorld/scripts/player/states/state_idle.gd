extends PlayerState
## 接地・入力なし。スタミナ回復。


func physics_update(delta: float) -> void:
	var p := player
	var c := p.config

	if _check_water():
		return
	if not p.is_on_floor():
		state_machine.transition_to(&"Fall")
		return

	p.last_safe_position = p.global_position
	p.stamina.regenerate(delta)
	p.move_horizontal(Vector3.ZERO, 0.0, c.ground_deceleration, delta)

	if p.consume_jump():
		state_machine.transition_to(&"Jump")
		return
	if p.move_dir != Vector3.ZERO:
		state_machine.transition_to(&"Move")
