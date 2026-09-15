#!/usr/bin/env python3
"""Unity が無い環境向けの C# 構文チェック (tree-sitter-c-sharp)。意味検査はしない。"""
import sys, pathlib
import tree_sitter_c_sharp as tscs
from tree_sitter import Language, Parser

parser = Parser(Language(tscs.language()))
root = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else ".")
bad = 0
files = sorted(root.rglob("*.cs"))
for f in files:
    tree = parser.parse(f.read_bytes())
    errors = []
    stack = [tree.root_node]
    while stack:
        n = stack.pop()
        if n.type == "ERROR" or n.is_missing:
            errors.append(f"{f}:{n.start_point[0]+1}:{n.start_point[1]+1} {n.type} '{n.text[:40].decode(errors='replace') if n.text else ''}'")
        stack.extend(n.children)
    if errors:
        bad += 1
        print("\n".join(errors))
print(f"{len(files)} files checked, {bad} with syntax errors")
sys.exit(1 if bad else 0)
