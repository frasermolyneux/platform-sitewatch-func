locals {
  sitewatch_resource_groups = {
    for location in var.locations :
    location => format("rg-platform-sitewatch-func-%s-%s", var.environment, lower(location))
  }
}

resource "azurerm_application_insights" "ai" {
  for_each = toset(var.locations)

  name                = format("ai-platform-sitewatch-func-%s-%s", var.environment, each.value)
  location            = each.value
  resource_group_name = local.sitewatch_resource_groups[each.value]
  workspace_id        = data.terraform_remote_state.platform_monitoring.outputs.log_analytics.workspace_id

  application_type   = "web"
  disable_ip_masking = true
}
