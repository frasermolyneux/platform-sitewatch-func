resource "azurerm_monitor_metric_alert" "availability" {
  for_each = local.monitor_action_groups == null ? {} : {
    for each in var.availability_tests : each.app => each if contains(keys(local.action_group_map), tostring(each.severity))
  }

  name = "${each.value.workload}-${each.value.environment} - ${each.key} - availability"

  resource_group_name = local.sitewatch_resource_groups[var.locations[0]]
  scopes              = [local.app_insights_map[each.value.app_insights].id]

  description = "Availability test for ${each.key}"

  criteria {
    metric_namespace = "microsoft.insights/components"
    metric_name      = "availabilityResults/availabilityPercentage"
    aggregation      = "Average"
    operator         = "LessThan"
    threshold        = 95

    dimension {
      name     = "availabilityResult/name"
      operator = "StartsWith"
      values   = [each.key]
    }
  }

  severity = each.value.severity

  frequency   = "PT1M"
  window_size = "PT30M"

  action {
    action_group_id = local.action_group_map[tostring(each.value.severity)].id
  }

  tags = {
    "Workload"    = each.value.workload
    "Environment" = each.value.environment
  }
}
