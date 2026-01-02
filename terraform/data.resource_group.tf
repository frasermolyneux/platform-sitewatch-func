data "azurerm_resource_group" "rg" {
  for_each = locals.workload_resource_groups

  name = each.value
}
