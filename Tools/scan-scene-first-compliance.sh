#!/usr/bin/env bash
# scan-scene-first-compliance.sh
# 静态扫描 BrotatoLike 生产代码中是否仍有代码拼装正式 UI 的模式。
# 需要标注 scene-first exception 注释或属于豁免类别的才能通过。
#
# 用法: bash Tools/scan-scene-first-compliance.sh
# 返回: 0 = 合规, 1 = 存在未豁免的代码创建 UI

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
GAME_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
SRC_DIR="$GAME_DIR/Src/Game"

# 豁免模式（这些文件/目录中的代码创建是允许的）
EXEMPT_DIRS=("Src/Validation" ".history" "Validation" "validation")
# 豁免行内容模式
EXEMPT_LINES=("scene-first exception" "metadata-only" "debug" "Debug" "validation" "Validation")
# 允许但记录的模式（metadata-only node）
ALLOWED_PATTERNS=("WaveRuntimeState" "RecoveryTickService" "ExperiencePickupLayer" "PointTargetingSession" "ExperiencePickup" "pickup = new")

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo "=== Scene-First Compliance Scan ==="
echo "Source: $SRC_DIR"
echo ""

# Step 1: grep for code-created UI patterns
MATCHES=$(
  grep -rn 'new \(Label\|Control\|ProgressBar\|HBoxContainer\|VBoxContainer\|PanelContainer\|ColorRect\|TextureRect\|Panel\|Node2D\)\b' \
    "$SRC_DIR" --include='*.cs' 2>/dev/null || true
)

if [ -z "$MATCHES" ]; then
  echo -e "${GREEN}PASS${NC} No code-created UI patterns found."
  exit 0
fi

# Step 2: classify each match
VIOLATIONS=0
ALLOWED=0
echo "Scanning matches..."
echo ""

while IFS= read -r line; do
  FILE_LINE="${line%%:*}"  # extract filename:lineno
  REST="${line#*:}"
  LINE_NO="${REST%%:*}"
  FILE_PATH="$(echo "$line" | cut -d: -f1)"

  # Skip exempt directories
  SKIP=false
  for exempt_dir in "${EXEMPT_DIRS[@]}"; do
    if [[ "$FILE_PATH" == *"$exempt_dir"* ]]; then
      SKIP=true
      break
    fi
  done
  if [ "$SKIP" = true ]; then
    continue
  fi

  # Check if the line itself or the previous line has an exemption comment
  FULL_LINE_CONTENT=$(sed -n "${LINE_NO}p" "$FILE_PATH" 2>/dev/null || echo "")
  PREV_LINE_CONTENT=""
  if [ "$LINE_NO" -gt 1 ]; then
    PREV_LINE_CONTENT=$(sed -n "$((LINE_NO - 1))p" "$FILE_PATH" 2>/dev/null || echo "")
  fi
  HAS_EXEMPT=false
  for exempt_pattern in "${EXEMPT_LINES[@]}"; do
    if echo "$FULL_LINE_CONTENT" | grep -q "$exempt_pattern"; then
      HAS_EXEMPT=true
      break
    fi
    if [ -n "$PREV_LINE_CONTENT" ] && echo "$PREV_LINE_CONTENT" | grep -q "$exempt_pattern"; then
      HAS_EXEMPT=true
      break
    fi
  done
  if [ "$HAS_EXEMPT" = true ]; then
    ALLOWED=$((ALLOWED + 1))
    continue
  fi

  # Check if this is an allowed metadata pattern
  IS_ALLOWED_META=false
  for allowed in "${ALLOWED_PATTERNS[@]}"; do
    if echo "$FULL_LINE_CONTENT" | grep -q "$allowed"; then
      IS_ALLOWED_META=true
      break
    fi
  done
  if [ "$IS_ALLOWED_META" = true ]; then
    ALLOWED=$((ALLOWED + 1))
    echo -e "  ${YELLOW}ALLOWED (metadata)${NC} $FILE_PATH:$LINE_NO"
    continue
  fi

  # Violation
  VIOLATIONS=$((VIOLATIONS + 1))
  echo -e "  ${RED}VIOLATION${NC} $FILE_PATH:$LINE_NO"
  echo "    $(echo "$line" | cut -d: -f3-)"
done <<< "$MATCHES"

echo ""
if [ "$VIOLATIONS" -gt 0 ]; then
  echo -e "${RED}FAIL${NC} Found $VIOLATIONS code-created formal UI violation(s) without scene-first exemption."
  echo "These must be migrated to scene-backed PackedScene instances."
  echo "Allowed (metadata-only): $ALLOWED"
  exit 1
else
  echo -e "${GREEN}PASS${NC} No undocumented code-created formal UI found."
  echo "Allowed (metadata-only or exempt): $ALLOWED"
  exit 0
fi
