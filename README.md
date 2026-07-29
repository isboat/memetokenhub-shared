# MemeTokenHub.Shared

Reusable contracts and infrastructure for the MemeTokenHub backend microservices.

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

See `docs/mth-docs/doc/backend/shared-library-instructions.md` for the complete blueprint.
