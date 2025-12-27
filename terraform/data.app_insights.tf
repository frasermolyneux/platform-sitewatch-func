provider "azurerm" {
  alias           = "geolocation"
  subscription_id = var.geolocation_app_insights.subscription_id
  features {}
  storage_use_azuread = true
}

provider "azurerm" {
  alias           = "portal"
  subscription_id = var.portal_app_insights.subscription_id
  features {}
  storage_use_azuread = true
}

data "azurerm_application_insights" "geolocation" {
  provider = azurerm.geolocation

  name                = var.geolocation_app_insights.name
  resource_group_name = var.geolocation_app_insights.resource_group_name
}

data "azurerm_application_insights" "portal" {
  provider = azurerm.portal

  name                = var.portal_app_insights.name
  resource_group_name = var.portal_app_insights.resource_group_name
}
