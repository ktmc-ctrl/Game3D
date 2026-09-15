extends PlayerState
## 滑空。落下速度をクランプし、カメラ基準で前進。スタミナ消費。


func enter(_previous: StringName) -> void:
	# 展開した瞬間に落下を殺す
	player.velocity.y = maxf(player.velocity.y, -player.config.glide_fall_speed)


func physics_update(delta: float) -> void:
	var p := player
	var c := p.config

	if _check_water():
		return
	if p.is_on_floor():
		_land()
		return
	if p.stamina.is_empty() or p.consume_jump():
		state_machine.transition_to(&"Fall")
		return

	p.stamina.drain_per_second(c.glide_stamina_per_second, delta)

	p.velocity.y = maxf(p.velocity.y - c.gravity * delta, -c.glide_fall_speed)
	if p.move_dir != Vector3.ZERO:
		p.move_horizontal(p.move_dir, c.glide_forward_speed, c.glide_acceleration, delta)
		p.face_direction(p.move_dir, delta)
	else:
		p.move_horizontal(Vector3.ZERO, 0.0, c.glide_deceleration, delta)
		p.face_direction(p.horizontal_velocity(), delta)

	if _check_wall_grab():
		return
