# Local toolchain reference

How to find and drive the editors this skill depends on, and how to validate
the optional Gemma (OpenAI-compatible) integration.

## Locate Unity

| Platform | Typical editor binary |
|----------|-----------------------|
| macOS (Unity Hub) | `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity` |
| Linux (Unity Hub) | `~/Unity/Hub/Editor/<version>/Editor/Unity` |
| Windows (Unity Hub) | `C:\Program Files\Unity\Hub\Editor\<version>\Editor\Unity.exe` |

1. Read `ProjectSettings/ProjectVersion.txt` (`m_EditorVersion`) to learn which
   version the project expects. Use that exact version when it is installed;
   otherwise pick the closest patch on the same LTS stream and say so.
2. List installed versions: `ls /Applications/Unity/Hub/Editor` (macOS) or
   `ls ~/Unity/Hub/Editor` (Linux). Unity Hub CLI is optional; the folder scan is
   enough.
3. Set `UNITY_EDITOR` to the binary path once and reuse it in commands below.

### Compile / batch commands

```sh
# Compile the project and quit; non-zero exit means compile errors.
"$UNITY_EDITOR" -batchmode -nographics -quit \
  -projectPath "<project>" -logFile - 

# Run a static editor method (smoke tests, scene builders).
"$UNITY_EDITOR" -batchmode -nographics -quit \
  -projectPath "<project>" -logFile - \
  -executeMethod SengokuWarSim.Editor.SengokuSmokeTest.Run

# Run EditMode tests (Unity Test Framework).
"$UNITY_EDITOR" -batchmode -nographics \
  -projectPath "<project>" -logFile - \
  -runTests -testPlatform EditMode -testResults "<project>/TestResults.xml"
```

The first batch run of a fresh project imports assets and regenerates
`Library/`; expect several minutes. A missing or expired license makes batch
mode exit before compiling. Check the log for `Licensing::Module`.

Never edit `Library/`, `Temp/`, `Logs/`, `obj/` or `*.csproj`/`*.sln` by hand.

## Locate Blender

| Platform | Binary |
|----------|--------|
| macOS | `/Applications/Blender.app/Contents/MacOS/Blender` |
| Linux | `blender` on PATH, or `~/blender-<ver>/blender` |
| Windows | `C:\Program Files\Blender Foundation\Blender <ver>\blender.exe` |

Headless asset generation:

```sh
"$BLENDER" -b -P path/to/script.py -- --out "<project>/Assets/Models"
```

Everything after `--` reaches the script through `sys.argv`. Use FBX export
with `axis_forward='-Z', axis_up='Y'` and `apply_scale_options='FBX_SCALE_ALL'`
so meshes arrive in Unity at 1 unit = 1 metre without a rotation fix-up.

Validate an add-on headlessly:

```sh
"$BLENDER" -b --python-expr "import bpy, sys; sys.path.insert(0,'<dir>'); import gemma_scene_assistant as m; m.register(); print('registered')"
```

## Local Gemma / OpenAI-compatible endpoint

Default endpoint: `http://localhost:11434/v1/chat/completions` (Ollama).

Before debugging controller code:

```sh
curl -s http://localhost:11434/api/tags          # server up? which models?
ollama pull gemma3:4b                            # if the requested model is missing
curl -s http://localhost:11434/v1/chat/completions \
  -H 'Content-Type: application/json' \
  -d '{"model":"gemma3:4b","messages":[{"role":"user","content":"hi"}]}'
```

If `api/tags` fails, the server is not running (`ollama serve`). If the model
name is not listed, the controller will get a 404 no matter what the code does.

LM Studio and llama.cpp servers expose the same `/v1/chat/completions` route on
their own ports; change the URL field in the Unity window or the Blender add-on
preferences rather than the code.

## When the editors are not installed (CI, remote containers)

- Keep engine-agnostic logic in an assembly with `noEngineReferences: true` so
  it can be reviewed and unit-tested independently.
- Syntax-check C# with a parser (e.g. `tree-sitter-c-sharp` from PyPI) and
  Blender scripts with `python -m py_compile`.
- Report clearly that compile, play-mode and render validation did not run.
