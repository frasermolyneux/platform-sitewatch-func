locals {
  sitewatch_resource_groups = {
    for location in var.locations :
    location => format("rg-platform-sitewatch-func-%s-%s", var.environment, lower(location))
  }

  app_insights_sampling_percentage = {
    dev = 25
    prd = 75
  }

  action_group_map = {
    0 = data.azurerm_monitor_action_group.critical
    1 = data.azurerm_monitor_action_group.high
    2 = data.azurerm_monitor_action_group.moderate
    3 = data.azurerm_monitor_action_group.low
    4 = data.azurerm_monitor_action_group.informational
  }

  app_insights_map = {
    "default"     = azurerm_application_insights.ai[var.locations[0]]
    "portal"      = data.azurerm_application_insights.portal
    "geolocation" = data.azurerm_application_insights.geolocation
  }
}
