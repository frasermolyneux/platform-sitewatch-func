resource "azurerm_service_plan" "flex" {
  # checkov:skip=CKV_AZURE_225:Scale-to-zero probes use independent production regions instead of billed zone-redundant always-ready instances; see README Hosting.
  for_each = data.azurerm_resource_group.rg

  name                = "asp-platform-sitewatch-${var.environment}-${each.key}-flex"
  resource_group_name = each.value.name
  location            = each.value.location
  os_type             = "Linux"
  sku_name            = "FC1"
  tags                = var.tags
}

resource "azurerm_storage_container" "flex_deployments" {
  for_each = data.azurerm_resource_group.rg

  name                  = "sitewatch-flex-deployments"
  storage_account_id    = azurerm_storage_account.function_app_storage[each.key].id
  container_access_type = "private"
}

resource "azurerm_function_app_flex_consumption" "app" {
  for_each = data.azurerm_resource_group.rg

  name                = "fn-platform-sitewatch-${var.environment}-${each.key}-${random_id.environment_location_id[each.key].hex}-flex"
  resource_group_name = each.value.name
  location            = each.value.location
  service_plan_id     = azurerm_service_plan.flex[each.key].id
  tags                = var.tags

  https_only                    = true
  public_network_access_enabled = true

  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${azurerm_storage_account.function_app_storage[each.key].primary_blob_endpoint}${azurerm_storage_container.flex_deployments[each.key].name}"
  storage_authentication_type = "SystemAssignedIdentity"

  runtime_name           = "dotnet-isolated"
  runtime_version        = "10.0"
  instance_memory_in_mb  = 512
  maximum_instance_count = 1

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.ai[each.key].connection_string
  }

  app_settings = merge(
    {
      "REGION_NAME"                      = each.value.location
      "AzureWebJobsStorage__accountName" = azurerm_storage_account.function_app_storage[each.key].name
      "AzureWebJobsStorage__credential"  = "managedidentity"
      "test_config"                      = jsonencode(var.availability_tests)
      "SiteWatch__DisableExternalChecks" = tostring(var.disable_external_checks)
      "xtremeidiots_forums_task_key"     = format("@Microsoft.KeyVault(VaultName=%s;SecretName=%s)", azurerm_key_vault.kv.name, azurerm_key_vault_secret.xtremeidiots_forums_task_key.name)
    },
    var.portal_app_insights == null ? {} : {
      "SiteWatch__Telemetry__portal" = data.azurerm_application_insights.portal[0].connection_string
    },
    var.geolocation_app_insights == null ? {} : {
      "SiteWatch__Telemetry__geolocation" = data.azurerm_application_insights.geolocation[0].connection_string
    }
  )
}

resource "azurerm_role_assignment" "flex_app_to_keyvault" {
  for_each = data.azurerm_resource_group.rg

  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_function_app_flex_consumption.app[each.key].identity[0].principal_id
}

resource "azurerm_role_assignment" "flex_app_to_storage_blob" {
  for_each = data.azurerm_resource_group.rg

  scope                = azurerm_storage_account.function_app_storage[each.key].id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = azurerm_function_app_flex_consumption.app[each.key].identity[0].principal_id
}

resource "azurerm_role_assignment" "flex_app_to_storage_queue" {
  for_each = data.azurerm_resource_group.rg

  scope                = azurerm_storage_account.function_app_storage[each.key].id
  role_definition_name = "Storage Queue Data Contributor"
  principal_id         = azurerm_function_app_flex_consumption.app[each.key].identity[0].principal_id
}
