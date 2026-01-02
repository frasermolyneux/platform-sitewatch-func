resource "azurerm_application_insights" "ai" {
  for_each = data.azurerm_resource_group.rg

  name = format("ai-platform-sitewatch-func-%s-%s", var.environment, each.key)

  location            = each.value.location
  resource_group_name = each.value.name

  workspace_id = local.platform_monitoring_workspace_id

  application_type   = "web"
  disable_ip_masking = true

  daily_data_cap_in_gb = 1
  retention_in_days    = 30
  sampling_percentage  = lookup(local.app_insights_sampling_percentage, var.environment, 25)

  tags = var.tags
}
