class_name Player
extends CharacterBody3D
## プレイヤー本体。入力の集約と共通の物理ヘルパーだけを持ち、
## 移動ロジックは StateMachine 配下の各ステートが担う。
## 見た目は Visual 以下に隔離してあり、後でモデルを差し替えても本体は変えない。

signal state_changed(previous: StringName, current: StringName)

## 全数値パラメータ (data/player/movement_config.tres)
@export var config: MovementConfig
## 移動入力の基準にするカメラ。未設定ならワールド軸
@export var camera_rig: CameraRig

@onready var visual: Node3D = $Visual
@onready var sensors: PlayerSensors = $Sensors
@onready var stamina: StaminaComponent = $Stamina
@onready var state_machine: PlayerStateMachine = $StateMachine

## 生の移動入力 (x: 右, y: 手前)
var input_dir := Vector2.ZERO
## カメラ基準に変換した水平移動方向 (正規化済み、入力なしなら ZERO)
var move_dir := Vector3.ZERO
var sprint_held := false
var jump_held := false
## 最後に安全に接地していた位置 (溺れた時の復帰先)
var last_safe_position := Vector3.ZERO
## 壁から離れた直後の再張り付き禁止タイマー
var climb_cooldown := 0.0
## Mantle ステートの目標点 (縁の上)
var mantle_target := Vector3.ZERO

var _jump_buffer := 0.0


func _ready() -> void:
	if config == null:
		config = MovementConfig.new()
	floor_max_angle = deg_to_rad(config.floor_max_angle_deg)
	stamina.setup(config)
	sensors.setup(self, floor_max_angle)
	last_safe_position = global_position
	state_machine.state_changed.connect(_on_state_changed)
	state_machine.setup(self)


func _physics_process(delta: float) -> void:
	_read_input(delta)
	if climb_cooldown > 0.0:
		climb_cooldown -= delta
	sensors.refresh()
	state_machine.physics_update(delta)
	if not state_machine.moves_body_directly():
		move_and_slide()


func _read_input(delta: float) -> void:
	input_dir = Input.get_vector("move_left", "move_right", "move_forward", "move_back")
	sprint_held = Input.is_action_pressed("sprint")
	jump_held = Input.is_action_pressed("jump")
	if Input.is_action_just_pressed("jump"):
		_jump_buffer = config.jump_buffer_time
	elif _jump_buffer > 0.0:
		_jump_buffer -= delta

	var basis := camera_rig.get_yaw_basis() if camera_rig else Basis.IDENTITY
	var dir := basis.x * input_dir.x + basis.z * input_dir.y
	dir.y = 0.0
	move_dir = dir.normalized() if dir.length_squared() > 0.0001 else Vector3.ZERO


## 先行入力込みでジャンプ入力を消費する
func consume_jump() -> bool:
	if _jump_buffer > 0.0:
		_jump_buffer = 0.0
		return true
	return false


# --- ステートから使う共通ヘルパー -----------------------------------------

func apply_gravity(delta: float) -> void:
	velocity.y = maxf(velocity.y - config.gravity * delta, -config.max_fall_speed)


## 水平速度を dir * speed へ accel で近づける。dir が ZERO なら減速
func move_horizontal(dir: Vector3, speed: float, accel: float, delta: float) -> void:
	var target := dir * speed
	var h := horizontal_velocity().move_toward(target, accel * delta)
	velocity.x = h.x
	velocity.z = h.z


## 空中操作。入力があるときだけ効き、既に速ければその速さを保つ
func air_move(delta: float) -> void:
	if move_dir == Vector3.ZERO:
		return
	var speed := maxf(config.air_control_speed, horizontal_velocity().length())
	move_horizontal(move_dir, speed, config.air_acceleration, delta)


## 本体の向きを dir へ回す。instant なら即時
func face_direction(dir: Vector3, delta: float, instant: bool = false) -> void:
	dir.y = 0.0
	if dir.length_squared() < 0.0001:
		return
	var target_yaw := atan2(-dir.x, -dir.z)
	if instant or config.rotation_speed <= 0.0:
		rotation.y = target_yaw
	else:
		rotation.y = lerp_angle(rotation.y, target_yaw, minf(config.rotation_speed * delta, 1.0))


func horizontal_velocity() -> Vector3:
	return Vector3(velocity.x, 0.0, velocity.z)


func horizontal_speed() -> float:
	return horizontal_velocity().length()


func respawn_at_safe_position() -> void:
	global_position = last_safe_position
	velocity = Vector3.ZERO


func current_state_name() -> StringName:
	return state_machine.current_state_name()


func _on_state_changed(previous: StringName, current: StringName) -> void:
	state_changed.emit(previous, current)
