@tool
class_name WaterVolume
extends Area3D
## 水域。原点が水面の高さ。Area3D の箱は原点から depth だけ下に伸びる。
## 見た目は今は StandardMaterial の平面。フェーズ2で水面シェーダーに差し替える。

## 水面の広さ (x, z)
@export var size := Vector2(20.0, 20.0):
	set(value):
		size = value
		_rebuild()
## 水の深さ
@export var depth: float = 3.5:
	set(value):
		depth = value
		_rebuild()
## 水面の色
@export var surface_color := Color(0.15, 0.45, 0.8, 0.6):
	set(value):
		surface_color = value
		_rebuild()


func _ready() -> void:
	_rebuild()


func get_surface_y() -> float:
	return global_position.y


func _rebuild() -> void:
	if not is_node_ready():
		return
	var shape_node: CollisionShape3D = get_node_or_null("CollisionShape3D")
	if shape_node:
		var box := shape_node.shape as BoxShape3D
		if box == null:
			box = BoxShape3D.new()
			shape_node.shape = box
		box.size = Vector3(size.x, depth, size.y)
		shape_node.position = Vector3(0.0, -depth * 0.5, 0.0)
	var surface: MeshInstance3D = get_node_or_null("Surface")
	if surface:
		var plane := surface.mesh as PlaneMesh
		if plane == null:
			plane = PlaneMesh.new()
			surface.mesh = plane
		plane.size = size
		var mat := surface.material_override as StandardMaterial3D
		if mat == null:
			mat = StandardMaterial3D.new()
			surface.material_override = mat
		mat.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
		mat.cull_mode = BaseMaterial3D.CULL_DISABLED
		mat.albedo_color = surface_color
