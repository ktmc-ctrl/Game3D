class_name PlayerState
extends Node
## ステートの基底。enter / exit / physics_update を上書きする。
## 複数ステートで共通の遷移チェック (水・壁つかみ・よじ登り) はここに置く。

var player: Player
var state_machine: PlayerStateMachine


func enter(_previous: StringName) -> void:
	pass


func exit() -> void:
	pass


func physics_update(_delta: float) -> void:
	pass


## true を返すステートは自分で global_position を動かす (Mantle など)
func moves_body_directly() -> bool:
	return false


## デバッグ UI 向けの追加情報
func debug_info() -> String:
	return ""


# --- 共通の遷移チェック ---------------------------------------------------

## 足元が十分に水に浸かっていれば Swim へ
func _check_water() -> bool:
	if player.sensors.water_depth() >= player.config.swim_enter_depth:
		state_machine.transition_to(&"Swim")
		return true
	return false


## 進行方向に登れる壁があれば Climb へ (原神式の自動張り付き)
func _check_wall_grab() -> bool:
	if player.climb_cooldown > 0.0 or player.stamina.is_empty():
		return false
	if player.move_dir == Vector3.ZERO:
		return false
	var sensors := player.sensors
	if not sensors.climb_surface_hit():
		return false
	# 壁に向かって進んでいるときだけ
	if player.move_dir.dot(sensors.climb_surface_normal()) > -0.5:
		return false
	state_machine.transition_to(&"Climb")
	return true


## 目の前に腰〜胸の高さの段差があり、その上が歩けるなら Mantle へ
func _check_mantle() -> bool:
	var sensors := player.sensors
	if sensors.wall_hit():
		return false  # 胸より高い壁は Climb の担当
	if not sensors.low_wall_hit() or not sensors.ledge_hit():
		return false
	if player.move_dir == Vector3.ZERO:
		return false
	if player.move_dir.dot(sensors.low_wall_normal()) > -0.5:
		return false
	player.mantle_target = sensors.ledge_point()
	state_machine.transition_to(&"Mantle")
	return true


## 着地時の遷移先
func _land() -> void:
	if player.move_dir != Vector3.ZERO:
		state_machine.transition_to(&"Move")
	else:
		state_machine.transition_to(&"Idle")
