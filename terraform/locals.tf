locals {
  sitewatch_resource_groups = {
    for location in var.locations :
    location => format("rg-platform-sitewatch-func-%s-%s", var.environment, lower(location))
  }
}
