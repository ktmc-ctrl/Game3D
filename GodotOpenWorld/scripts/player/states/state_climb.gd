extends PlayerState
## 壁に張り付き、壁面の接線方向に上下左右移動。
## 頂上の縁を検知したら Mantle、スタミナ 0 か壁消失で Fall、壁ジャンプで Fall。

var _normal := Vector3.FORWARD
var _moving := false


func enter(_previous: StringName) -> void:
	var p := player
	p.velocity = Vector3.ZERO
	_normal = p.sensors.climb_surface_normal()
	p.face_direction(-_normal, 0.0, true)


func physics_update(delta: float) -> void:
	var p := player
	var c := p.config
	var s := p.sensors

	if p.stamina.is_empty():
		_detach()
		return
	if _check_water():
		return

	# 壁の追跡。胸の Ray が外れたら縁を探す
	if s.wall_hit():
		_normal = s.wall_normal()
	elif s.ledge_hit():
		p.mantle_target = s.ledge_point()
		state_machine.transition_to(&"Mantle")
		return
	elif s.low_wall_hit():
		_normal = s.low_wall_normal()
	else:
		_detach()
		return

	p.face_direction(-_normal, delta, true)

	# 壁ジャンプ (壁から離れる方向へ)
	if p.consume_jump():
		if p.stamina.has_at_least(c.climb_jump_stamina_cost):
			p.stamina.drain(c.climb_jump_stamina_cost)
			p.velocity = _normal * c.wall_jump_away_velocity + Vector3.UP * c.wall_jump_up_velocity
			p.climb_cooldown = c.climb_regrab_cooldown
			state_machine.transition_to(&"Fall")
		else:
			_detach()
		return

	# 壁面の接線基底。入力は生の 2D (x: 右, y: 手前) をそのまま使う
	var wall_up := (Vector3.UP - _normal * _normal.dot(Vector3.UP)).normalized()
	var wall_right := wall_up.cross(_normal).normalized()
	var move := wall_up * -p.input_dir.y + wall_right * p.input_dir.x
	if move.length_squared() > 1.0:
		move = move.normalized()
	_moving = move.length_squared() > 0.0001

	p.velocity = move * c.climb_speed - _normal * c.climb_stick_speed
	var rate := c.climb_stamina_per_second if _moving else c.climb_idle_stamina_per_second
	p.stamina.drain_per_second(rate, delta)

	# 下に降りて床に着いたら歩きに戻る
	if p.is_on_floor() and p.input_dir.y > 0.0:
		state_machine.transition_to(&"Idle")


func _detach() -> void:
	player.climb_cooldown = player.config.climb_regrab_cooldown
	player.velocity = Vector3.ZERO
	state_machine.transition_to(&"Fall")


func debug_info() -> String:
	return "moving" if _moving else "hanging"
