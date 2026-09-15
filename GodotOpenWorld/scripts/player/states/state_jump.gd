extends PlayerState
## ジャンプ初速を与えて上昇中。頂点で Fall へ。


func enter(_previous: StringName) -> void:
	player.velocity.y = player.config.jump_velocity


func physics_update(delta: float) -> void:
	var p := player
	var c := p.config

	if _check_water():
		return

	p.apply_gravity(delta)
	p.air_move(delta)
	p.face_direction(p.move_dir, delta)

	if p.velocity.y <= 0.0:
		state_machine.transition_to(&"Fall")
		return

	if p.consume_jump():
		if not p.stamina.is_empty() and p.sensors.ground_distance() >= c.glide_min_height:
			state_machine.transition_to(&"Glide")
			return

	if _check_wall_grab():
		return
