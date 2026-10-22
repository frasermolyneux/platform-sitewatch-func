locals {
  subscriptions = [for sub in var.subscriptions : {
    name            = sub.name
    subscription_id = sub.subscription_id
  }]
}

output "subscriptions" {
  value = local.subscriptions
}

output "log_analytics" {
  value = data.terraform_remote_state.platform_monitoring.outputs.log_analytics
}

output "key_vault" {
  value = {
    name                = azurerm_key_vault.kv.name
    id                  = azurerm_key_vault.kv.id
    resource_group_name = azurerm_key_vault.kv.resource_group_name
  }
}

output "func_apps" {
  value = [for app in azurerm_function_app_flex_consumption.app : {
    name                = app.name
    resource_group_name = app.resource_group_name
    location            = app.location
  }]
}
