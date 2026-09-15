extends PlayerState
## よじ登り。player.mantle_target (縁の上の点) へ、上→前の順に補間で移動する。
## 物理は使わず位置を直接動かすので moves_body_directly() = true。

var _start := Vector3.ZERO
var _mid := Vector3.ZERO
var _end := Vector3.ZERO
var _t := 0.0
var _duration := 0.3


func enter(_previous: StringName) -> void:
	var p := player
	p.velocity = Vector3.ZERO
	_t = 0.0
	_start = p.global_position
	var forward := -p.global_transform.basis.z
	forward.y = 0.0
	forward = forward.normalized()
	_end = p.mantle_target + forward * p.config.mantle_forward_margin + Vector3.UP * 0.05
	_mid = Vector3(_start.x, _end.y, _start.z)
	# 低い段差は短く済ませる (1.5m を基準に 0.3〜1.0 倍)
	var height_ratio := clampf((_end.y - _start.y) / 1.5, 0.3, 1.0)
	_duration = maxf(p.config.mantle_duration * height_ratio, 0.01)


func physics_update(delta: float) -> void:
	var p := player
	_t += delta / _duration
	var t := clampf(_t, 0.0, 1.0)
	if t < 0.5:
		p.global_position = _start.lerp(_mid, t * 2.0)
	else:
		p.global_position = _mid.lerp(_end, (t - 0.5) * 2.0)
	p.velocity = Vector3.ZERO
	if _t >= 1.0:
		p.global_position = _end
		p.last_safe_position = _end
		state_machine.transition_to(&"Idle")


func moves_body_directly() -> bool:
	return true
