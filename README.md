# Game3D ― 戦国村人合戦 (SengokuWarSim)

戦国時代の村人同士が田畑を挟んで戦う 3D 戦争シミュレーション。Unity 2022.3 LTS 用プロジェクトと、
村人モデルを生成する Blender スクリプト、Unity/Blender 開発用の Claude Code スキルを含む。

```
.claude/skills/unity-blender-game-dev/   Claude Code スキル (SKILL.md + 参照資料 + Gemma 連携アセット)
SengokuWarSim/                            Unity プロジェクト
  Assets/Scenes/Battle.unity              入口シーン
  Assets/Scripts/Sim/                     戦闘ロジック (UnityEngine 非依存, noEngineReferences)
  Assets/Scripts/Runtime/                 3D 表示・カメラ・操作・HUD
  Assets/Editor/                          シーンビルダー / バッチ用スモークテスト / Gemma コントローラ
  Assets/Tests/EditMode/                  NUnit テスト (Sim 層)
  Blender/make_villager.py                村人 FBX 生成スクリプト
tools/                                    .meta 生成、Unity なしでの Sim ビルド・テスト、構文チェック
SengokuLife/                              Unreal Engine 5.8 C++ プロジェクト (T001-A: Module 基盤のみ)
  Source/SengokuSim/                      Actor / Engine 非依存の Simulation 層 (依存は Core のみ)
  Source/SengokuLife/                     ゲーム側 Runtime Module (SengokuLife -> SengokuSim のみ許可)
  tools/build_sengokulife_mac.sh          macOS での Development Editor 通常ビルド
```

## 遊び方

1. Unity Hub で `SengokuWarSim/` を 2022.3 系で開く (初回は Library 生成に数分)
2. `Assets/Scenes/Battle.unity` を開いて Play
   - シーンが壊れていたらメニュー `Sengoku > Build Battle Scene` で作り直せる
3. 上ノ村 (藍色の帯) を指揮して、下ノ村 (茜色) を打ち破る

| 操作 | 内容 |
|------|------|
| 左クリック / ドラッグ | 部隊を選択 (Shift で追加) |
| 右クリック (地面) | 移動。複数選択時は横一列に展開 |
| 右クリック (敵部隊) | 攻撃 |
| WASD / 矢印 | カメラ移動、Q/E・中ボタンドラッグで回転、ホイールでズーム |
| Space | 一時停止 |
| 1 / 2 / 3 | 速度 x1 / x2 / x4 |
| H | 停止 |
| Ctrl+A | 味方全選択 |
| R (決着後) | 同じ戦場で再戦 |

## 部隊 (どちらの村も同じ編成、計 8 部隊)

| 部隊 | 人数 | 役割 |
|------|------|------|
| 竹槍衆 | 24 | 前線の壁。士気が高め |
| 鍬衆 | 20 | 手数の多い近接。脆い |
| 狩弓衆 | 16 | 射程 18m。接近されると弱い |
| 投石衆 | 18 | 射程 12m。士気が低い |
| 村長組 | 8 | 少数精鋭。半径 12m の味方の士気回復を助ける |

## ルール

- 一斉攻撃ごとに兵一人あたり命中判定。命中率は地形・装甲・士気・側面攻撃で変わる
- 死者が出ると士気が下がる。士気 20 未満で敗走、45 まで回復すると立ち直る。近くの味方の敗走・壊滅でも下がる
- 敗走した部隊は自陣の端まで逃げると戦場離脱
- 地形: 水田 (移動 55%、命中 -20%)、丘 (被弾 -25%、射撃射程 +20%)、林 (矢の被弾 -40%)、街道 (移動 +15%)
- 相手の有効部隊 (生存かつ敗走していない) が無くなれば勝ち。15 分経過なら残存兵数で判定

## 検証

### Unity がある環境

```sh
UNITY_EDITOR=/Applications/Unity/Hub/Editor/2022.3.20f1/Unity.app/Contents/MacOS/Unity
# コンパイル + ヘッドレス戦闘 + シーン組み立てのスモークテスト
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath SengokuWarSim -logFile - \
  -executeMethod SengokuWarSim.Editor.SengokuSmokeTest.Run
# EditMode テスト
"$UNITY_EDITOR" -batchmode -nographics -projectPath SengokuWarSim -logFile - \
  -runTests -testPlatform EditMode -testResults SengokuWarSim/TestResults.xml
```

### Unity が無い環境 (CI など)

Sim 層は UnityEngine に依存しないので、Roslyn と NUnitLite だけでコンパイル・テストできる。

```sh
CSC=<path>/csc.dll NUNIT_DIR=<dir with nunit.framework.dll + nunitlite.dll> tools/build_sim_local.sh
python3 tools/check_csharp_syntax.py SengokuWarSim/Assets   # tree-sitter による構文チェック
```

## Blender

```sh
blender -b -P SengokuWarSim/Blender/make_villager.py -- --out SengokuWarSim/Assets/Models --preview
```

## Gemma / ローカル LLM 連携 (任意)

- Unity: `Window > Gemma > Scene Controller`。Ollama (`http://localhost:11434/v1/chat/completions`) に自然言語で指示し、
  `create` / `transform` / `set_color` の JSON だけを受け付けてシーンに反映する (Undo 対応)
- Blender: `.claude/skills/unity-blender-game-dev/assets/gemma_scene_assistant.py` をアドオンとしてインストール
- 手順の詳細は `.claude/skills/unity-blender-game-dev/references/local-toolchain.md`
