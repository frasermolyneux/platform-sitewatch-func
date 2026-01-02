resource "azurerm_storage_account" "function_app_storage" {
  for_each = data.azurerm_resource_group.rg

  name                = "safn${random_id.environment_location_id[each.key].hex}"
  resource_group_name = each.value.name
  location            = each.value.location

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
