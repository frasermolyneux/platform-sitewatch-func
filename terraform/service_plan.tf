resource "azurerm_service_plan" "sp" {
  for_each = data.azurerm_resource_group.rg

  name                = format("asp-platform-sitewatch-func-%s-%s", var.environment, each.key)
  location            = each.value.location
  resource_group_name = each.value.name

  os_type  = "Linux"
  sku_name = var.app_service_plan.sku
}
