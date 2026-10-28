#!/bin/bash
# Production deploy directory: /opt/zepp-step/
# Same-directory env file (do not commit) must define:
#   ZEPP_USER / ZEPP_PASS
#   optional ZEPP_STEP_MIN / ZEPP_STEP_MAX (defaults 100 / 400)
# cron example: 0 20 * * * /opt/zepp-step/run.sh
set -euo pipefail
DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$DIR"
set -a
# shellcheck disable=SC1091
source "$DIR/env"
set +a

OUT=""
RC=0
set +e
OUT="$(python3 "$DIR/bushu.py" 2>&1)"
RC=$?
set -e

# Strip anything that looks like a password assignment just in case
SAFE_OUT="$(printf '%s' "$OUT" | sed -E 's/(ZEPP_PASS|passwd|password)[=:][^[:space:]]+/\1=***/Ig')"

if [ "$RC" -eq 0 ]; then
  MSG="【Zepp刷步】成功
${SAFE_OUT}"
else
  MSG="【Zepp刷步】失败(退出码${RC})
${SAFE_OUT}"
fi

# Notify; do not fail the script solely on notify errors
qq-notify "$MSG" >/dev/null 2>&1 || true

printf '%s\n' "$SAFE_OUT"
exit "$RC"
