# MemeTokenHub.Shared

Reusable contracts and infrastructure for the MemeTokenHub backend microservices.

The library targets .NET 10. Service repositories consuming the package must use a target framework compatible with `net10.0`.

## Included capabilities

- Public DTOs, pagination, enums, and consistent community vocabulary.
- RFC 7807 exceptions and error middleware with safe production responses.
- JWT issuing/validation and capability-based authorization helpers.
- Versioned integration-event envelopes, contracts, serialization, and deduplication primitives.
- Azure Service Bus, MongoDB, logging, and correlation registration helpers.
- A generic MongoDB repository base for service-owned collections.

The package deliberately excludes service-specific domain logic, persistence models, provider clients, and event handlers. Every service owns its database and consumes a pinned package version.

## Build

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet pack src/MemeTokenHub.Shared/MemeTokenHub.Shared.csproj --no-build
```

## GitHub Packages

Yes. `MemeTokenHub.Shared` is configured as a NuGet package that can be published to GitHub Packages and consumed by each backend service at a pinned semantic version.

Publishing is release-driven. Create a GitHub release with a semantic-version tag such as `v1.0.0`; the `Publish GitHub Package` workflow builds and tests the solution, packs that version, and publishes it to the repository owner's NuGet registry using the workflow's short-lived `GITHUB_TOKEN`.

### Consume the package from another service

GitHub Packages requires authentication, including for public NuGet packages. Create a GitHub personal access token with `read:packages`, expose it to the service repository as `GITHUB_PACKAGES_TOKEN`, and add the owner feed without committing the token:

```bash
dotnet nuget add source \
  "https://nuget.pkg.github.com/isboat/index.json" \
  --name github \
  --username "YOUR_GITHUB_USERNAME" \
  --password "$GITHUB_PACKAGES_TOKEN" \
  --store-password-in-clear-text

dotnet add <service-api.csproj> package MemeTokenHub.Shared \
  --version 1.0.0 \
  --source github
```

In GitHub Actions, grant the consuming workflow `packages: read` and use `${{ secrets.GITHUB_TOKEN }}` when the service repository has package access. If it does not, use an organization or repository secret containing a token with `read:packages`. Pin an exact package version in every service; do not use project references or floating versions.

See `docs/mth-docs/doc/backend/shared-library-instructions.md` for the complete blueprint.
