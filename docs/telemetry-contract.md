# Telemetry contract with platform-status-web

`platform-sitewatch-func` is the sole producer of the availability telemetry that
`platform-status-web` (and, eventually, the `status-pages` content repo) depends on to compute
public status. This document describes the explicit, stable contract between the two repositories.

## What gets emitted

Contract version 2 adds a stable check identity. Every native Application Insights
`availabilityResults` row written by this app carries four
explicit `customDimensions`, built by `ExternalHealthCheck.BuildContractDimensions`:

| Dimension     | Required | Source                                                                 |
| ------------- | -------- | ----------------------------------------------------------------------- |
| `componentId` | Yes      | `TestConfig.Component`, falling back to `TestConfig.App` when unset     |
| `checkId`     | Yes      | `TestConfig.CheckId`, falling back to `componentId` for existing checks |
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
- A check identity is duplicated within a site, or an explicit check ID is blank.
- The interval is not a multiple of 30 seconds between 30 and 3600 seconds, the method is not
  GET/HEAD, or retries are outside 0..3.

This keeps the `siteId`/`componentId` relationship enforced at the producer, not just assumed by
the consumer.

The mirrored `contract/status-monitoring-bindings.json` additionally declares the canonical
public inputs, resource aliases, regions and collection intervals. Production Terraform variable
validation requires exactly one configured producer check for every consumed binding with the
same cadence and telemetry destination, and requires the complete regional deployment set.
Consumer catalogue tests compare all brand projections with this same fixture. Update both copies
together; a missing binding is a release failure, not permission for status to collect it itself.

## Deployment order

Because `platform-status-web` explicitly filters owning site/component/check identities and
selects the latest native measurement per region rather than averaging a success window,
**this producer must be deployed before**
`platform-status-web`/`status-pages` content starts depending on these dimensions or on the
regional live-status query. Verify complete regional observations before cutting over existing
public coverage. A missing or stale measurement must never become a healthy observation.

## Monitoring ownership and request budgets

Sitewatch owns endpoint targets, collection, cadence and operational alerts as part of
platform-monitoring. Status owns consumption, public presentation/history and operator publishing;
it must not collect the same workload endpoints independently.

Existing checks retain GET, redirects enabled, a 30-second interval and three retries. Reviewed
public baselines explicitly use zero retries, GET/HEAD, headers-only completion and no redirect following; a 2xx/3xx response
is healthy for those no-redirect checks, without authenticating or executing a business transaction.
New baseline intervals are 300 seconds except Server Events at 60 seconds.
Existing checks retain content-reading completion; a failed body transfer remains an availability
failure rather than changing their established request semantics.

The singleton schedule runs slower checks immediately on host startup and then no sooner than
their configured interval. Existing 30-second checks retain every timer tick, including ordinary
invocation jitter. Delayed ticks collect due checks rather than missing a UTC slot. Restarts may
add one collection round; no persisted per-target scheduler or always-ready instance is required.
The approved migration allowance is US$15/month incremental normal-operation planning, not a cap.
Failures, restarts, execution time and telemetry volumes remain variable; do not silently remove
coverage or move collection into status to enforce that allowance.

## Multi-target routing is unaffected

Tests route to `default`, `platform`, `portal`, or `geolocation` Application Insights targets via
`MultiTargetAvailabilityTelemetry`/`AvailabilityTelemetryTargets`; the contract dimensions above are
additive metadata on every row. Each result goes to one target: `default` is the regional host
resource; `platform` is the existing primary-region Sitewatch resource shared by the added baselines.

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
