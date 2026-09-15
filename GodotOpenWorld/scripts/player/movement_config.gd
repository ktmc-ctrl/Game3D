class_name MovementConfig
extends Resource
## プレイヤー移動の全数値パラメータ。
## data/player/movement_config.tres として保存し、Player の config に差す。
## ステートは player.config.xxx で参照するだけで、数値を自前で持たない。

@export_group("Ground")
## 歩き速度 (m/s)
@export var walk_speed: float = 4.0
## 走り速度 (m/s)
@export var run_speed: float = 7.0
## 地上での加速 (m/s^2)
@export var ground_acceleration: float = 40.0
## 地上での減速 (m/s^2)
@export var ground_deceleration: float = 50.0
## 向きの追従速度 (rad/s 相当)。0 で即時
@export var rotation_speed: float = 12.0
## 歩ける最大斜度 (度)。これより急な面は「壁」扱いで登攀対象になる
@export var floor_max_angle_deg: float = 45.0
## 走り中のスタミナ消費 (毎秒)。0 で無消費
@export var sprint_stamina_per_second: float = 0.0

@export_group("Air")
## 重力 (m/s^2)
@export var gravity: float = 20.0
## 落下速度の上限 (m/s)
@export var max_fall_speed: float = 40.0
## ジャンプ初速 (m/s)
@export var jump_velocity: float = 8.0
## 空中での操作速度 (m/s)。既に速い場合はその速度を維持する
@export var air_control_speed: float = 5.0
## 空中での加速 (m/s^2)
@export var air_acceleration: float = 12.0
## 足場を離れてからジャンプを受け付ける猶予 (秒)
@export var coyote_time: float = 0.12
## 着地前にジャンプを先行入力できる時間 (秒)
@export var jump_buffer_time: float = 0.1

@export_group("Climb")
## 登攀の移動速度 (m/s)
@export var climb_speed: float = 2.5
## 壁に押し付ける速度 (m/s)。張り付き維持用
@export var climb_stick_speed: float = 1.5
## 登攀で移動中のスタミナ消費 (毎秒)
@export var climb_stamina_per_second: float = 8.0
## 壁に張り付いて静止中のスタミナ消費 (毎秒)
@export var climb_idle_stamina_per_second: float = 2.0
## 壁ジャンプのスタミナ消費
@export var climb_jump_stamina_cost: float = 15.0
## 壁ジャンプの上方向初速 (m/s)
@export var wall_jump_up_velocity: float = 7.0
## 壁ジャンプの壁から離れる初速 (m/s)
@export var wall_jump_away_velocity: float = 4.0
## 壁から離れた後、再び張り付けるようになるまでの時間 (秒)
@export var climb_regrab_cooldown: float = 0.3
## よじ登りにかける時間 (秒)
@export var mantle_duration: float = 0.35
## よじ登り着地点を縁からさらに前へずらす距離 (m)
@export var mantle_forward_margin: float = 0.2

@export_group("Glide")
## 滑空中の落下速度 (m/s)
@export var glide_fall_speed: float = 1.5
## 滑空中の前進速度 (m/s)
@export var glide_forward_speed: float = 6.0
## 滑空中の加速 (m/s^2)
@export var glide_acceleration: float = 8.0
## 滑空中に入力が無いときの減速 (m/s^2)
@export var glide_deceleration: float = 2.0
## 滑空のスタミナ消費 (毎秒)
@export var glide_stamina_per_second: float = 6.0
## 滑空を開けるのに必要な地面までの高さ (m)
@export var glide_min_height: float = 2.0

@export_group("Swim")
## 泳ぎ速度 (m/s)
@export var swim_speed: float = 3.0
## 泳ぎの加速 (m/s^2)
@export var swim_acceleration: float = 15.0
## 泳ぎで移動中のスタミナ消費 (毎秒)
@export var swim_stamina_per_second: float = 4.0
## 水面で静止中のスタミナ消費 (毎秒)
@export var swim_idle_stamina_per_second: float = 1.0
## 足元がこの深さ (m) 以上水に浸かったら泳ぎに入る
@export var swim_enter_depth: float = 1.0
## 泳ぎから歩きに戻る判定のヒステリシス (m)
@export var swim_exit_hysteresis: float = 0.2
## 泳いでいるとき、原点 (足元) を水面からこの深さに保つ (m)
@export var swim_float_depth: float = 1.3
## 浮力の強さ。目標高さとの差にこの係数を掛けて上下速度にする
@export var swim_buoyancy: float = 5.0
## スタミナ切れで沈む速度 (m/s)
@export var sink_speed: float = 2.0
## 沈み始めてから最後の接地位置に戻されるまでの時間 (秒)
@export var drown_respawn_delay: float = 2.0
## 復帰時に回復するスタミナの割合 (0-1)
@export var drown_stamina_refill_ratio: float = 0.5

@export_group("Stamina")
## スタミナ最大値
@export var stamina_max: float = 100.0
## 地上での回復速度 (毎秒)
@export var stamina_regen_per_second: float = 25.0
## 消費してから回復が始まるまでの待ち (秒)
@export var stamina_regen_delay: float = 0.5
