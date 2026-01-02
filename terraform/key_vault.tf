resource "azurerm_key_vault" "kv" {
  name                = format("kv-%s-%s", random_id.environment_id.hex, var.locations[0])
  location            = var.locations[0]
  resource_group_name = data.azurerm_resource_group.rg[var.locations[0]].name
  tenant_id           = data.azurerm_client_config.current.tenant_id

  tags = var.tags

  soft_delete_retention_days = 90
  purge_protection_enabled   = true
  rbac_authorization_enabled = true

  sku_name = "standard"

  network_acls {
    bypass         = "AzureServices"
    default_action = "Allow"
  }
}

resource "azurerm_key_vault_secret" "xtremeidiots_forums_task_key" {
  name         = "xtremeidiots-forums-task-key"
  value        = "placeholder"
  key_vault_id = azurerm_key_vault.kv.id

  lifecycle {
    ignore_changes = [value]
  }
}
