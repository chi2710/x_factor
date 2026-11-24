#!/bin/bash
# Kiểm tra môi trường + biên dịch thử toàn bộ Assets/Resources/Scripts bằng Roslyn của Unity.
# Không cần mở Unity. Chạy từ bất kỳ đâu: bash .claude/init.sh
set -e

UNITY_DIR="D:/Unity/6000.5.0f1/Editor/Data"
DOTNET="$UNITY_DIR/DotNetSdk/dotnet.exe"
CSC="$UNITY_DIR/DotNetSdk/sdk/8.0.318/Roslyn/bincore/csc.dll"
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
OUT_DIR="${TMPDIR:-/tmp}/sample2d-check"

cd "$PROJECT_DIR"   # đường dẫn reference trong .csproj là tương đối

echo "=== Kiểm tra môi trường ==="
for f in "$DOTNET" "$CSC" Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj Assets/InputSystem_Actions.cs; do
  [ -e "$f" ] || { echo "THIẾU: $f"; echo "(Nếu thiếu .csproj: mở Unity > Preferences > External Tools > Regenerate project files)"; exit 1; }
done
echo "OK"

echo "=== Gom reference từ .csproj ==="
mkdir -p "$OUT_DIR"
RSP="$OUT_DIR/build.rsp"
# Lấy HintPath, bỏ 3 shim netstandard/System.Reflection.Emit* (trùng với Facades), loại trùng theo tên file.
grep -ho '<HintPath>[^<]*' Assembly-CSharp.csproj Assembly-CSharp-Editor.csproj \
  | sed 's/<HintPath>//' \
  | grep -v 'shims[\\/]netstandard[\\/]System\.Reflection\.Emit' \
  | awk -F'[\\\\/]' '!seen[$NF]++' \
  | sed 's/.*/-r:"&"/' > "$OUT_DIR/refs.txt"
# ProjectReference (task Behavior Designer, Wrappers…) không có HintPath: lấy DLL Unity đã build sẵn.
ls Library/ScriptAssemblies/Opsive.*.dll 2>/dev/null | sed 's/.*/-r:"&"/' >> "$OUT_DIR/refs.txt"
echo "$(wc -l < "$OUT_DIR/refs.txt") reference"

DEFINES=$(grep -o '<DefineConstants>[^<]*' Assembly-CSharp-Editor.csproj | head -1 | sed 's/<DefineConstants>//')

{
  echo "-nologo -noconfig -nostdlib+ -t:library -langversion:9.0 -nowarn:CS0649,CS0414,CS0169"
  echo "-out:\"$(cygpath -m "$OUT_DIR")/check.dll\""   # csc cần đường dẫn Windows
  echo "-define:\"$DEFINES\""
  cat "$OUT_DIR/refs.txt"
  echo "\"Assets/InputSystem_Actions.cs\""
  find Assets/Resources/Scripts -name '*.cs' | sed 's/.*/"&"/'
} > "$RSP"

echo "=== Biên dịch Assets/Resources/Scripts ==="
if "$DOTNET" "$CSC" "@$RSP" > "$OUT_DIR/build.log" 2>&1; then
  grep -c 'warning' "$OUT_DIR/build.log" | xargs -I{} echo "{} warning"
  echo "=== Môi trường OK — 0 lỗi biên dịch ==="
else
  grep 'error' "$OUT_DIR/build.log"
  echo "=== BIÊN DỊCH LỖI: $(grep -c 'error' "$OUT_DIR/build.log") lỗi (log: $OUT_DIR/build.log) ==="
  exit 1
fi
