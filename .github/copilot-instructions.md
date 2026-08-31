# Copilot instructions

- This repository owns a .NET 10 isolated Azure Functions workload for scheduled external
  availability checks, Application Insights availability telemetry, health endpoints, and its
  Terraform.
- Use the SDK pinned in `global.json`; build and test through `src/MX.Platform.SiteWatch.slnx`.
- Preserve the 30-second timer, five-check concurrency cap, five-second HTTP timeout, three Polly
  retries with 2/4/8-second backoff, cancellation behavior, token substitution, and disable flag.
- `contract/availability-telemetry-contract.json` defines required `componentId`, `siteId`, and
  `region` dimensions. Follow `docs/telemetry-contract.md` for coordinated contract changes and
  producer-before-consumer deployment ordering.
- Preserve named Application Insights target routing and fallback to the required host
  `APPLICATIONINSIGHTS_CONNECTION_STRING`.
- Terraform owns regional Function Apps, Application Insights resources, availability alerts, and
  Key Vault integration; it uses separate dev/prd backends and workload/monitoring remote state.
- Keep target secrets and URL tokens in app settings or Key Vault.
- Never commit credentials or generated `bin/`, `obj/`, `.terraform/`, or state files.
