#!/usr/bin/env bash
set -euo pipefail

: "${FUNCTION_APP_HOSTNAME:?FUNCTION_APP_HOSTNAME is required}"
if [[ ! "$FUNCTION_APP_HOSTNAME" =~ ^([a-zA-Z0-9-]+\.)+azurewebsites\.net$ ]]; then
  echo "::error::Invalid Function App hostname."
  exit 1
fi

deadline=$((SECONDS + 300))
for endpoint in live ready; do
  while true; do
    remaining=$((deadline - SECONDS))
    if (( remaining <= 0 )); then
      echo "::error::Function App did not become live and ready within five minutes."
      exit 1
    fi

    status=$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
      --max-time "$((remaining < 20 ? remaining : 20))" \
      "https://${FUNCTION_APP_HOSTNAME}/api/health/${endpoint}") || status=000
    echo "Function ${endpoint} endpoint returned HTTP ${status}."
    if [[ "$status" == 200 ]]; then
      break
    fi

    remaining=$((deadline - SECONDS))
    if (( remaining > 0 )); then
      sleep "$((remaining < 10 ? remaining : 10))"
    fi
  done
done
