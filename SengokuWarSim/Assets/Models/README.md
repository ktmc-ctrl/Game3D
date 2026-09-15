# Models

`SengokuWarSim/Blender/make_villager.py` の FBX 出力先。

```sh
blender -b -P SengokuWarSim/Blender/make_villager.py -- --out SengokuWarSim/Assets/Models --preview
```

現状のゲームは `FigureFactory` がプリミティブ合成で村人を作るので、このフォルダが空でも動く。
FBX に差し替える場合は `SquadView.BuildFigures` で `FigureFactory.Build` の代わりに
ここのモデルを Instantiate する。
