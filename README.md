# platform-sitewatch-func

[![Build and Test](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/build-and-test.yml)
[![Code Quality](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/codequality.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/codequality.yml)
[![Copilot Setup Steps](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/copilot-setup-steps.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/copilot-setup-steps.yml)
[![Dependabot Auto-Merge](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/dependabot-automerge.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/dependabot-automerge.yml)
[![Deploy Dev](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/deploy-dev.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/deploy-dev.yml)
[![Deploy Prd](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/deploy-prd.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/deploy-prd.yml)
[![Destroy Development](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/destroy-development.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/destroy-development.yml)
[![Destroy Environment](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/destroy-environment.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/destroy-environment.yml)
[![PR Verify](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/pr-verify.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/pr-verify.yml)

## Documentation

* [Development Workflows](/docs/development-workflows.md) - Branch strategy, CI/CD triggers, and deployment flows.
* [Telemetry Contract](/docs/telemetry-contract.md) - The explicit customDimensions contract shared with `platform-status-web`.
* [Telemetry Pipelines](/docs/telemetry.md) - Azure Functions host filtering and isolated worker telemetry behavior.

## Overview

.NET 10 isolated Azure Functions app that runs external HTTP availability checks on a timer and publishes Availability telemetry to Application Insights. Targets are defined in configuration (or `test_config`) with optional per-target telemetry connection strings and token replacement for secrets. Polly-driven retries wrap each request, and a health endpoint exposes basic liveness for probes. Infrastructure is provisioned with Terraform and deployed via GitHub Actions workflows above.

## Hosting

Deployments target Linux Flex Consumption, which supports .NET 10. Each regional Function uses
512 MB, scales to zero with no always-ready instances, and is limited to one instance. Deployment
packages and host storage use its system-assigned identity against workload-owned storage.
Existing regional Application Insights resources, named telemetry targets and availability
dimensions are preserved.

AzureRM 5.9 injects a reserved, empty-key `AzureWebJobsStorage` connection string even for
identity-based deployments. The managed deployment health step first verifies the identity
host-storage settings and removes only that conflicting setting, without logging connection
strings. This prevents the legacy string from overriding managed identity while shared-key
access remains disabled.

Zone redundancy is deliberately disabled: Flex would bill at least two always-ready instances
per scaling group even when the configured count is zero
([Microsoft guidance](https://learn.microsoft.com/en-us/azure/reliability/reliability-functions#instance-distribution-across-zones)).
Production uses three independent regional probes instead. An individual regional probe can
pause during a zonal outage. The `CKV_AZURE_225` exception is limited to the new FC1 plan and
records this cost/reliability tradeoff; it does not disable scanning for other resources.

A subscription administrator must register the `Microsoft.App` resource provider before the
first Flex deployment in that subscription. This bootstrap prerequisite does not require
granting the workload CI identity subscription-wide permissions.

The migration first adds Flex Functions alongside the legacy Linux Consumption resources.
Legacy probe configuration is held unchanged for rollback. Remove the legacy Functions, plans
and their role assignments only after development and all three production regions have passed
health checks and emitted genuine availability telemetry.

## Contributing

Please read the [contributing](CONTRIBUTING.md) guidance; this is a learning and development project.

## Security

Please read the [security](SECURITY.md) guidance; I am always open to security feedback through email or opening an issue.
