resource "azurerm_storage_account" "function_app_storage" {
  for_each = toset(var.locations)

  name                = "safn${random_id.environment_location_id[each.value].hex}"
  resource_group_name = local.sitewatch_resource_groups[each.value]
  location            = each.value

  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  access_tier              = "Hot"

  https_traffic_only_enabled = true
  min_tls_version            = "TLS1_2"

  allow_nested_items_to_be_public = false

  local_user_enabled        = true
  shared_access_key_enabled = true

  identity {
    type = "SystemAssigned"
  }

  tags = var.tags
}

resource "azurerm_storage_container" "function_app_container" {
  for_each = toset(var.locations)

  name                  = "app-package"
  storage_account_id    = azurerm_storage_account.function_app_storage[each.value].id
  container_access_type = "private"
}
