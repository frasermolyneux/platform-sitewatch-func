resource "azurerm_application_insights" "ai" {
  for_each = toset(var.locations)

  name = format("ai-platform-sitewatch-func-%s-%s", var.environment, each.value)

  location            = each.value
  resource_group_name = local.sitewatch_resource_groups[each.value]

  workspace_id = data.terraform_remote_state.platform_monitoring.outputs.log_analytics.id

  application_type   = "web"
  disable_ip_masking = true

  daily_data_cap_in_gb = 1
  retention_in_days    = 30
  sampling_percentage  = lookup(local.app_insights_sampling_percentage, var.environment, 25)

  tags = var.tags
}
