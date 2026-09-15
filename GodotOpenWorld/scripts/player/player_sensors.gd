class_name PlayerSensors
extends Node3D
## 環境検知をまとめる。ステートは RayCast を直接触らず、ここの結果だけ読む。
## Player 本体 (CharacterBody3D) が向きを変えるので、子の Ray は自動で正面を向く。

@onready var wall_ray_high: RayCast3D = $WallRayHigh
@onready var wall_ray_low: RayCast3D = $WallRayLow
@onready var ledge_ray: RayCast3D = $LedgeRay
@onready var ground_ray: RayCast3D = $GroundRay
@onready var water_sensor: Area3D = $WaterSensor

## この値より法線の y が小さい面は「急 (歩けない)」= 登攀対象
var _walkable_cos: float = cos(deg_to_rad(45.0))
## 天井に近い面 (法線がこれより下を向く) は登攀対象にしない
var _overhang_limit: float = -0.3
var _water_volumes: Array[Area3D] = []


func setup(body: CollisionObject3D, floor_max_angle: float) -> void:
	_walkable_cos = cos(floor_max_angle)
	for ray in [wall_ray_high, wall_ray_low, ledge_ray, ground_ray]:
		ray.add_exception(body)
	water_sensor.area_entered.connect(_on_water_entered)
	water_sensor.area_exited.connect(_on_water_exited)


## 現在の位置・向きで Ray を更新する。physics_process の頭で一度呼ぶ。
func refresh() -> void:
	wall_ray_high.force_raycast_update()
	wall_ray_low.force_raycast_update()
	ledge_ray.force_raycast_update()
	ground_ray.force_raycast_update()


func is_steep(normal: Vector3) -> bool:
	return normal.y < _walkable_cos


func _is_climbable(ray: RayCast3D) -> bool:
	if not ray.is_colliding():
		return false
	var n := ray.get_collision_normal()
	return is_steep(n) and n.y > _overhang_limit


# --- 壁 -------------------------------------------------------------------

## 胸の高さの Ray が登れる壁に当たっているか
func wall_hit() -> bool:
	return _is_climbable(wall_ray_high)


func wall_normal() -> Vector3:
	return wall_ray_high.get_collision_normal()


func wall_point() -> Vector3:
	return wall_ray_high.get_collision_point()


## 膝の高さの Ray が登れる壁 (または低い段差) に当たっているか
func low_wall_hit() -> bool:
	return _is_climbable(wall_ray_low)


func low_wall_normal() -> Vector3:
	return wall_ray_low.get_collision_normal()


## 胸か膝のどちらかの Ray が登れる面に当たっているか。
## 手前に倒れた急斜面は胸の Ray が届かないので膝も見る
func climb_surface_hit() -> bool:
	return wall_hit() or low_wall_hit()


## 登攀に使う法線。胸の Ray を優先し、外れていれば膝の Ray
func climb_surface_normal() -> Vector3:
	if wall_hit():
		return wall_normal()
	return low_wall_normal()


# --- 縁 (よじ登り) --------------------------------------------------------

## 頭上から前方下向きの Ray が歩ける面に当たっているか
func ledge_hit() -> bool:
	if not ledge_ray.is_colliding():
		return false
	return not is_steep(ledge_ray.get_collision_normal())


func ledge_point() -> Vector3:
	return ledge_ray.get_collision_point()


# --- 地面 -----------------------------------------------------------------

## 足元から地面までの距離。何も無ければ INF
func ground_distance() -> float:
	if not ground_ray.is_colliding():
		return INF
	return ground_ray.global_position.distance_to(ground_ray.get_collision_point())


# --- 水 -------------------------------------------------------------------

func in_water() -> bool:
	return not _water_volumes.is_empty()


## 重なっている水域のうち一番高い水面の y
func water_surface_y() -> float:
	var best := -INF
	for volume in _water_volumes:
		if volume.has_method("get_surface_y"):
			best = maxf(best, volume.get_surface_y())
	return best


## 足元 (Player 原点) がどれだけ水面より下にあるか。水に入っていなければ負の大きな値
func water_depth() -> float:
	if not in_water():
		return -INF
	return water_surface_y() - global_position.y


func _on_water_entered(area: Area3D) -> void:
	if area.has_method("get_surface_y") and not _water_volumes.has(area):
		_water_volumes.append(area)


func _on_water_exited(area: Area3D) -> void:
	_water_volumes.erase(area)
