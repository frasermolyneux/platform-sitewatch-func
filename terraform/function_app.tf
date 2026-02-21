resource "azurerm_linux_function_app" "app" {
  for_each = data.azurerm_resource_group.rg

  name = format("fn-platform-sitewatch-func-%s-%s-%s", var.environment, each.key, random_id.environment_location_id[each.key].hex)
  tags = var.tags

  resource_group_name = each.value.name
  location            = each.value.location

  service_plan_id = azurerm_service_plan.sp[each.key].id

  storage_account_name       = azurerm_storage_account.function_app_storage[each.key].name
  storage_account_access_key = azurerm_storage_account.function_app_storage[each.key].primary_access_key
  //storage_uses_managed_identity = false

  https_only                    = true
  public_network_access_enabled = true

  functions_extension_version = "~4"

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      use_dotnet_isolated_runtime = true
      dotnet_version              = "9.0"
    }

    application_insights_connection_string = azurerm_application_insights.ai[each.key].connection_string

    ftps_state          = "Disabled"
    always_on           = false // Not possible with consumption tier
    minimum_tls_version = "1.2"
  }

  app_settings = merge(
    {
      "REGION_NAME" = each.value.location

      "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"

      "default_appinsights_connection_string" = azurerm_application_insights.ai[var.locations[0]].connection_string
      "SiteWatch__Telemetry__default"         = azurerm_application_insights.ai[var.locations[0]].connection_string

      "test_config"                      = jsonencode(var.availability_tests)
      "SiteWatch__DisableExternalChecks" = tostring(var.disable_external_checks)

      "xtremeidiots_forums_task_key" = format("@Microsoft.KeyVault(VaultName=%s;SecretName=%s)", azurerm_key_vault.kv.name, azurerm_key_vault_secret.xtremeidiots_forums_task_key.name)

      // https://learn.microsoft.com/en-us/azure/azure-monitor/profiler/profiler-azure-functions#app-settings-for-enabling-profiler
      "APPINSIGHTS_PROFILERFEATURE_VERSION"  = "1.0.0"
      "DiagnosticServices_EXTENSION_VERSION" = "~3"
    },
    var.portal_app_insights == null ? {} : {
      "portal_appinsights_connection_string" = data.azurerm_application_insights.portal[0].connection_string
      "SiteWatch__Telemetry__portal"         = data.azurerm_application_insights.portal[0].connection_string
    },
    var.geolocation_app_insights == null ? {} : {
      "geolocation_appinsights_connection_string" = data.azurerm_application_insights.geolocation[0].connection_string
      "SiteWatch__Telemetry__geolocation"         = data.azurerm_application_insights.geolocation[0].connection_string
    }
  )

  lifecycle {
    ignore_changes = [
      app_settings["WEBSITE_ENABLE_SYNC_UPDATE_SITE"],
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
    ]
  }
}

resource "azurerm_role_assignment" "app_to_keyvault" {
  for_each = data.azurerm_resource_group.rg

  scope                = azurerm_key_vault.kv.id
  role_definition_name = "Key Vault Secrets User"
  principal_id         = azurerm_linux_function_app.app[each.key].identity[0].principal_id
}

resource "azurerm_role_assignment" "app_to_storage_blob" {
  for_each = data.azurerm_resource_group.rg

  scope                = azurerm_storage_account.function_app_storage[each.key].id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_linux_function_app.app[each.key].identity[0].principal_id
}

resource "azurerm_role_assignment" "app_to_storage_queue" {
  for_each = data.azurerm_resource_group.rg

  scope                = azurerm_storage_account.function_app_storage[each.key].id
  role_definition_name = "Storage Queue Data Contributor"
  principal_id         = azurerm_linux_function_app.app[each.key].identity[0].principal_id
}
