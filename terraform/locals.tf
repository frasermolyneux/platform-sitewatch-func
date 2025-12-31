locals {
  sitewatch_resource_groups = {
    for location in var.locations :
    location => data.terraform_remote_state.platform_workloads.outputs.workload_resource_groups[var.workload_name][var.environment].resource_groups[lower(location)].name
  }

  app_insights_sampling_percentage = {
    dev = 25
    prd = 75
  }

  action_group_map = {
    0 = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.critical
    1 = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.high
    2 = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.moderate
    3 = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.low
    4 = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.informational
  }

  app_insights_map = {
    "default"     = azurerm_application_insights.ai[var.locations[0]]
    "portal"      = data.azurerm_application_insights.portal
    "geolocation" = data.azurerm_application_insights.geolocation
  }
}
