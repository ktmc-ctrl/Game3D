"""Gemma Scene Assistant - Blender add-on.

Sends a natural-language request to a local OpenAI-compatible endpoint
(Ollama by default), receives Python that uses ``bpy``, shows it in a text
block for review, and can execute it on demand.

WARNING: generated code runs with Blender's full process permissions. The
syntax check is not a sandbox. Save your file before executing.

Install: Edit > Preferences > Add-ons > Install... > pick this file.
Panel:   3D Viewport > Sidebar (N) > Gemma
Headless registration check:
    blender -b --python-expr "import sys; sys.path.insert(0,'<dir>'); import gemma_scene_assistant as m; m.register(); print('ok')"
"""

bl_info = {
    "name": "Gemma Scene Assistant",
    "author": "unity-blender-game-dev skill",
    "version": (0, 2, 0),
    "blender": (3, 6, 0),
    "location": "View3D > Sidebar > Gemma",
    "description": "Generate bpy scripts from natural language via a local LLM endpoint",
    "category": "Development",
}

import ast
import json
import re
import urllib.error
import urllib.request

import bpy

SYSTEM_PROMPT = (
    "You write Python for Blender's bpy API. Reply with a single Python code "
    "block and nothing else. The code must be self-contained, use only bpy and "
    "the standard library, work in Blender 3.6+ and never touch the filesystem, "
    "network, subprocess or os modules. Units are metres. Name every object you "
    "create."
)

FORBIDDEN_MODULES = {"os", "subprocess", "sys", "shutil", "socket", "urllib", "requests", "pathlib", "ctypes"}
TEXT_NAME = "gemma_generated.py"


def _extract_code(content: str) -> str:
    match = re.search(r"```(?:python)?\s*\n(.*?)```", content, re.S)
    return (match.group(1) if match else content).strip()


def _static_check(code: str):
    """Return a list of human-readable problems. Empty list == passes."""
    problems = []
    try:
        tree = ast.parse(code)
    except SyntaxError as exc:
        return [f"SyntaxError: {exc}"]
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                if alias.name.split(".")[0] in FORBIDDEN_MODULES:
                    problems.append(f"imports forbidden module '{alias.name}'")
        elif isinstance(node, ast.ImportFrom):
            if node.module and node.module.split(".")[0] in FORBIDDEN_MODULES:
                problems.append(f"imports forbidden module '{node.module}'")
        elif isinstance(node, ast.Call):
            func = node.func
            name = getattr(func, "id", None) or getattr(func, "attr", None)
            if name in {"exec", "eval", "__import__", "open"}:
                problems.append(f"calls '{name}'")
    return problems


def _request_code(url: str, model: str, prompt: str, timeout: float = 120.0) -> str:
    payload = {
        "model": model,
        "temperature": 0.1,
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": prompt},
        ],
    }
    req = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        body = json.loads(resp.read().decode("utf-8"))
    return _extract_code(body["choices"][0]["message"]["content"])


class GemmaPreferences(bpy.types.AddonPreferences):
    bl_idname = __name__

    url: bpy.props.StringProperty(
        name="Endpoint URL",
        default="http://localhost:11434/v1/chat/completions",
    )
    model: bpy.props.StringProperty(name="Model", default="gemma3:4b")

    def draw(self, context):
        self.layout.prop(self, "url")
        self.layout.prop(self, "model")


class GEMMA_OT_generate(bpy.types.Operator):
    bl_idname = "gemma.generate"
    bl_label = "Generate bpy script"
    bl_description = "Ask the local model for a bpy script and open it in the Text Editor"

    def execute(self, context):
        prefs = context.preferences.addons[__name__].preferences
        prompt = context.scene.gemma_prompt.strip()
        if not prompt:
            self.report({"WARNING"}, "Prompt is empty")
            return {"CANCELLED"}
        try:
            code = _request_code(prefs.url, prefs.model, prompt)
        except urllib.error.URLError as exc:
            self.report({"ERROR"}, f"Endpoint unreachable: {exc}")
            return {"CANCELLED"}
        except (KeyError, ValueError) as exc:
            self.report({"ERROR"}, f"Unexpected response: {exc}")
            return {"CANCELLED"}

        text = bpy.data.texts.get(TEXT_NAME) or bpy.data.texts.new(TEXT_NAME)
        text.clear()
        text.write(code)
        problems = _static_check(code)
        context.scene.gemma_last_check = "; ".join(problems) if problems else "OK"
        level = {"WARNING"} if problems else {"INFO"}
        self.report(level, f"Script written to '{TEXT_NAME}' ({context.scene.gemma_last_check})")
        return {"FINISHED"}


class GEMMA_OT_execute(bpy.types.Operator):
    bl_idname = "gemma.execute"
    bl_label = "Execute generated script"
    bl_description = "Run the reviewed script. This is NOT sandboxed."
    bl_options = {"REGISTER", "UNDO"}

    def execute(self, context):
        text = bpy.data.texts.get(TEXT_NAME)
        if text is None:
            self.report({"ERROR"}, "Nothing generated yet")
            return {"CANCELLED"}
        code = text.as_string()
        problems = _static_check(code)
        if problems:
            self.report({"ERROR"}, "Refusing to run: " + "; ".join(problems))
            return {"CANCELLED"}
        try:
            exec(compile(code, TEXT_NAME, "exec"), {"bpy": bpy, "__name__": "__gemma__"})
        except Exception as exc:  # noqa: BLE001 - surface any script error to the user
            self.report({"ERROR"}, f"Script failed: {exc}")
            return {"CANCELLED"}
        self.report({"INFO"}, "Script executed")
        return {"FINISHED"}


class GEMMA_PT_panel(bpy.types.Panel):
    bl_label = "Gemma Scene Assistant"
    bl_space_type = "VIEW_3D"
    bl_region_type = "UI"
    bl_category = "Gemma"

    def draw(self, context):
        layout = self.layout
        layout.prop(context.scene, "gemma_prompt", text="")
        layout.operator(GEMMA_OT_generate.bl_idname, icon="CONSOLE")
        row = layout.row()
        row.label(text=f"Check: {context.scene.gemma_last_check or '-'}")
        layout.operator(GEMMA_OT_execute.bl_idname, icon="PLAY")
        layout.label(text="Review the script in the Text Editor first.", icon="ERROR")


CLASSES = (GemmaPreferences, GEMMA_OT_generate, GEMMA_OT_execute, GEMMA_PT_panel)


def register():
    for cls in CLASSES:
        bpy.utils.register_class(cls)
    bpy.types.Scene.gemma_prompt = bpy.props.StringProperty(
        name="Prompt", default="Add a low-poly hut with a thatched roof at the origin."
    )
    bpy.types.Scene.gemma_last_check = bpy.props.StringProperty(name="Last check", default="")


def unregister():
    for cls in reversed(CLASSES):
        bpy.utils.unregister_class(cls)
    del bpy.types.Scene.gemma_prompt
    del bpy.types.Scene.gemma_last_check


if __name__ == "__main__":
    register()
