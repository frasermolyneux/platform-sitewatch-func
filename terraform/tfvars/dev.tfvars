environment   = "dev"
workload_name = "platform-sitewatch-func"
locations     = ["swedencentral", "westus2"]

subscription_id = "6cad03c1-9e98-4160-8ebe-64dd30f1bbc7"

subscriptions = {
  sub-enterprise-devtest-legacy = {
    name            = "sub-enterprise-devtest-legacy"
    subscription_id = "1b5b28ed-1365-4a48-b285-80f80a6aaa1b"
  },
  sub-visualstudio-enterprise = {
    name            = "sub-visualstudio-enterprise"
    subscription_id = "6cad03c1-9e98-4160-8ebe-64dd30f1bbc7"
  },
  sub-molyneux-me-dev = {
    name            = "sub-molyneux-me-dev"
    subscription_id = "ef3cc6c2-159e-4890-9193-13673dded835"
  }
}

geolocation_app_insights = null

portal_app_insights = null

availability_tests = [
  {
    workload     = "synthetic"
    environment  = "dev"
    app          = "google-availability"
    app_insights = "default"
    uri          = "https://www.google.co.uk/"
    severity     = "informational"
  },
  {
    workload     = "synthetic"
    environment  = "dev"
    app          = "microsoft-availability"
    app_insights = "default"
    uri          = "https://www.microsoft.com/"
    severity     = "informational"
  }
]

disable_external_checks = false

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
  resource_group_name  = "rg-tf-platform-monitoring-dev-uksouth-01"
  storage_account_name = "sa9d99036f14d5"
  container_name       = "tfstate"
  key                  = "terraform.tfstate"
  subscription_id      = "7760848c-794d-4a19-8cb2-52f71a21ac2b"
  tenant_id            = "e56a6947-bb9a-4a6e-846a-1f118d1c3a14"
}

tags = {
  Environment = "dev",
  Workload    = "platform-sitewatch-func",
  DeployedBy  = "GitHub-Terraform",
  Git         = "https://github.com/frasermolyneux/platform-sitewatch-func"
}
