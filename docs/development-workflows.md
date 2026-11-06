# Development Workflows

Target: Engineers working on platform-sitewatch-func (Azure Functions uptime checks). Covers branch strategy, CI/CD triggers, and label-driven deployment flows.

## Branch Strategy & Triggers

### Feature Development feature/*, bugfix/*, hotfix/*
- **build-and-test.yml**: Runs on push to feature/bugfix/hotfix branches
  - Build and test via `dotnet-func-ci` targeting .NET 10
  - Runs a Development Terraform plan when Terraform files change; no deployments

### Pull Requests → main
- **pr-verify.yml**: Validation pipeline (runs on PR open/updates/reopen/label changes)
  - Build and test
  - Terraform plan for dev by default (skips drafts)
  - Terraform plan+apply + deploy to dev when PR has `deploy-dev` label
  - Terraform plan for prd when PR has `run-prd-plan` label
  - Concurrency groups prevent parallel dev/prd plans and deployments

### Production Promotion
- **deploy-prd.yml**: Promotion pipeline
  - Runs on pushes to `main` changing `src/**`, `contract/**`, or `terraform/**`, the weekly Friday 5am UTC schedule, and manual dispatch. Workflow-only pushes do not start a promotion.
  - Scheduled and manual events force both change flags to true in `detect-changes/v1.0`: they are full Build → Dev apply/deploy/health → Prd apply/deploy/health promotions, even without a source diff.
  - Source or Terraform changes, or tainted development state, trigger Build → Dev plan/apply → Deploy Dev for every event type.
  - A failed or cancelled build blocks development apply; failed development deployment or health checks block production promotion.
  - When Terraform is unchanged and development state is intact, reads existing development outputs instead of applying Terraform.
  - Production applies Terraform when it changed and otherwise reads existing outputs; source or Terraform changes deploy the production application, including newly created Flex Functions.
  - Development must pass liveness and readiness before production promotion; production regions deploy sequentially with the same bounded health gate.

### Scheduled & On-Demand
- **codequality.yml**: Weekly Monday 5:15am UTC + PR/push to main
  - SonarCloud analysis, DevOps secure scanning, dependency review (on PRs)
- **deploy-dev.yml**: Manual dispatch
  - Build → Dev plan/apply → Deploy Dev
- **destroy-development.yml**: Destroys Development daily at 23:45 UTC or on manual dispatch.
- **destroy-environment.yml**: Destroys the selected Development or Production environment on manual dispatch.

## Standard Developer Flow

### Local Development
```bash
# Standard validation sequence
cd src/MX.Platform.SiteWatch.App
dotnet clean
dotnet build
dotnet test --filter "FullyQualifiedName!~IntegrationTests"
```

### Feature Branch → PR → Merge Flow

```mermaid
graph TD
    A[Create feature branch] --> B[Make commits]
    B --> C{Push to feature/bugfix/hotfix}
    C --> D[build-and-test runs]
    D --> E[Open PR to main]
    E --> F[pr-verify triggers]
    F --> G[Build & Test]
    G --> H[Dev TF Plan]
    H --> I{Add deploy-dev label?}
    I -->|Yes| J[Dev TF Apply + Deploy Dev]
    I -->|No| K[Review & Approve]
    J --> K
    K --> L[Merge PR]
    L --> M{Source, contract or Terraform changed?}
    M -->|No| T[No automatic promotion]
    M -->|Yes| N[Build & Test]
    N --> O[Dev: apply Terraform if needed, otherwise read outputs]
    O --> P[Deploy Dev + Health]
    P --> R[Prd: apply Terraform if changed, otherwise read outputs]
    R --> S[Deploy Prd + Health]

    style D fill:#e1f5ff
    style F fill:#e1f5ff
    style M fill:#ffe1e1
    style P fill:#90EE90
    style R fill:#FFB6C1
    style S fill:#FFB6C1
```

## Copilot & Label Guidance

- `deploy-dev` label: runs Terraform plan+apply for Development and deploys the function app to dev during PR validation.
- `run-prd-plan` label: runs Terraform plan for Production during PR validation.
- Draft PRs skip Terraform actions. Dependabot PRs can run the default Development plan and the label-triggered Production plan, but cannot run the Development deployment.

## Composite Actions (frasermolyneux/actions)

- **dotnet-func-ci**: Build and test the function app.
- **terraform-plan** / **terraform-plan-and-apply**: Init, plan, and optionally apply Terraform.
- Terraform actions use the root's committed `.terraform-version` and provider lock file with readonly initialization. Plans, including PR plans, are stored in the private `tfplan` container in the initialized backend account; the environment's OIDC identity needs blob data write access, Azure CLI, and network access. Saved plans may contain secrets and require appropriate access and retention controls.
- Optional Terraform additional arguments are parsed as quoted data, not shell expressions; shell expansion, pipes and redirection are not supported.
- **deploy-function-app**: Deploy Azure Function Apps from build artifacts.
- `.github/scripts/wait-for-function-health.sh`: Fail deployment if the application does not expose healthy liveness and readiness endpoints within five minutes. Used by promotion, manual development and PR development deployments.

## Quick Reference

| Scenario           | Workflow       | Trigger                  | Terraform     | Deploy               |
| ------------------ | -------------- | ------------------------ | ------------- | -------------------- |
| Feature commit     | build-and-test | Push to feature/bugfix/hotfix | Dev plan when Terraform changes | ❌ |
| PR validation      | pr-verify      | PR to main               | Dev plan      | Optional dev (label) |
| Merge to main      | deploy-prd     | Push changing src, contract, or terraform | As needed | Dev+Prd, health-gated |
| Weekly maintenance | deploy-prd     | Friday 5am UTC           | Dev+Prd apply | Dev+Prd, health-gated |
| Manual promotion   | deploy-prd     | Manual dispatch          | Dev+Prd apply | Dev+Prd, health-gated |
| Manual dev refresh | deploy-dev     | Manual dispatch          | Dev apply     | Dev                  |
| Development teardown | destroy-development | Daily 23:45 UTC or manual dispatch | Dev destroy | ❌ |
| Targeted teardown  | destroy-environment | Manual dispatch     | Dev or Prd destroy | ❌ |
| Code quality       | codequality    | Monday 5:15am UTC + PR/push | ❌          | ❌                    |

## Environment Secrets

OIDC authentication uses GitHub environment variables (not repository secrets):
- `AZURE_CLIENT_ID`: Service principal app ID
- `AZURE_TENANT_ID`: Azure AD tenant
- `AZURE_SUBSCRIPTION_ID`: Target subscription

Defined in **Development** and **Production** environments within GitHub repository settings.
