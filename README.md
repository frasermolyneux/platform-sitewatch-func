# platform-sitewatch-func

[![Code Quality](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/codequality.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/codequality.yml)
[![Build and Test](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/build-and-test.yml)
[![PR Verify](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/pr-verify.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/pr-verify.yml)
[![Deploy Dev](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/deploy-dev.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/deploy-dev.yml)
[![Deploy Prd](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/deploy-prd.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/deploy-prd.yml)
[![Copilot Setup Steps](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/copilot-setup-steps.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/copilot-setup-steps.yml)
[![Dependabot Automerge](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/dependabot-automerge.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/dependabot-automerge.yml)
[![Destroy Development](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/destroy-development.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/destroy-development.yml)
[![Destroy Environment](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/destroy-environment.yml/badge.svg)](https://github.com/frasermolyneux/platform-sitewatch-func/actions/workflows/destroy-environment.yml)

## Documentation

* [Development Workflows](/docs/development-workflows.md) - Branch strategy, CI/CD triggers, and deployment flows.

## Overview

.NET 9 isolated Azure Functions app that runs external HTTP availability checks on a timer and publishes Availability telemetry to Application Insights. Targets are defined in configuration (or `test_config`) with optional per-target telemetry connection strings and token replacement for secrets. Polly-driven retries wrap each request, and a health endpoint exposes basic liveness for probes. Infrastructure is provisioned with Terraform and deployed via GitHub Actions workflows above.

## Contributing

Please read the [contributing](CONTRIBUTING.md) guidance; this is a learning and development project.

## Security

Please read the [security](SECURITY.md) guidance; I am always open to security feedback through email or opening an issue.

## Local dev: MCP wire-up

This repo is wired to the `frasermolyneux-copilot` MCP server, which exposes the shared org instruction / prompt / agent catalog from [`frasermolyneux/.github-copilot`](https://github.com/frasermolyneux/.github-copilot) to MCP-capable clients (VS Code Copilot Chat, the GitHub Copilot coding agent, Copilot CLI, Claude Desktop, etc.).

- Coding-agent config: [`.github/copilot/mcp_config.json`](.github/copilot/mcp_config.json)
- Setup steps that build the server in the runner: [`.github/workflows/copilot-setup-steps.yml`](.github/workflows/copilot-setup-steps.yml)
- Server source, content-root resolution, and per-client wire-up snippets: [`.github-copilot/mcp-server/README.md`](https://github.com/frasermolyneux/.github-copilot/blob/v0.1.0/mcp-server/README.md)
