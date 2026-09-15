#!/usr/bin/env bash
# Unity なしで Sim アセンブリをコンパイルし、EditMode テストを NUnitLite で実行する。
# 前提: $DOTNET (muxer), $CSC (csc.dll), $NUNIT_DIR (nunit.framework.dll と nunitlite.dll)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${OUT:-$ROOT/.local-build}"
DOTNET="${DOTNET:-$HOME/.dotnet/dotnet}"
: "${CSC:?set CSC to csc.dll path}"
: "${NUNIT_DIR:?set NUNIT_DIR to a dir containing nunit.framework.dll and nunitlite.dll}"
RT_DIR="$(dirname "$($DOTNET --list-runtimes | head -1 | sed -E 's/.*\[(.*)\]/\1/')")/Microsoft.NETCore.App/$($DOTNET --list-runtimes | head -1 | awk '{print $2}')"
mkdir -p "$OUT"
REFS=()
for f in "$RT_DIR"/System.*.dll "$RT_DIR"/netstandard.dll "$RT_DIR"/mscorlib.dll "$RT_DIR"/System.dll; do
  [ -f "$f" ] && REFS+=("-r:$f")
done

echo "== compile SengokuWarSim.Sim"
"$DOTNET" "$CSC" -nologo -nostdlib -noconfig -target:library -langversion:9.0 -warnaserror+ -nowarn:CS1591 \
  -out:"$OUT/SengokuWarSim.Sim.dll" "${REFS[@]}" "$ROOT"/SengokuWarSim/Assets/Scripts/Sim/*.cs

echo "== compile tests"
"$DOTNET" "$CSC" -nologo -nostdlib -noconfig -target:exe -langversion:9.0 -define:UNITY_INCLUDE_TESTS \
  -out:"$OUT/SengokuWarSim.Tests.dll" "${REFS[@]}" -r:"$OUT/SengokuWarSim.Sim.dll" \
  -r:"$NUNIT_DIR/nunit.framework.dll" -r:"$NUNIT_DIR/nunitlite.dll" \
  -main:SengokuWarSim.Tests.LocalRunner \
  "$ROOT"/SengokuWarSim/Assets/Tests/EditMode/*.cs "$ROOT"/tools/LocalRunner.cs
cp "$NUNIT_DIR"/nunit.framework.dll "$NUNIT_DIR"/nunitlite.dll "$OUT"/
cat > "$OUT/SengokuWarSim.Tests.runtimeconfig.json" <<JSON
{ "runtimeOptions": { "tfm": "net8.0", "framework": { "name": "Microsoft.NETCore.App", "version": "8.0.0" }, "rollForward": "Major" } }
JSON

echo "== run tests"
"$DOTNET" "$OUT/SengokuWarSim.Tests.dll" --noresult --labels=On
