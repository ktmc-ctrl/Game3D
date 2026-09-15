class_name StaminaComponent
extends Node
## スタミナの保持と増減。数値は MovementConfig から受け取る。
## 誰が消費/回復させるかはステート側の責任で、ここは値の管理だけ。

signal changed(current: float, maximum: float)
signal depleted

var current: float = 100.0
var maximum: float = 100.0

var _regen_per_second: float = 25.0
var _regen_delay: float = 0.5
var _regen_delay_left: float = 0.0


func setup(config: MovementConfig) -> void:
	maximum = config.stamina_max
	current = maximum
	_regen_per_second = config.stamina_regen_per_second
	_regen_delay = config.stamina_regen_delay
	_regen_delay_left = 0.0
	changed.emit(current, maximum)


## 即時に amount だけ消費する。0 になった瞬間に depleted を出す。
func drain(amount: float) -> void:
	if amount <= 0.0:
		return
	var was_positive := current > 0.0
	current = maxf(current - amount, 0.0)
	_regen_delay_left = _regen_delay
	changed.emit(current, maximum)
	if was_positive and current <= 0.0:
		depleted.emit()


## 毎秒 rate の割合で消費する (physics_process 用)。
func drain_per_second(rate: float, delta: float) -> void:
	drain(rate * delta)


## 回復を進める。呼ぶかどうかはステートが決める (地上のみ、など)。
func regenerate(delta: float) -> void:
	if _regen_delay_left > 0.0:
		_regen_delay_left -= delta
		return
	if current >= maximum:
		return
	current = minf(current + _regen_per_second * delta, maximum)
	changed.emit(current, maximum)


func refill(ratio: float = 1.0) -> void:
	current = clampf(maximum * ratio, 0.0, maximum)
	_regen_delay_left = 0.0
	changed.emit(current, maximum)


func is_empty() -> bool:
	return current <= 0.0


func has_at_least(amount: float) -> bool:
	return current >= amount


func ratio() -> float:
	return current / maximum if maximum > 0.0 else 0.0
