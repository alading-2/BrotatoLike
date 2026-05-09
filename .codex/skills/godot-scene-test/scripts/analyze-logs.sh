#!/usr/bin/env bash
set -euo pipefail

# analyze-logs.sh — 从最新测试日志中提取关键错误
# 用法: ./analyze-logs.sh [--date YYYY-MM-DD] [--run HH-MM-SS] [--module <name>]

LOG_BASE="${LOG_BASE:-.ai-temp/scene-tests/runs}"
DATE="${1:-$(ls -1t "$LOG_BASE" 2>/dev/null | head -1)}"

if [[ -z "$DATE" || ! -d "$LOG_BASE/$DATE" ]]; then
    echo "未找到日志目录: $LOG_BASE/$DATE"
    echo "可用日期:"
    ls -1t "$LOG_BASE" 2>/dev/null | head -10 || echo "  (无)"
    exit 1
fi

DATE_DIR="$LOG_BASE/$DATE"

# 找最新一次运行
RUN=$(ls -1t "$DATE_DIR" 2>/dev/null | head -1)
if [[ -z "$RUN" || ! -d "$DATE_DIR/$RUN" ]]; then
    echo "日期 $DATE 下无运行记录"
    exit 1
fi

RUN_DIR="$DATE_DIR/$RUN"
INDEX="$RUN_DIR/index.json"

echo "=== 测试运行: $DATE/$RUN ==="
echo ""

if [[ -f "$INDEX" ]]; then
    # 用 python3 解析 JSON（比 grep 可靠）
    python3 -c "
import json, sys
try:
    with open('$INDEX') as f:
        data = json.load(f)
    s = data.get('summary', {})
    print(f\"总场景: {s.get('totalScenes', '?')}  已执行: {s.get('executedScenes', '?')}  跳过: {s.get('skippedScenes', '?')}\")
    print(f\"通过: {s.get('passedScenes', '?')}  失败: {s.get('failedScenes', '?')}  超时: {s.get('timedOutScenes', '?')}\")
    print(f\"总重试: {s.get('attemptsUsed', '?')}\")
    print()
    print('--- 场景详情 ---')
    for entry in data.get('entries', []):
        status = entry.get('status', 'unknown')
        scene = entry.get('scene', '?')
        reason = entry.get('failureReason', '')
        first_err = entry.get('firstError', '')
        if status == 'passed':
            print(f'  [PASS] {scene}')
        elif status == 'failed':
            print(f'  [FAIL] {scene}')
            if reason:
                print(f'         原因: {reason}')
            if first_err:
                print(f'         首个错误: {first_err[:120]}')
        elif status == 'timed_out':
            print(f'  [TIMEOUT] {scene}')
except Exception as e:
    print(f'解析 index.json 失败: {e}', file=sys.stderr)
    sys.exit(1)
" 2>&1
else
    echo "未找到 index.json，回退到 grep..."
    echo ""
    echo "=== 错误汇总 ==="
    rg -n -m 80 -C 2 "ERROR:|\\[ERROR\\]|\\[FAIL\\]|Exception|Cannot instantiate|C# Script Error|Unhandled exception" "$RUN_DIR" 2>/dev/null || echo "  (无匹配错误)"
fi

echo ""
echo "=== 日志目录: $RUN_DIR ==="
echo "  保留供复查，分析完成后可删除:"
echo "  rm -r -- $RUN_DIR"
