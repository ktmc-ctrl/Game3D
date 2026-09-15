class_name CameraRig
extends Node3D
## 三人称カメラ。Yaw → Pitch → SpringArm3D → Camera3D。
## target を追従するだけで target の子にはしない。

## 追従対象
@export var target: Node3D
## 注視点のオフセット (胸の高さ)
@export var target_offset := Vector3(0.0, 1.4, 0.0)
## 追従の速さ。0 で即時
@export var follow_speed: float = 20.0
## マウス感度 (ピクセルあたりの度)
@export var mouse_sensitivity: float = 0.2
## 見下ろし限界 (度、負)
@export var pitch_min_deg: float = -75.0
## 見上げ限界 (度)
@export var pitch_max_deg: float = 60.0
## アームの長さ
@export var arm_length: float = 4.5
## 起動時にマウスをキャプチャする
@export var capture_mouse_on_start: bool = true

@onready var yaw: Node3D = $Yaw
@onready var pitch: Node3D = $Yaw/Pitch
@onready var spring_arm: SpringArm3D = $Yaw/Pitch/SpringArm3D
@onready var camera: Camera3D = $Yaw/Pitch/SpringArm3D/Camera3D


func _ready() -> void:
	spring_arm.spring_length = arm_length
	if target is CollisionObject3D:
		spring_arm.add_excluded_object(target.get_rid())
	if target:
		global_position = target.global_position + target_offset
	if capture_mouse_on_start:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED


func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		yaw.rotation.y -= deg_to_rad(event.relative.x * mouse_sensitivity)
		var new_pitch := pitch.rotation.x - deg_to_rad(event.relative.y * mouse_sensitivity)
		pitch.rotation.x = clampf(new_pitch, deg_to_rad(pitch_min_deg), deg_to_rad(pitch_max_deg))
	elif event.is_action_pressed("ui_cancel"):
		if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
		else:
			Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	elif event is InputEventMouseButton and event.pressed and Input.mouse_mode == Input.MOUSE_MODE_VISIBLE:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED


func _physics_process(delta: float) -> void:
	if target == null:
		return
	var goal := target.global_position + target_offset
	if follow_speed <= 0.0:
		global_position = goal
	else:
		global_position = global_position.lerp(goal, minf(follow_speed * delta, 1.0))


## 移動入力の基準にする、Yaw だけを含む基底
func get_yaw_basis() -> Basis:
	return Basis(Vector3.UP, yaw.rotation.y)
