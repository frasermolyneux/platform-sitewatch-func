# Copilot instructions

- This repository owns a .NET 10 isolated Azure Functions workload for scheduled external
  availability checks, Application Insights availability telemetry, health endpoints, and its
  Terraform.
- Use the SDK pinned in `global.json`; build and test through `src/MX.Platform.SiteWatch.slnx`.
- Preserve the 30-second timer, five-check concurrency cap, five-second HTTP timeout, cancellation,
  token substitution and disable flag. Existing checks retain three Polly retries with 2/4/8-second
  backoff; explicitly reviewed zero-retry baselines and per-check intervals are supported.
- Keep cadence state in the host-singleton scheduler so late ticks do not skip slow checks.
- Sitewatch/platform-monitoring own measurement, endpoint targets and operational alerts. Status
  consumes measurements, not workload endpoints; never bypass this boundary to meet a cost target.
- `contract/availability-telemetry-contract.json` defines required `componentId`, `checkId`, `siteId`, and
  `region` dimensions. Follow `docs/telemetry-contract.md` for coordinated contract changes and
  producer-before-consumer deployment ordering.
- Preserve named Application Insights target routing and fallback to the required host
  `APPLICATIONINSIGHTS_CONNECTION_STRING`.
- Terraform owns regional Function Apps, Application Insights resources, availability alerts, and
  Key Vault integration; it uses separate dev/prd backends and workload/monitoring remote state.
- Keep target secrets and URL tokens in app settings or Key Vault.
- Never commit credentials or generated `bin/`, `obj/`, `.terraform/`, or state files.
