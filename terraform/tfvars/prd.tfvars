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
  sub-platform-shared = {
    name            = "sub-platform-shared"
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
  subscription_id     = "903b6685-c12a-4703-ac54-7ec1ff15ca43"
  resource_group_name = "rg-geo-location-prd-swedencentral"
  name                = "ai-geo-location-prd-swedencentral"
}

portal_app_insights = {
  subscription_id     = "32444f38-32f4-409f-889c-8e8aa2b5b4d1"
  resource_group_name = "rg-portal-core-prd-uksouth"
  name                = "ai-portal-core-prd-uksouth"
}

availability_tests = [
  {
    workload     = "portal-repository-v1"
    environment  = "prd"
    app          = "app-portal-repo-prd-uksouth-v1-655b79d54d89"
    app_insights = "portal"
    uri          = "https://app-portal-repo-prd-uksouth-v1-655b79d54d89.azurewebsites.net/v1.0/health/live"
    severity     = "critical"
    site         = "xi"
    component    = "xi.sitewatch.repository-api-v1"
  },
  {
    workload     = "portal-repository-v2"
    environment  = "prd"
    app          = "app-portal-repo-prd-uksouth-v2-655b79d54d89"
    app_insights = "portal"
    uri          = "https://app-portal-repo-prd-uksouth-v2-655b79d54d89.azurewebsites.net/v2.0/health/live"
    severity     = "critical"
    site         = "xi"
    component    = "xi.sitewatch.repository-api-v2"
  },
  {
    workload     = "portal-repository-func"
    environment  = "prd"
    app          = "fn-portal-repository-func-prd-uksouth-ad4109a900bb"
    app_insights = "portal"
    uri          = "https://fn-portal-repository-func-prd-uksouth-ad4109a900bb.azurewebsites.net/api/health/live"
    severity     = "high"
    site         = "xi"
    component    = "xi.sitewatch.repository-func"
  },
  {
    workload     = "portal-servers-integration"
    environment  = "prd"
    app          = "app-portal-servers-int-prd-uksouth-v1-66febf7b1b9d"
    app_insights = "portal"
    uri          = "https://app-portal-servers-int-prd-uksouth-v1-66febf7b1b9d.azurewebsites.net/v1.0/health/live"
    severity     = "high"
    site         = "xi"
    component    = "xi.sitewatch.servers-integration-api"
  },
  {
    workload     = "portal-sync"
    environment  = "prd"
    app          = "fn-portal-sync-func-prd-uksouth-50d011333089"
    app_insights = "portal"
    uri          = "https://fn-portal-sync-func-prd-uksouth-50d011333089.azurewebsites.net/api/health/live"
    severity     = "critical"
    site         = "xi"
    component    = "xi.sitewatch.portal-sync"
  },
  {
    workload     = "portal-web"
    environment  = "prd"
    app          = "app-portal-web-prd-uksouth-fe584e15256a"
    app_insights = "portal"
    uri          = "https://app-portal-web-prd-uksouth-fe584e15256a.azurewebsites.net/api/health/live"
    severity     = "critical"
    site         = "xi"
    component    = "xi.sitewatch.portal-web"
  },
  {
    workload     = "geolocation"
    environment  = "prd"
    app          = "app-geo-location-api-prd-swedencentral-6f10eaac01a0"
    app_insights = "geolocation"
    uri          = "https://app-geo-location-api-prd-swedencentral-6f10eaac01a0.azurewebsites.net/v1.0/health/live"
    severity     = "high"
    site         = "mx"
    component    = "mx.sitewatch.geo-location-api"
  },
  {
    workload     = "geolocation"
    environment  = "prd"
    app          = "app-geo-location-web-prd-swedencentral-6f10eaac01a0"
    app_insights = "geolocation"
    uri          = "https://app-geo-location-web-prd-swedencentral-6f10eaac01a0.azurewebsites.net/api/health/live"
    severity     = "high"
    site         = "mx"
    component    = "mx.sitewatch.geo-location-web"
  },
  {
    workload     = "xtremeidiots-forums"
    environment  = "prd"
    app          = "www.xtremeidiots.com-web"
    app_insights = "default"
    uri          = "https://www.xtremeidiots.com"
    severity     = "critical"
    site         = "xi"
    component    = "xi.sitewatch.forums-web"
  },
  {
    workload     = "xtremeidiots-forums"
    environment  = "prd"
    app          = "www.xtremeidiots.com-tasks"
    app_insights = "default"
    uri          = "https://www.xtremeidiots.com/applications/core/interface/task/web.php?key=%xtremeidiots_forums_task_key%"
    severity     = "high"
    site         = "xi"
    component    = "xi.sitewatch.forums-tasks"
  },
  {
    workload     = "xtremeidiots-redirect"
    environment  = "prd"
    app          = "redirect.xtremeidiots.net"
    app_insights = "default"
    uri          = "https://redirect.xtremeidiots.net"
    severity     = "high"
    site         = "xi"
    component    = "xi.sitewatch.redirect"
  },
  {
    workload     = "bishopsbees"
    environment  = "prd"
    app          = "bishopbees.co.uk"
    app_insights = "default"
    uri          = "https://bishopsbees.co.uk"
    severity     = "critical"
    site         = "mx"
    component    = "mx.sitewatch.bishopsbees"
  },
  {
    workload     = "molyneux-me"
    environment  = "prd"
    app          = "molyneux.me"
    app_insights = "default"
    uri          = "https://www.molyneux.me/"
    severity     = "low"
    site         = "mx"
    component    = "mx.sitewatch.molyneux-me"
  },
  {
    workload     = "platform-radar"
    environment  = "prd"
    app          = "radar.molyneux.io"
    app_insights = "default"
    uri          = "https://radar.molyneux.io/health/live"
    severity     = "high"
    site         = "mx"
    component    = "mx.sitewatch.radar"
  },
  {
    workload         = "geo-location"
    environment      = "prd"
    app              = "mx.geo-location-web.routing"
    app_insights     = "platform"
    uri              = "https://www.geo-location.net/api/health/live"
    severity         = "high"
    site             = "mx"
    component        = "mx.sitewatch.geo-location-web"
    check_id         = "mx.geo-location-web.routing"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "bqba"
    environment      = "prd"
    app              = "mx.probe.bqba"
    app_insights     = "platform"
    uri              = "https://bqba.co.uk/"
    severity         = "moderate"
    site             = "mx"
    component        = "mx.probe.bqba"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "dm-electrical-services"
    environment      = "prd"
    app              = "mx.probe.dm-electrical-services"
    app_insights     = "platform"
    uri              = "https://www.dm-electricalservices.co.uk/"
    severity         = "moderate"
    site             = "mx"
    component        = "mx.probe.dm-electrical-services"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "craftpledge"
    environment      = "prd"
    app              = "mx.probe.craftpledge"
    app_insights     = "platform"
    uri              = "https://www.craftpledge.org/"
    severity         = "moderate"
    site             = "mx"
    component        = "mx.probe.craftpledge"
    interval_seconds = 300
    method           = "HEAD"
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "talkwithtiles"
    environment      = "prd"
    app              = "mx.probe.talkwithtiles"
    app_insights     = "platform"
    uri              = "https://talkwithtiles.molyneux.me/"
    severity         = "moderate"
    site             = "mx"
    component        = "mx.probe.talkwithtiles"
    interval_seconds = 300
    method           = "HEAD"
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "travel-itinerary"
    environment      = "prd"
    app              = "mx.probe.travel-itinerary"
    app_insights     = "platform"
    uri              = "https://travelplans.molyneux.me/"
    severity         = "moderate"
    site             = "mx"
    component        = "mx.probe.travel-itinerary"
    interval_seconds = 300
    method           = "HEAD"
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "twenty-one"
    environment      = "prd"
    app              = "mx.probe.twenty-one"
    app_insights     = "platform"
    uri              = "https://twenty-one.molyneux.me/"
    severity         = "moderate"
    site             = "mx"
    component        = "mx.probe.twenty-one"
    interval_seconds = 300
    method           = "HEAD"
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "trip-side-kick"
    environment      = "prd"
    app              = "mx.probe.trip-side-kick-website"
    app_insights     = "platform"
    uri              = "https://tripsidekick.net/"
    severity         = "moderate"
    site             = "mx"
    component        = "mx.probe.trip-side-kick-website"
    interval_seconds = 300
    method           = "HEAD"
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "trip-side-kick"
    environment      = "prd"
    app              = "mx.probe.trip-side-kick-app"
    app_insights     = "platform"
    uri              = "https://tripsidekick.app/api/health/live"
    severity         = "moderate"
    site             = "mx"
    component        = "mx.probe.trip-side-kick-app"
    interval_seconds = 300
    method           = "HEAD"
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "portal-web"
    environment      = "prd"
    app              = "xi.portal-web.routing"
    app_insights     = "platform"
    uri              = "https://portal.xtremeidiots.com/api/health/live"
    severity         = "high"
    site             = "xi"
    component        = "xi.sitewatch.portal-web"
    check_id         = "xi.portal-web.routing"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "portal-repository-v1"
    environment      = "prd"
    app              = "xi.repository-api-v1.readiness"
    app_insights     = "platform"
    uri              = "https://app-portal-repo-prd-uksouth-v1-655b79d54d89.azurewebsites.net/v1.0/health/ready"
    severity         = "high"
    site             = "xi"
    component        = "xi.sitewatch.repository-api-v1"
    check_id         = "xi.repository-api-v1.readiness"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "portal-repository-v2"
    environment      = "prd"
    app              = "xi.repository-api-v2.readiness"
    app_insights     = "platform"
    uri              = "https://app-portal-repo-prd-uksouth-v2-655b79d54d89.azurewebsites.net/v2.0/health/ready"
    severity         = "high"
    site             = "xi"
    component        = "xi.sitewatch.repository-api-v2"
    check_id         = "xi.repository-api-v2.readiness"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "portal-server-events"
    environment      = "prd"
    app              = "xi.sitewatch.server-events"
    app_insights     = "platform"
    uri              = "https://fn-portal-server-events-prd-uksouth-3e4bb7b00601.azurewebsites.net/api/health/live"
    severity         = "high"
    site             = "xi"
    component        = "xi.sitewatch.server-events"
    interval_seconds = 60
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "xi-cdn"
    environment      = "prd"
    app              = "xi.probe.cdn"
    app_insights     = "platform"
    uri              = "https://cdn.xtremeidiots.com/_platform/health"
    severity         = "moderate"
    site             = "xi"
    component        = "xi.probe.cdn"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "xi-redirect-manager"
    environment      = "prd"
    app              = "xi.probe.maps"
    app_insights     = "platform"
    uri              = "https://maps.xtremeidiots.com/"
    severity         = "moderate"
    site             = "xi"
    component        = "xi.probe.maps"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "xi-arcade"
    environment      = "prd"
    app              = "xi.probe.arcade"
    app_insights     = "platform"
    uri              = "https://www.xtremeidiots.com/arcade/"
    severity         = "moderate"
    site             = "xi"
    component        = "xi.probe.arcade"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "xi-casino"
    environment      = "prd"
    app              = "xi.probe.casino"
    app_insights     = "platform"
    uri              = "https://www.xtremeidiots.com/casino/"
    severity         = "moderate"
    site             = "xi"
    component        = "xi.probe.casino"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "xi-dailygames"
    environment      = "prd"
    app              = "xi.probe.dailygames"
    app_insights     = "platform"
    uri              = "https://www.xtremeidiots.com/dailygames/"
    severity         = "moderate"
    site             = "xi"
    component        = "xi.probe.dailygames"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "xi-powerball"
    environment      = "prd"
    app              = "xi.probe.powerball"
    app_insights     = "platform"
    uri              = "https://www.xtremeidiots.com/powerball/"
    severity         = "moderate"
    site             = "xi"
    component        = "xi.probe.powerball"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "xi-bank"
    environment      = "prd"
    app              = "xi.probe.wallet"
    app_insights     = "platform"
    uri              = "https://www.xtremeidiots.com/wallet/"
    severity         = "moderate"
    site             = "xi"
    component        = "xi.probe.wallet"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  },
  {
    workload         = "xi-freezetag-stats"
    environment      = "prd"
    app              = "xi.probe.freezetag"
    app_insights     = "platform"
    uri              = "https://www.xtremeidiots.com/stats/stats.html"
    severity         = "moderate"
    site             = "xi"
    component        = "xi.probe.freezetag"
    interval_seconds = 300
    retries          = 0
    follow_redirects = false
    headers_only     = true
  }
]

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
