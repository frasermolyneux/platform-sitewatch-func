resource "azurerm_service_plan" "sp" {
  for_each = toset(var.locations)

  name                = format("asp-platform-sitewatch-func-%s-%s", var.environment, each.value)
  location            = each.value
  resource_group_name = local.sitewatch_resource_groups[each.value]

  os_type  = "Linux"
  sku_name = var.app_service_plan.sku
}
