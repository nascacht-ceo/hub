# nc-cloud

Cloud-agnostic abstractions for multi-tenant applications.

## Purpose

Provides interfaces and base implementations for building applications that work across multiple cloud providers (AWS, GCP, Azure) with multi-tenant support.

## Key Interfaces

| Interface | Description |
|-----------|-------------|
| `ITenant` | Base tenant with `TenantId` and `Name` |
| `ITenantManager<T>` | Add/remove tenants, create cloud-specific services |
| `ITenantAccessor` | Get/set current tenant context (AsyncLocal-based) |
| `ICloudFileService` | Manage cloud storage containers/buckets |
| `ICloudFileProvider` | File operations within a container |

## Multi-Tenancy Pattern

```csharp
// Set tenant context for current async flow
using (tenantAccessor.SetTenant("customer-a"))
{
    // All operations use customer-a's credentials
    var service = await tenantManager.GetServiceAsync<IS3Client>();
}
```

## Provider Implementations

- **nc-aws** - `AmazonTenant`, `AmazonTenantManager`
- **nc-google** - `GoogleTenant`, `GoogleTenantManager`
- **nc-azure** - (planned)

## Usage

```csharp
services.AddSingleton<ITenantAccessor<AmazonTenant>, TenantAccessor<AmazonTenant>>();
services.AddSingleton<AmazonTenantManager>();
```
