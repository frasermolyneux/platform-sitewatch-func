resource "azurerm_user_assigned_identity" "app" {
  for_each = toset(var.locations)

  name                = format("id-platform-sitewatch-func-%s-%s", var.environment, lower(each.value))
  location            = each.value
  resource_group_name = local.sitewatch_resource_groups[each.value]
  tags                = var.tags
}

resource "azurerm_linux_function_app" "app" {
  for_each = toset(var.locations)

  name = format("fn-platform-sitewatch-func-%s-%s-%s", var.environment, each.value, random_id.environment_location_id[each.value].hex)
  tags = var.tags

  resource_group_name = local.sitewatch_resource_groups[each.value]
  location            = each.value

  service_plan_id = azurerm_service_plan.sp[each.value].id

  storage_account_name       = azurerm_storage_account.function_app_storage[each.value].name
  storage_account_access_key = azurerm_storage_account.function_app_storage[each.value].primary_access_key

  https_only                    = true
  public_network_access_enabled = false

  functions_extension_version = "~4"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app[each.value].id]
  }

  site_config {
    application_stack {
      use_dotnet_isolated_runtime = true
      dotnet_version              = "10.0"
    }

    application_insights_connection_string = azurerm_application_insights.ai[each.value].connection_string
    application_insights_key               = azurerm_application_insights.ai[each.value].instrumentation_key

    ftps_state          = "Disabled"
    always_on           = false
    minimum_tls_version = "1.2"
  }

  app_settings = {
    "ApplicationInsightsAgent_EXTENSION_VERSION" = "~3"

    "default_appinsights_connection_string"     = azurerm_application_insights.ai[var.locations[0]].connection_string
    "portal_appinsights_connection_string"      = data.azurerm_application_insights.portal.connection_string
    "geolocation_appinsights_connection_string" = data.azurerm_application_insights.geolocation.connection_string

    "AZURE_CLIENT_ID" = azurerm_user_assigned_identity.app[each.value].client_id

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
  principal_id         = azurerm_user_assigned_identity.app[each.value].principal_id
}
