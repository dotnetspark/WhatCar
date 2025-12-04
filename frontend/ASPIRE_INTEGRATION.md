# Frontend Aspire Integration

## Overview

The frontend is orchestrated by .NET Aspire, which handles:

- Service discovery (no hardcoded URLs)
- Environment variable injection
- Development server lifecycle
- Production deployment

## How It Works

### Service Discovery

Aspire automatically injects service URLs as environment variables:

```typescript
// api.ts uses Aspire-injected URLs
const API_BASE_URL = import.meta.env.VITE_services__whatcar_api__https__0;
```

### AppHost Configuration

```csharp
var frontend = builder.AddNpmApp("frontend", "../../../frontend")
    .WithHttpEndpoint(port: 5173, env: "PORT")
    .WithReference(webApi)  // Injects whatcar-api URL
    .WaitFor(webApi);
```

### CORS Configuration

Backend allows all localhost origins in development (managed by Aspire):

```csharp
policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
```

## Development

```bash
# Start everything with Aspire
dotnet run --project backend/WhatCar.AppHost/WhatCar.AppHost.csproj
```

Aspire will:

1. Start SQL Server container
2. Start Redis container
3. Run CSV loader
4. Start OData API
5. Start WhatCar API
6. Start frontend (npm run dev)
7. Open Aspire dashboard

## Production

Aspire generates:

- Docker Compose files
- Kubernetes manifests
- Azure Container Apps configuration

```bash
# Publish
dotnet publish backend/WhatCar.AppHost/WhatCar.AppHost.csproj
```

## Benefits

✅ No hardcoded URLs  
✅ Service discovery built-in  
✅ Unified development experience  
✅ Production-ready deployment  
✅ Health checks and monitoring
