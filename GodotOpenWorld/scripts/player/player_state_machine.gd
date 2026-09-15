class_name PlayerStateMachine
extends Node
## 子ノードの PlayerState を名前で管理する明示的ステートマシン。
## 遷移は transition_to(&"Climb") のように名前で呼ぶ。

signal state_changed(previous: StringName, current: StringName)

@export var initial_state_name: StringName = &"Idle"

var player: Player
var current_state: PlayerState
var _states: Dictionary = {}


func setup(p: Player) -> void:
	player = p
	for child in get_children():
		if child is PlayerState:
			_states[child.name] = child
			child.player = p
			child.state_machine = self
	assert(_states.has(initial_state_name), "initial state '%s' not found" % initial_state_name)
	_change(_states[initial_state_name], &"")


func has_state(state_name: StringName) -> bool:
	return _states.has(state_name)


func get_state(state_name: StringName) -> PlayerState:
	return _states.get(state_name)


func current_state_name() -> StringName:
	return current_state.name if current_state else &""


func transition_to(state_name: StringName) -> void:
	assert(_states.has(state_name), "unknown state '%s'" % state_name)
	var next: PlayerState = _states[state_name]
	if next == current_state:
		return
	var previous_name := current_state_name()
	if current_state:
		current_state.exit()
	_change(next, previous_name)


func _change(next: PlayerState, previous_name: StringName) -> void:
	current_state = next
	current_state.enter(previous_name)
	state_changed.emit(previous_name, current_state.name)


func physics_update(delta: float) -> void:
	if current_state:
		current_state.physics_update(delta)


## 現在のステートが自分で位置を動かすなら true (Player は move_and_slide を呼ばない)
func moves_body_directly() -> bool:
	return current_state != null and current_state.moves_body_directly()
