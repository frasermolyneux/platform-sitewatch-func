locals {
  sitewatch_resource_groups = {
    for location in var.locations :
    location => format("rg-platform-sitewatch-func-%s-%s", var.environment, lower(location))
  }

  app_insights_sampling_percentage = {
    dev = 25
    prd = 75
  }
}
