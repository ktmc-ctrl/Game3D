extends CanvasLayer
## デバッグ表示。スタミナバー、現在ステート、センサー状態。F3 (debug_toggle) で表示切替。

@export var player: Player

@onready var state_label: Label = %StateLabel
@onready var stamina_bar: ProgressBar = %StaminaBar
@onready var stamina_label: Label = %StaminaLabel
@onready var info_label: Label = %InfoLabel


func _ready() -> void:
	if player == null:
		return
	player.stamina.changed.connect(_on_stamina_changed)
	player.state_changed.connect(_on_state_changed)
	_on_stamina_changed(player.stamina.current, player.stamina.maximum)
	_on_state_changed(&"", player.current_state_name())


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("debug_toggle"):
		visible = not visible


func _process(_delta: float) -> void:
	if player == null or not visible:
		return
	var s := player.sensors
	var state := player.state_machine.current_state
	var extra := state.debug_info() if state else ""
	state_label.text = "State: %s%s" % [player.current_state_name(), ("  (" + extra + ")") if extra != "" else ""]
	var ground := s.ground_distance()
	info_label.text = "\n".join([
		"speed: %.2f m/s  vy: %.2f" % [player.horizontal_speed(), player.velocity.y],
		"floor: %s  wall: %s  low: %s  ledge: %s" % [player.is_on_floor(), s.wall_hit(), s.low_wall_hit(), s.ledge_hit()],
		"ground: %s  water depth: %s" % [("%.2f" % ground) if is_finite(ground) else "-", ("%.2f" % s.water_depth()) if s.in_water() else "-"],
		"pos: (%.1f, %.1f, %.1f)" % [player.global_position.x, player.global_position.y, player.global_position.z],
	])


func _on_stamina_changed(current: float, maximum: float) -> void:
	stamina_bar.max_value = maximum
	stamina_bar.value = current
	stamina_label.text = "Stamina %d / %d" % [roundi(current), roundi(maximum)]


func _on_state_changed(_previous: StringName, current: StringName) -> void:
	state_label.text = "State: %s" % current
