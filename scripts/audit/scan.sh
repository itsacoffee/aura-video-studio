#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="${1:-.}"
ART_DIR="$REPO_ROOT/artifacts/audit"
REPORT="$ART_DIR/merge_audit_report.md"
EFFECTIVE="$ART_DIR/effective_appsettings.json"

mkdir -p "$ART_DIR"

add_report() { printf "%s\n" "$1" >> "$REPORT"; }

echo "## Merge Audit Report" > "$REPORT"
add_report "Generated: $(date -Iseconds)"
add_report ""

hardfail=0

add_report "### Conflict Markers"
conflicts=$(grep -RInE '^(<<<<<<<|=======|>>>>>>>)' "$REPO_ROOT" | grep -vE '(package-lock.json|yarn.lock)' || true)
if [ -n "$conflicts" ]; then
  hardfail=1
  add_report "**FOUND conflict markers:**"
  echo "$conflicts" | sed 's/^/- /' >> "$REPORT"
else
  add_report "No conflict markers found."
fi
add_report ""

add_report "### Duplicate Files (by normalized name)"
mapfile -t files < <(find "$REPO_ROOT" -type f)
declare -A list
for f in "${files[@]}"; do
  base=$(basename "$f"); name="${base%.*}"; key=$(echo "$name" | tr '[:upper:]' '[:lower:]')
  list[$key]="${list[$key]}|$f"
done
dupes=0
for k in "${!list[@]}"; do
  IFS='|' read -r -a arr <<< "${list[$k]}"
  if [ "${#arr[@]}" -gt 2 ]; then
    add_report "- $k"
    for p in "${arr[@]}"; do [ -n "$p" ] && add_report "  - $p"; done
    dupes=1
  fi
done
[ "$dupes" -eq 0 ] && add_report "No duplicate basenames detected."
add_report ""

add_report "### Duplicate C# Type Names"
declare -A types
while IFS= read -r -d '' f; do
  while IFS= read -r line; do
    if [[ "$line" =~ ^[[:space:]]*(public|internal|protected|private)?[[:space:]]*(sealed[[:space:]]+|abstract[[:space:]]+)?(class|interface|record|enum)[[:space:]]+([A-Za-z_][A-Za-z0-9_]*) ]]; then
      name="${BASH_REMATCH[4]}"
      types[$name]="${types[$name]}|$f"
    fi
  done <"$f"
done < <(find "$REPO_ROOT" -type f -name '*.cs' -print0)

dupt=0
for k in "${!types[@]}"; do
  IFS='|' read -r -a arr <<< "${types[$k]}"
  if [ "${#arr[@]}" -gt 2 ]; then
    add_report "- $k"
    for p in "${arr[@]}"; do [ -n "$p" ] && add_report "  - $p"; done
    dupt=1
  fi
done
[ "$dupt" -eq 0 ] && add_report "No duplicate C# type names detected."
add_report ""

add_report "### Duplicate TS/TSX Default Export Names"
declare -A tsnames
while IFS= read -r -d '' f; do
  name=""
  if grep -Eq 'export[[:space:]]+default[[:space:]]+function[[:space:]]+[A-Za-z_][A-Za-z0-9_]*' "$f"; then
    name=$(grep -Eo 'export[[:space:]]+default[[:space:]]+function[[:space:]]+[A-Za-z_][A-Za-z0-9_]*' "$f" | head -n1 | awk '{print $4}')
  elif grep -Eq 'export[[:space:]]+default[[:space:]]+class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*' "$f"; then
    name=$(grep -Eo 'export[[:space:]]+default[[:space:]]+class[[:space:]]+[A-Za-z_][A-Za-z0-9_]*' "$f" | head -n1 | awk '{print $4}')
  fi
  if [ -n "$name" ]; then
    tsnames[$name]="${tsnames[$name]}|$f"
  fi
done < <(find "$REPO_ROOT" -type f \( -name '*.ts' -o -name '*.tsx' \) -print0)

dupts=0
for k in "${!tsnames[@]}"; do
  IFS='|' read -r -a arr <<< "${tsnames[$k]}"
  if [ "${#arr[@]}" -gt 2 ]; then
    add_report "- $k"
    for p in "${arr[@]}"; do [ -n "$p" ] && add_report "  - $p"; done
    dupts=1
  fi
done
[ "$dupts" -eq 0 ] && add_report "No duplicate TS default export names detected."
add_report ""

add_report "### Duplicate XAML Resource Keys"
declare -A xkeys
while IFS= read -r -d '' f; do
  while IFS= read -r k; do
    key="${k#*x:Key="}"; key="${key%%"*}"
    xkeys[$key]="${xkeys[$key]}|$f"
  done < <(grep -Eo 'x:Key\s*=\s*"[^"]+"' "$f" || true)
done < <(find "$REPO_ROOT" -type f -name '*.xaml' -print0)

dupkeys=0
for k in "${!xkeys[@]}"; do
  IFS='|' read -r -a arr <<< "${xkeys[$k]}"
  if [ "${#arr[@]}" -gt 2 ]; then
    add_report "- $k"
    for p in "${arr[@]}"; do [ -n "$p" ] && add_report "  - $p"; done
    dupkeys=1
  fi
done
[ "$dupkeys" -eq 0 ] && add_report "No duplicate XAML resource keys detected."
add_report ""

add_report "### TODO/FIXME/HACK markers"
marks=$(grep -RInE 'TODO|FIXME|HACK' "$REPO_ROOT" || true)
if [ -n "$marks" ]; then
  echo "$marks" | sed 's/^/- /' >> "$REPORT"
else
  add_report "No markers found."
fi
add_report ""

add_report "### JSON validation and effective appsettings"
valid=1
tmp="{}"
while IFS= read -r -d '' j; do
  if jq empty "$j" >/dev/null 2>&1; then
    tmp=$(jq -s 'reduce .[] as $item ({}; . * $item)' <<<"[$tmp, $(cat "$j")]")
  else
    add_report "**INVALID JSON:** $j"
    valid=0
  fi
done < <(find "$REPO_ROOT" -type f -name 'appsettings*.json' -print0 | sort -z)

echo "$tmp" | jq . > "$EFFECTIVE" || echo "{}" > "$EFFECTIVE"
add_report "Wrote effective appsettings: $EFFECTIVE"
add_report ""

if [ "$hardfail" -eq 1 ] || [ "$valid" -eq 0 ]; then
  add_report "**RESULT: FAIL**"
  echo "FAIL: see $REPORT"
  exit 1
else
  add_report "**RESULT: PASS**"
  echo "PASS: see $REPORT"
  exit 0
fi
