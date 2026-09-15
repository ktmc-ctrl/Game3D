# GodotOpenWorld ― オープンワールド探索プロトタイプ

Godot 4.3 以降 / GDScript。原神の探索部分を参考にした移動・探索のプロトタイプ。
見た目はプリミティブ。数値は `data/player/movement_config.tres` に集約してあり、インスペクタで調整する。

## 構成

```
project.godot                 入力マップ (WASD / Space / Shift / F3)、物理レイヤー名
addons/                       フェーズ2で Terrain3D が入る
data/player/movement_config.tres  移動・登攀・滑空・泳ぎ・スタミナの全数値 (MovementConfig)
scenes/main.tscn              入口。テストマップ + Player + CameraRig + DebugHud
scenes/player/player.tscn     CharacterBody3D + Visual + Sensors + Stamina + StateMachine
scenes/camera/camera_rig.tscn Yaw → Pitch → SpringArm3D → Camera3D
scenes/ui/debug_hud.tscn      スタミナバー・ステート表示・センサー状態
scenes/world/water_volume.tscn 水域 (Area3D + 水面の平面)
scenes/test/test_map.tscn     坂 (30/50/70°)、段差、崖、塔、登攀禁止の壁、池
scenes/test/smoke_test.tscn   ヘッドレスの動作テスト
scripts/player/               player.gd / movement_config.gd / stamina_component.gd /
                              player_sensors.gd / player_state_machine.gd / states/*.gd
```

### 設計

- `Player` は入力の集約と共通ヘルパー (重力、水平移動、向き) だけ。移動ロジックは `StateMachine` 配下のステートが持つ。
- ステート: Idle / Move / Jump / Fall / Climb / Mantle / Glide / Swim。遷移は `transition_to(&"Climb")` のように名前で明示。
- `PlayerSensors` が壁・段差の縁・地面距離・水面高さをまとめて返す。ステートは RayCast を直接触らない。
- `StaminaComponent` は値の管理とシグナルだけ。いつ消費/回復するかはステートが決める。
- 見た目は `Player/Visual` 以下に隔離。モデル差し替え時はこの中身を入れ替える。

### 物理レイヤー

| 番号 | 名前 | 用途 |
|---|---|---|
| 1 | world | 地形・構造物 |
| 2 | player | プレイヤー本体 |
| 3 | water | 水域 Area3D |
| 4 | no_climb | 当たるが登れない面 |

## 操作

| 操作 | 内容 |
|---|---|
| WASD | 移動 (カメラ基準) |
| Shift | 走り |
| Space | ジャンプ / 空中で滑空の開閉 / 登攀中は壁ジャンプ |
| マウス | カメラ回転 |
| Esc | マウスの解放・再キャプチャ |
| F3 | デバッグ UI 表示切替 |

登攀は壁に向かって進むと自動で張り付く。壁の上端に来ると自動でよじ登る。低い段差も前進で乗り越える。

## ヘッドレスの動作テスト

Godot の実行ファイルで:

```
godot --headless --fixed-fps 60 --path GodotOpenWorld res://scenes/test/smoke_test.tscn
```

入力を流し込んで歩き・走り・ジャンプ・登攀・よじ登り・壁ジャンプ・滑空・泳ぎ・溺れ復帰・スタミナ回復を順に確認する。失敗があると終了コード 1。

## フェーズ1 エディタでの確認手順

1. Godot 4.3 以降で `GodotOpenWorld/project.godot` を開く (初回は import に少し時間がかかる)。
2. F5 で実行。`scenes/main.tscn` が起動し、左上にデバッグ UI が出る。
3. 歩き/走り: WASD で移動、Shift で速くなる。State が `Move (walk)` / `Move (run)` になる。
4. ジャンプ: Space。State が `Jump` → `Fall` → `Idle`。
5. 登攀: 正面の灰色の塔 (奥) に向かって進むと `Climb` になり、スタミナが減る。W で登り、A/D で横移動、S で降りる。頂上に着くと `Mantle` を経て上に立つ。
6. 壁ジャンプ: 登攀中に Space。壁から離れて `Fall` になる。
7. スタミナ切れ: 塔を登ったまま待つと、0 になった瞬間に落ちる。地上に戻ると回復する。
8. 滑空: 塔の頂上から歩いて落ち、空中で Space。`Glide` になり落下が遅くなる。WASD で進む。もう一度 Space で閉じる。スタミナが 0 になると強制的に閉じる。
9. 泳ぎ: 右奥の池 (青い面) に入る。`Swim` で水面に浮く。スタミナが 0 になると沈み、2 秒後に最後に立っていた場所へ戻る。砂色の斜面から歩いて上がれる。
10. 段差: 右手前の低い段差 (0.5m / 1.2m) に向かって歩くと乗り越える。3m の壁は登攀してから上に上がる。
11. 登攀禁止: 赤い壁は登れずに止まる。
12. 坂: 左奥の 3 枚の坂。30° は歩ける。50° と 70° は歩けず、押し続けると登攀に切り替わる。
13. 数値の調整: `data/player/movement_config.tres` をダブルクリックし、インスペクタで変更。実行中でも反映される値が多い (速度・重力・スタミナなど)。
