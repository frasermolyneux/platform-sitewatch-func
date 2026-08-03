# Telemetry contract with platform-status-web

`platform-sitewatch-func` is the sole producer of the availability telemetry that
`platform-status-web` (and, eventually, the `status-pages` content repo) depends on to compute
public status. This document describes the explicit, stable contract between the two repositories.

## What gets emitted

Every native Application Insights `availabilityResults` row written by this app carries three
explicit `customDimensions`, built by `ExternalHealthCheck.BuildContractDimensions`:

| Dimension     | Required | Source                                                                 |
| ------------- | -------- | ----------------------------------------------------------------------- |
| `componentId` | Yes      | `TestConfig.Component`, falling back to `TestConfig.App` when unset     |
| `siteId`      | Yes      | `TestConfig.Site` (validated at startup, see below)                     |
| `region`      | Yes      | The `REGION_NAME` app setting (canonical Azure region string), or the sentinel `unknown` if missing/empty |

The canonical machine-readable shape of this contract lives in
[`contract/availability-telemetry-contract.json`](/contract/availability-telemetry-contract.json),
which is duplicated verbatim in `platform-status-web/contract/availability-telemetry-contract.json`.
Both repositories have a test (`TelemetryContractFixtureTests`) that loads their own copy of this
file and asserts their code's dimension names against it, so a rename on either side that isn't
mirrored in the other repo's copy fails CI instead of silently drifting in production.

## Validation

`TestConfigValidator` runs at startup (wired into `Program.cs`'s `PostConfigure<SiteWatchOptions>`)
and throws `InvalidOperationException` — failing the app's startup, consistent with this
repository's fail-fast configuration style — when:

- Any test config is missing `Site`.
- A test config's `Component` (when set) does not start with `"{Site}."`.

This keeps the `siteId`/`componentId` relationship enforced at the producer, not just assumed by
the consumer.

## Deployment order

Because `platform-status-web` always forces a `customDimensions.siteId` filter onto every
Application Insights query (see that repo's `TelemetryFilters.WithSiteId`) and classifies live
status per `customDimensions.region`, **this producer must be deployed before**
`platform-status-web`/`status-pages` content starts depending on these dimensions or on the
regional live-status query. There is currently no live `status-pages` content relying on the
previous, unversioned `component` dimension name, so this is a clean cutover rather than a live
migration — but any future contract change should still follow producer-then-consumer ordering.

## Multi-target routing is unaffected

Tests continue to route to `default`, `portal`, or `geolocation` Application Insights targets via
`MultiTargetAvailabilityTelemetry`/`AvailabilityTelemetryTargets`; the contract dimensions above are
additive metadata on every row regardless of which target(s) receive it.

## How to update this contract

1. Update [`contract/availability-telemetry-contract.json`](/contract/availability-telemetry-contract.json)
   identically in both repositories.
2. Update the emission code (`ExternalHealthCheck.cs`) and its tests (`ComponentDimensionTests.cs`,
   `TestConfigValidator(Tests).cs`) here.
3. Update the consuming query/filter code and tests in `platform-status-web`
   (`AvailabilityQueryBuilder(Tests).cs`, `TelemetryFilters(Tests).cs`,
   `ComponentStatusCalculator(Tests).cs`).
4. Deploy this repository (the producer) before `platform-status-web`/`status-pages` content starts
   relying on the change.
5. Re-run both repos' test suites — the `TelemetryContractFixtureTests` in each will fail if the
   fixture and code fall out of sync.
