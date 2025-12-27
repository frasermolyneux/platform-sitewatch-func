variable "environment" {
  default = "dev"
}

variable "locations" {
  type    = list(string)
  default = ["uksouth", "eastus"]
}

variable "subscription_id" {}

variable "subscriptions" {
  type = map(object({
    name            = string
    subscription_id = string
  }))
}

variable "geolocation_app_insights" {
  type = object({
    subscription_id     = string
    resource_group_name = string
    name                = string
  })
}

variable "portal_app_insights" {
  type = object({
    subscription_id     = string
    resource_group_name = string
    name                = string
  })
}

variable "availability_tests" {
  type = list(object({
    workload     = string
    environment  = string
    app          = string
    app_insights = string
    uri          = string
    severity     = number
  }))
}

variable "app_service_plan" {
  type = object({
    sku = string
  })
}

variable "platform_monitoring_state" {
  description = "Backend config for platform-monitoring remote state"
  type = object({
    resource_group_name  = string
    storage_account_name = string
    container_name       = string
    key                  = string
    subscription_id      = string
    tenant_id            = string
  })
}

variable "tags" {
  default = {}
}
