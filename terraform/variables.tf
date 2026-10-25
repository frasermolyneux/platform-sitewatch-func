variable "environment" {
  default = "dev"
}

variable "workload_name" {
  description = "Name of the workload as defined in platform-workloads state"
  type        = string
  default     = "platform-sitewatch-func"
}

variable "locations" {
  type    = list(string)
  default = ["swedencentral", "eastus"]
}

variable "subscription_id" {}

variable "subscriptions" {
  type = map(object({
    name            = string
    subscription_id = string
  }))
}

variable "geolocation_app_insights" {
  type    = object({ subscription_id = string, resource_group_name = string, name = string })
  default = null
}

variable "portal_app_insights" {
  type    = object({ subscription_id = string, resource_group_name = string, name = string })
  default = null
}

variable "availability_tests" {
  type = list(object({
    workload         = string
    environment      = string
    app              = string
    app_insights     = string
    uri              = string
    severity         = string
    site             = string
    component        = optional(string)
    check_id         = optional(string)
    interval_seconds = optional(number, 30)
    method           = optional(string, "GET")
    retries          = optional(number, 3)
    follow_redirects = optional(bool, true)
    headers_only     = optional(bool, false)
  }))

  validation {
    condition = var.environment != "prd" || (
      sort(var.locations) == sort(jsondecode(file("${path.module}/../contract/status-monitoring-bindings.json")).regions)
      && alltrue([
        for reference in jsondecode(file("${path.module}/../contract/status-monitoring-bindings.json")).checks :
        length([for test in var.availability_tests : test if(
          test.site == reference.siteId && coalesce(test.component, test.app) == reference.componentId
          && coalesce(test.check_id, test.component, test.app) == reference.checkId
          && test.interval_seconds == reference.intervalSeconds
          && sort(test.app_insights == "default" ? [
            for location in var.locations : "sitewatch-${location}"
          ] : [test.app_insights]) == sort(reference.resources)
        )]) == 1
      ])
    )
    error_message = "Production monitoring must preserve every status-consumed identity, cadence, regional source and telemetry destination declared in the shared bindings contract."
  }
}

variable "platform_workloads_state" {
  description = "Backend config for platform-workloads remote state (used to read workload resource groups/backends)"
  type = object({
    resource_group_name  = string
    storage_account_name = string
    container_name       = string
    key                  = string
    subscription_id      = string
    tenant_id            = string
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

variable "disable_external_checks" {
  description = "When true, timer triggers will short-circuit and skip outbound availability checks."
  type        = bool
  default     = false
}
