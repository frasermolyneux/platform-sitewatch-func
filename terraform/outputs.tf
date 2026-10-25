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
    hostname            = app.default_hostname
  }]
}

output "status_monitoring_contract" {
  description = "Read-only availability bindings for status consumers; deliberately excludes endpoint targets and credentials."
  value = {
    version  = 2
    producer = "frasermolyneux/platform-sitewatch-func"
    resources = merge(
      { for location in var.locations : "sitewatch-${location}" => azurerm_application_insights.ai[location].id },
      { for alias, resource in local.app_insights_map : alias => resource.id if alias != "default" }
    )
    checks = [for test in var.availability_tests : {
      site_id                   = test.site
      component_id              = coalesce(test.component, test.app)
      check_id                  = coalesce(test.check_id, test.component, test.app)
      expected_interval_seconds = test.interval_seconds
      expected_regions          = var.locations
      resource_aliases = test.app_insights == "default" || !contains(keys(local.app_insights_map), test.app_insights) ? [
        for location in var.locations : "sitewatch-${location}"
      ] : [test.app_insights]
    }]
  }
}
