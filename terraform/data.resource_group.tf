data "azurerm_resource_group" "rg" {
  for_each = local.workload_resource_groups

  name = each.value.name
}
