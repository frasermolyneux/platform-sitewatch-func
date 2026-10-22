#!/usr/bin/env bash
set -euo pipefail

: "${FUNCTION_APP_NAME:?FUNCTION_APP_NAME is required}"
: "${FUNCTION_APP_RESOURCE_GROUP:?FUNCTION_APP_RESOURCE_GROUP is required}"

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
