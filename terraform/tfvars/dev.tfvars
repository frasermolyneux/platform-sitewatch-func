environment = "dev"
locations   = ["uksouth", "eastus"]

subscription_id = "d68448b0-9947-46d7-8771-baa331a3063a"

subscriptions = {
  sub-enterprise-devtest-legacy = {
    name            = "sub-enterprise-devtest-legacy"
    subscription_id = "1b5b28ed-1365-4a48-b285-80f80a6aaa1b"
  },
  sub-visualstudio-enterprise = {
    name            = "sub-visualstudio-enterprise"
    subscription_id = "d68448b0-9947-46d7-8771-baa331a3063a"
  },
  sub-molyneux-me-dev = {
    name            = "sub-molyneux-me-dev"
    subscription_id = "ef3cc6c2-159e-4890-9193-13673dded835"
  }
}

geolocation_app_insights = {
  subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
  resource_group_name = "rg-geolocation-dev-uksouth-01"
  name                = "ai-geolocation-dev-uksouth-01"
}

portal_app_insights = {
  subscription_id     = "d68448b0-9947-46d7-8771-baa331a3063a"
  resource_group_name = "rg-portal-core-dev-uksouth-01"
  name                = "ai-portal-core-dev-uksouth-01"
}

availability_tests = [
  {
    workload     = "portal-event-ingest"
    environment  = "dev"
    app          = "fn-portal-event-ingest-dev-uksouth-01-fafcb30ca7e0"
    app_insights = "portal"
    uri          = "https://fn-portal-event-ingest-dev-uksouth-01-fafcb30ca7e0.azurewebsites.net/api/health"
    severity     = 4
  },
  {
    workload     = "portal-repository-v1"
    environment  = "dev"
    app          = "app-portal-repo-dev-uksouth-v1-ebd9159c6051"
    app_insights = "portal"
    uri          = "https://app-portal-repo-dev-uksouth-v1-ebd9159c6051.azurewebsites.net/api/health"
    severity     = 4
  },
  {
    workload     = "portal-repository-v2"
    environment  = "dev"
    app          = "app-portal-repo-dev-uksouth-v2-ebd9159c6051"
    app_insights = "portal"
    uri          = "https://app-portal-repo-dev-uksouth-v2-ebd9159c6051.azurewebsites.net/api/health"
    severity     = 4
  },
  {
    workload     = "portal-repository-func"
    environment  = "dev"
    app          = "fn-portal-repo-func-dev-uksouth-01-be9e6fe6e9c7"
    app_insights = "portal"
    uri          = "https://fn-portal-repo-func-dev-uksouth-01-be9e6fe6e9c7.azurewebsites.net/api/health"
    severity     = 4
  },
  {
    workload     = "portal-servers-integration"
    environment  = "dev"
    app          = "app-portal-servers-int-dev-uksouth-01-32s5yslgz4hea"
    app_insights = "portal"
    uri          = "https://app-portal-servers-int-dev-uksouth-01-32s5yslgz4hea.azurewebsites.net/api/health"
    severity     = 4
  },
  {
    workload     = "portal-sync"
    environment  = "dev"
    app          = "fn-portal-sync-dev-uksouth-01-f65d076b94fb"
    app_insights = "portal"
    uri          = "https://fn-portal-sync-dev-uksouth-01-f65d076b94fb.azurewebsites.net/api/health"
    severity     = 4
  },
  {
    workload     = "geolocation"
    environment  = "dev"
    app          = "app-geolocation-api-dev-uksouth-01-3omiauqb7et4w"
    app_insights = "geolocation"
    uri          = "https://app-geolocation-api-dev-uksouth-01-3omiauqb7et4w.azurewebsites.net/api/health"
    severity     = 4
  },
  {
    workload     = "geolocation"
    environment  = "dev"
    app          = "app-geolocation-web-dev-uksouth-01-tzcaho2oarnae"
    app_insights = "geolocation"
    uri          = "https://app-geolocation-web-dev-uksouth-01-tzcaho2oarnae.azurewebsites.net/api/health"
    severity     = 4
  },
  {
    workload     = "molyneux-me"
    environment  = "dev"
    app          = "dev.molyneux.me"
    app_insights = "default"
    uri          = "https://dev.molyneux.me/"
    severity     = 4
  }
]

app_service_plan = {
  sku = "Y1"
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
