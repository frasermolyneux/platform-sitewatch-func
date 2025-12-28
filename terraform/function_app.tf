resource "azurerm_function_app_flex_consumption" "app" {
  for_each = toset(var.locations)

  name = format("fn-platform-sitewatch-func-%s-%s-%s", var.environment, each.value, random_id.environment_location_id[each.value].hex)
  tags = var.tags

  resource_group_name = local.sitewatch_resource_groups[each.value]
  location            = each.value

  service_plan_id = azurerm_service_plan.sp[each.value].id

  storage_container_type      = "blobContainer"
  storage_container_endpoint  = "${azurerm_storage_account.function_app_storage[each.value].primary_blob_endpoint}${azurerm_storage_container.function_app_container[each.value].name}"
  storage_authentication_type = "SystemAssignedIdentity"

  runtime_name           = "dotnet-isolated"
  runtime_version        = "10.0"
  maximum_instance_count = 40

  https_only                    = true
  public_network_access_enabled = false

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_insights_connection_string = azurerm_application_insights.ai[each.value].connection_string
    application_insights_key               = azurerm_application_insights.ai[each.value].instrumentation_key

    minimum_tls_version = "1.2"
  }

  app_settings = {
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"

    "default_appinsights_connection_string"     = azurerm_application_insights.ai[var.locations[0]].connection_string
    "portal_appinsights_connection_string"      = data.azurerm_application_insights.portal.connection_string
    "geolocation_appinsights_connection_string" = data.azurerm_application_insights.geolocation.connection_string

    "test_config" = jsonencode(var.availability_tests)

    "xtremeidiots_forums_task_key" = format("@Microsoft.KeyVault(VaultName=%s;SecretName=%s)", azurerm_key_vault.kv.name, azurerm_key_vault_secret.xtremeidiots_forums_task_key.name)

    "APPINSIGHTS_PROFILERFEATURE_VERSION"  = "1.0.0"
    "DiagnosticServices_EXTENSION_VERSION" = "~3"
  }
}

resource "azurerm_role_assignment" "app_to_keyvault" {
  for_each = toset(var.locations)

  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_function_app_flex_consumption.app[each.value].identity[0].principal_id
}

resource "azurerm_role_assignment" "app_to_storage_blob" {
  for_each = toset(var.locations)

  scope                = azurerm_storage_account.function_app_storage[each.value].id
  role_definition_name = "Storage Blob Data Owner"
  principal_id         = azurerm_function_app_flex_consumption.app[each.value].identity[0].principal_id
}
