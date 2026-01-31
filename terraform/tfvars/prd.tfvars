environment   = "prd"
workload_name = "platform-sitewatch-func"
locations     = ["uksouth", "eastus", "westeurope"]

subscription_id = "7760848c-794d-4a19-8cb2-52f71a21ac2b"

subscriptions = {
  sub-fm-geolocation-prd = {
    name            = "sub-fm-geolocation-prd"
    subscription_id = "d3b204ab-7c2b-47f7-8d5a-de19e85591e7"
  },
  sub-mx-consulting-prd = {
    name            = "sub-mx-consulting-prd"
    subscription_id = "655da25d-da46-40c0-8e81-5debe2dcd024"
  },
  sub-platform-connectivity = {
    name            = "sub-platform-connectivity"
    subscription_id = "db34f572-8b71-40d6-8f99-f29a27612144"
  },
  sub-platform-identity = {
    name            = "sub-platform-identity"
    subscription_id = "c391a150-f992-41a6-bc81-ebc22bc64376"
  },
  sub-platform-management = {
    name            = "sub-platform-management"
    subscription_id = "7760848c-794d-4a19-8cb2-52f71a21ac2b"
  },
  sub-platform-strategic = {
    name            = "sub-platform-strategic"
    subscription_id = "903b6685-c12a-4703-ac54-7ec1ff15ca43"
  },
  sub-talkwithtiles-prd = {
    name            = "sub-talkwithtiles-prd"
    subscription_id = "e1e5de62-3573-4b44-a52b-0f1431675929"
  },
  sub-xi-demomanager-prd = {
    name            = "sub-xi-demomanager-prd"
    subscription_id = "845766d6-b73f-49aa-a9f6-eaf27e20b7a8"
  },
  sub-xi-portal-prd = {
    name            = "sub-xi-portal-prd"
    subscription_id = "32444f38-32f4-409f-889c-8e8aa2b5b4d1"
  },
  sub-finances-prd = {
    name            = "sub-finances-prd"
    subscription_id = "957a7d34-8562-4098-bb4c-072e08386d07"
  },
  sub-molyneux-me-prd = {
    name            = "sub-molyneux-me-prd"
    subscription_id = "3cc59319-eb1e-4b52-b19e-09a49f9db2e7"
  }
}

geolocation_app_insights = {
  subscription_id     = "d3b204ab-7c2b-47f7-8d5a-de19e85591e7"
  resource_group_name = "rg-geolocation-prd-uksouth-01"
  name                = "ai-geolocation-prd-uksouth-01"
}

portal_app_insights = {
  subscription_id     = "32444f38-32f4-409f-889c-8e8aa2b5b4d1"
  resource_group_name = "rg-portal-core-prd-uksouth"
  name                = "ai-portal-core-prd-uksouth"
}

availability_tests = [
  {
    workload     = "portal-event-ingest"
    environment  = "prd"
    app          = "fn-portal-event-ingest-prd-uksouth-3a23e3c6dc68"
    app_insights = "portal"
    uri          = "https://fn-portal-event-ingest-prd-uksouth-3a23e3c6dc68.azurewebsites.net/api/health"
    severity     = "critical"
  },
  {
    workload     = "portal-repository-v1"
    environment  = "prd"
    app          = "app-portal-repo-prd-uksouth-v1-655b79d54d89"
    app_insights = "portal"
    uri          = "https://app-portal-repo-prd-uksouth-v1-655b79d54d89.azurewebsites.net/api/health"
    severity     = "critical"
  },
  {
    workload     = "portal-repository-v2"
    environment  = "prd"
    app          = "app-portal-repo-prd-uksouth-v2-655b79d54d89"
    app_insights = "portal"
    uri          = "https://app-portal-repo-prd-uksouth-v2-655b79d54d89.azurewebsites.net/api/health"
    severity     = "critical"
  },
  {
    workload     = "portal-repository-func"
    environment  = "prd"
    app          = "fn-portal-repository-func-prd-uksouth-ad4109a900bb"
    app_insights = "portal"
    uri          = "https://fn-portal-repository-func-prd-uksouth-ad4109a900bb.azurewebsites.net/api/health"
    severity     = "high"
  },
  {
    workload     = "portal-servers-integration"
    environment  = "prd"
    app          = "app-portal-servers-int-prd-uksouth-01-bxxyivgotrxya"
    app_insights = "portal"
    uri          = "https://app-portal-servers-int-prd-uksouth-01-bxxyivgotrxya.azurewebsites.net/api/health"
    severity     = "high"
  },
  {
    workload     = "portal-sync"
    environment  = "prd"
    app          = "fn-portal-sync-func-prd-uksouth-50d011333089"
    app_insights = "portal"
    uri          = "https://fn-portal-sync-func-prd-uksouth-50d011333089.azurewebsites.net/api/health"
    severity     = "critical"
  },
  {
    workload     = "portal-web"
    environment  = "prd"
    app          = "app-portal-web-prd-uksouth-fe584e15256a"
    app_insights = "portal"
    uri          = "https://app-portal-web-prd-uksouth-fe584e15256a.azurewebsites.net/api/health"
    severity     = "critical"
  },
  {
    workload     = "geolocation"
    environment  = "prd"
    app          = "app-geolocation-api-prd-uksouth-01-queggvl6v5yta"
    app_insights = "geolocation"
    uri          = "https://app-geolocation-api-prd-uksouth-01-queggvl6v5yta.azurewebsites.net/api/health"
    severity     = "high"
  },
  {
    workload     = "geolocation"
    environment  = "prd"
    app          = "app-geolocation-web-prd-uksouth-01-ndrrqvrn34qke"
    app_insights = "geolocation"
    uri          = "https://app-geolocation-web-prd-uksouth-01-ndrrqvrn34qke.azurewebsites.net/api/health"
    severity     = "high"
  },
  {
    workload     = "xtremeidiots-forums"
    environment  = "prd"
    app          = "www.xtremeidiots.com-web"
    app_insights = "default"
    uri          = "https://www.xtremeidiots.com"
    severity     = "critical"
  },
  {
    workload     = "xtremeidiots-forums"
    environment  = "prd"
    app          = "www.xtremeidiots.com-tasks"
    app_insights = "default"
    uri          = "https://www.xtremeidiots.com/applications/core/interface/task/web.php?key=%xtremeidiots_forums_task_key%"
    severity     = "high"
  },
  {
    workload     = "xtremeidiots-redirect"
    environment  = "prd"
    app          = "redirect.xtremeidiots.net"
    app_insights = "default"
    uri          = "https://redirect.xtremeidiots.net"
    severity     = "high"
  },
  {
    workload     = "bishopsbees"
    environment  = "prd"
    app          = "bishopbees.co.uk"
    app_insights = "default"
    uri          = "https://bishopsbees.co.uk"
    severity     = "critical"
  },
  {
    workload     = "molyneux-me"
    environment  = "prd"
    app          = "molyneux.me"
    app_insights = "default"
    uri          = "https://www.molyneux.me/"
    severity     = "low"
  }
]

app_service_plan = {
  sku = "Y1"
}

platform_workloads_state = {
  resource_group_name  = "rg-tf-platform-workloads-prd-uksouth-01"
  storage_account_name = "sadz9ita659lj9xb3"
  container_name       = "tfstate"
  key                  = "terraform.tfstate"
  subscription_id      = "7760848c-794d-4a19-8cb2-52f71a21ac2b"
  tenant_id            = "e56a6947-bb9a-4a6e-846a-1f118d1c3a14"
}

platform_monitoring_state = {
  resource_group_name  = "rg-tf-platform-monitoring-prd-uksouth-01"
  storage_account_name = "sa74f04c5f984e"
  container_name       = "tfstate"
  key                  = "terraform.tfstate"
  subscription_id      = "7760848c-794d-4a19-8cb2-52f71a21ac2b"
  tenant_id            = "e56a6947-bb9a-4a6e-846a-1f118d1c3a14"
}

tags = {
  Environment = "prd",
  Workload    = "platform-sitewatch-func",
  DeployedBy  = "GitHub-Terraform",
  Git         = "https://github.com/frasermolyneux/platform-sitewatch-func"
}
