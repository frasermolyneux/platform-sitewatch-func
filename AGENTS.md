# platform-sitewatch-func agent brief

## Purpose and ownership

This repository owns the scheduled Azure Functions workload that probes external sites, publishes
native Application Insights availability telemetry, exposes liveness/readiness endpoints, and
provisions its Function Apps, telemetry, configuration, and availability alerts with Terraform.

## Important paths

- `src/MX.Platform.SiteWatch.App/` — function host, timer function, HTTP client, configuration,
  health endpoints, and telemetry routing.
- `src/MX.Platform.SiteWatch.App/Availability/` — multi-target Application Insights fan-out.
- `src/MX.Platform.SiteWatch.App.Tests/` — probe, retry, logging, tracing, configuration, and
  telemetry contract tests.
- `contract/availability-telemetry-contract.json` — machine-readable producer/consumer contract.
- `terraform/` — application infrastructure; `backends/` and `tfvars/` contain dev/prd inputs.
- `docs/telemetry-contract.md` — cross-repository telemetry and deployment-order contract.
- `docs/development-workflows.md` — current CI/CD and label-driven deployment behavior.
- `src/*/bin/`, `src/*/obj/`, and Terraform `.terraform/` directories are generated.

## Useful commands

```pwsh
dotnet build src/MX.Platform.SiteWatch.slnx
dotnet test src/MX.Platform.SiteWatch.slnx --filter "FullyQualifiedName!~IntegrationTests"
dotnet test src/MX.Platform.SiteWatch.App.Tests/MX.Platform.SiteWatch.App.Tests.csproj --filter "FullyQualifiedName~ExternalHealthCheckTests"
dotnet format src/MX.Platform.SiteWatch.slnx --verify-no-changes

terraform -chdir=terraform fmt -check -recursive
terraform -chdir=terraform init -lockfile=readonly -backend-config=backends/dev.backend.hcl
terraform -chdir=terraform validate
terraform -chdir=terraform plan -var-file=tfvars/dev.tfvars
```

Use the SDK pinned by `global.json`. Run the smallest test selection that covers a code change.
Terraform init, validate, or plan requires the appropriate Azure/OIDC context.

## Contracts and constraints

- `ExternalHealthCheck` runs every 30 seconds, skips when external checks are disabled, and probes
  configured tests in parallel with a maximum concurrency of five.
- Preserve the five-second timeout and existing defaults: GET, redirects enabled, 30-second
  interval and three Polly retries with 2/4/8-second backoff. Reviewed public baselines explicitly
  use zero retries and longer intervals; do not silently change existing checks' budgets.
- Per-check scheduling is host-singleton state. Collect immediately on startup and when due,
  including delayed timer invocations; do not gate slow checks on an exact UTC timer slot.
  Preserve cancellation, token replacement and one terminal availability result per completed check.
- Availability results must include the `componentId`, `checkId`, `siteId`, and `region` custom dimensions
  defined by `contract/availability-telemetry-contract.json`. Configuration validation enforces
  the site/component relationship.
- Telemetry can fan out to named Application Insights targets; unknown/default targets fall back
  to the Function App's own required `APPLICATIONINSIGHTS_CONNECTION_STRING`.
- The telemetry contract is consumed outside this repository. Follow
  `docs/telemetry-contract.md`, keep the fixture synchronized with its consumer, and deploy the
  producer before consumers rely on a changed contract.
- Terraform creates regional Function Apps and Application Insights resources, reads workload and
  monitoring remote state, and owns availability metric alerts/action-group wiring. Dev and prd
  use separate Azure backends.
- External target URLs and replacement tokens may be sensitive; keep secrets in app settings or
  Key Vault rather than source control.
- Deployments are performed by GitHub Actions; do not deploy from routine development commands.
- Sitewatch and platform-monitoring own availability measurement, targets, cadence, operational
  thresholds and alerts. Status consumes these signals; never move workload probing into the
  status runtime to avoid monitoring cost. Surface any coverage/cost trade-off for approval.

## Authoritative repository docs

- `README.md`
- `docs/telemetry-contract.md`
- `docs/development-workflows.md`
- `contract/availability-telemetry-contract.json`
