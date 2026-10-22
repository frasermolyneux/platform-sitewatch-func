#!/usr/bin/env bash
set -euo pipefail

: "${FUNCTION_APP_HOSTNAME:?FUNCTION_APP_HOSTNAME is required}"
: "${FUNCTION_APP_NAME:?FUNCTION_APP_NAME is required}"
: "${FUNCTION_APP_RESOURCE_GROUP:?FUNCTION_APP_RESOURCE_GROUP is required}"
if [[ ! "$FUNCTION_APP_HOSTNAME" =~ ^([a-zA-Z0-9-]+\.)+azurewebsites\.net$ ]]; then
  echo "::error::Invalid Function App hostname."
  exit 1
fi

account_name=$(az functionapp config appsettings list \
  --name "$FUNCTION_APP_NAME" --resource-group "$FUNCTION_APP_RESOURCE_GROUP" \
  --query "[?name=='AzureWebJobsStorage__accountName'].value | [0]" \
  --output tsv --only-show-errors)
credential=$(az functionapp config appsettings list \
  --name "$FUNCTION_APP_NAME" --resource-group "$FUNCTION_APP_RESOURCE_GROUP" \
  --query "[?name=='AzureWebJobsStorage__credential'].value | [0]" \
  --output tsv --only-show-errors)
if [[ ! "$account_name" =~ ^[a-z0-9]{3,24}$ || "$credential" != managedidentity ]]; then
  echo "::error::Identity-based host storage must be configured before removing legacy storage settings."
  exit 1
fi

# AzureRM 5.9 injects a flat, empty-key connection string which overrides identity settings.
legacy_count=$(az functionapp config appsettings list \
  --name "$FUNCTION_APP_NAME" --resource-group "$FUNCTION_APP_RESOURCE_GROUP" \
  --query "length([?name=='AzureWebJobsStorage'])" --output tsv --only-show-errors)
if [[ "$legacy_count" != 0 && "$legacy_count" != 1 ]]; then
  echo "::error::Unexpected legacy host-storage configuration."
  exit 1
fi
if [[ "$legacy_count" == 1 ]]; then
  az functionapp config appsettings delete \
    --name "$FUNCTION_APP_NAME" --resource-group "$FUNCTION_APP_RESOURCE_GROUP" \
    --setting-names AzureWebJobsStorage --output none --only-show-errors
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
