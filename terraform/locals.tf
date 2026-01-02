locals {
  workload_resource_groups = {
    for location in var.locations :
    location => data.terraform_remote_state.platform_workloads.outputs.workload_resource_groups[var.workload_name][var.environment].resource_groups[lower(location)]
  }

  workload_backend = try(
    data.terraform_remote_state.platform_workloads.outputs.workload_terraform_backends[var.workload_name][var.environment],
    null
  )

  workload_administrative_unit = try(
    data.terraform_remote_state.platform_workloads.outputs.workload_administrative_units[var.workload_name][var.environment],
    null
  )

  severity_levels = {
    critical      = 0
    high          = 1
    moderate      = 2
    low           = 3
    informational = 4
  }

  app_insights_sampling_percentage = {
    dev = 25
    prd = 75
  }

  action_group_map = {
    critical      = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.critical
    high          = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.high
    moderate      = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.moderate
    low           = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.low
    informational = data.terraform_remote_state.platform_monitoring.outputs.monitor_action_groups.informational
  }

  app_insights_map = merge(
    {
      "default" = azurerm_application_insights.ai[var.locations[0]]
    },
    var.portal_app_insights == null ? {} : { "portal" = data.azurerm_application_insights.portal[0] },
    var.geolocation_app_insights == null ? {} : { "geolocation" = data.azurerm_application_insights.geolocation[0] }
  )
}
