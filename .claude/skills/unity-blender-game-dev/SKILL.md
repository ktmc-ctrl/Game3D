---
name: unity-blender-game-dev
description: Build, modify, and validate 3D game projects on this Mac with Unity and Blender, including optional local Gemma/OpenAI-compatible editor tooling. Use for requests to create or prototype a game, implement Unity gameplay or editor code, make Blender assets for a game, connect the supplied Gemma controllers, or test those outputs in this environment.
---

# Unity and Blender Game Development

Use Unity as the game runtime and Blender for 3D asset work unless the user chooses a different division. Treat local Gemma integration as an optional authoring tool; add it only when the request involves natural-language scene control or local LLM workflows.

## Start from the actual project

1. Read project-local instructions before changing files.
2. Detect a Unity project by `ProjectSettings/ProjectVersion.txt` and a Blender project by `.blend` files or Blender scripts. Preserve the project's chosen render pipeline, input system, directory layout, and editor version.
3. If the user asks for a new game and the current directory is not a Unity project, create a clearly named project directory within the authorized workspace. If they refer to an existing game without identifying it, locate likely projects first and avoid mutating an unrelated project.
4. Read [references/local-toolchain.md](references/local-toolchain.md) when locating or running the installed editors, validating integrations, or configuring the local endpoint.

## Build a playable Unity slice

- Put runtime scripts under `Assets/`; put editor-only code under `Assets/Editor/`.
- Implement the smallest playable slice that demonstrates the requested loop, including controls, win/fail or completion behavior when relevant, camera, lighting, and readable feedback.
- Create or edit scenes, prefabs, materials, and project settings through Unity-supported serialized assets or editor scripts. Never edit `Library/`, `Temp/`, or generated solution files as source.
- Keep scene mutations undoable in editor tools using `Undo.RecordObject`, `Undo.RegisterCreatedObjectUndo`, or related APIs.
- After changes, compile in the project's Unity version. Add focused EditMode or PlayMode tests when behavior warrants them, and run a batch smoke test for generated editor tooling or scene construction.
- Do not claim playability from compilation alone. Verify the scene or test the core loop when the installed editor and license allow it.

## Create Blender assets

- Prefer deterministic Blender Python for procedural assets, repeated exports, or headless verification. Use interactive Blender only when visual sculpting or user review requires it.
- Keep scale, origin, forward/up axes, naming, materials, and export format aligned with the target Unity project. Export into an intentional project asset directory rather than an arbitrary workspace location.
- Validate add-ons by registering and exercising their main operation with Blender in background mode. For visible asset work, render a preview or inspect the result before delivery.

## Use the local Gemma controllers

- For Unity natural-language scene control, copy [assets/Gemma4UnityController.cs](assets/Gemma4UnityController.cs) to the target project's `Assets/Editor/Gemma4UnityController.cs`, then compile it in that project. It accepts only validated `create`, `transform`, and `set_color` JSON actions and records Unity Undo operations.
- For Blender natural-language `bpy` generation, copy [assets/gemma_scene_assistant.py](assets/gemma_scene_assistant.py) and install it as a Blender add-on. Generated Python runs with Blender's full process permissions; syntax validation is not a sandbox. Use trusted local endpoints and save work before execution.
- The default OpenAI-compatible endpoint is Ollama-style localhost. Confirm the server and the requested model actually exist before diagnosing controller code.
- Treat the bundled files as starting points. Adapt them to project-specific object schemas or actions when the user requests broader capabilities, then repeat their compile and smoke tests.

## Finish with evidence

Report the playable entry scene, controls, key created assets, and the exact validation performed. State separately when an editor compile passed but visual behavior, a live model endpoint, or a player build was not exercised.
