data "azurerm_client_config" "current" {}

data "azuread_client_config" "current" {}

resource "random_id" "environment_id" {
  byte_length = 6
}

resource "random_id" "environment_location_id" {
  for_each = data.azurerm_resource_group.rg

  byte_length = 6
}
