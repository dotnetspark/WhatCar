# WhatCar Project Plan

## Reference

All technical and workflow standards are governed by `.specify/memory/constitution.md`.

## Implementation Status

This plan reflects the **actual implementation** as of 2025-12-04.

## Phases

### Phase 1: Data Foundation & OData Architecture ✅ COMPLETED

- Created **WhatCar.ODataCore** library with entities (Vehicle, SalesData) and VehicleSalesDbContext
- Implemented CSV ingestion tool (`tools/CsvIngestor.cs`) for df_VEH0120_GB.csv
- Normalized vehicle registration data into SQL Server
- Established Entity Framework Core 10 migrations
- Database schema: Vehicle (VehicleId, BodyType, Make, GenModel, Model, Fuel, LicenceStatus, VehicleHash) → SalesData (SalesId, VehicleId, Year, Quarter, UnitsSold)

### Phase 2: OData API ✅ COMPLETED

- Built **WhatCar.ODataApi** service with ASP.NET Core OData 10.0.0-preview.1
- Implemented OData endpoints: `/odata/Vehicles`, `/odata/SalesData`
- Configured OData query features: $filter, $orderby, $top, $skip, $expand, $apply (aggregations)
- Enabled attribute routing and query features (maxTop: 100)
- OData EDM model built via `ODataApiModelBuilder.GetEdmModel()`

### Phase 3: Natural Language Query API ✅ COMPLETED

- Built **WhatCar.Api** service with natural language to OData translation
- Integrated Azure OpenAI for LLM-powered query translation
- Implemented endpoints:
  - `POST /api/v1/query` - Non-streaming natural language query
  - `POST /api/v1/query/stream` - Streaming natural language query
- Created `PromptBuilder` service with OData syntax guidance (including $apply inline filter patterns)
- Created `LlmClient` for Azure OpenAI chat completions
- Created `ODataExecutor` for executing translated OData queries against WhatCar.ODataApi
- Configured Polly resilience handlers (90s attempt timeout, 120s total, 180s circuit breaker, 1 retry max)
- Added `SchemaSummarizer` for providing LLM with database schema context
- Implemented result validation (`ResultValidator`) and chart type detection

### Phase 4: React Frontend with Chat Interface ✅ COMPLETED

- Implemented **React 19** + **TypeScript** frontend with **Vite** build tooling
- Styled with **Tailwind CSS 4.x**
- Built chat-based UI as primary interaction pattern:
  - `ChatInterface.tsx` - Main chat container with message history and input
  - `ChatMessage.tsx` - Message rendering component
  - `ResultRenderer.tsx` - Automatic chart type detection and rendering
- Implemented visualization components with **Recharts**:
  - `TrendChart.tsx` - Line charts for time-series (Year/Quarter trends)
  - `ComparisonChart.tsx` - Bar charts for entity comparisons (Make, Fuel type)
  - `RankingChart.tsx` - Horizontal bar charts for top-N rankings
  - Table view for complex/unrecognized result structures
- Example queries displayed to guide users

### Phase 5: Infrastructure & Deployment 🚧 IN PROGRESS

- **WhatCar.AppHost** - .NET Aspire orchestration for local development
- **WhatCar.ServiceDefaults** - Shared service configuration
- SQL Server connection via connection strings
- Redis distributed cache configuration (referenced but not verified)
- CORS configured for localhost development
- OpenAPI documentation with Scalar UI (development only)
- **Not yet implemented**: Azure deployment, CI/CD pipeline, production monitoring

## Not Implemented (Planned Features)

The following features are **defined in the constitution** but **not yet built**:

- ❌ Vehicle recommendation algorithm with scoring matrix
- ❌ Dedicated comparison endpoints (`/api/v1/vehicles/compare`, `/api/v1/vehicles/{id}/comparable`)
- ❌ Autocomplete search endpoint (`/api/v1/vehicles/search`)
- ❌ Top-N rankings endpoint (`/api/v1/rankings/top`)
- ❌ Adoption trends endpoint (`/api/v1/trends/adoption`)
- ❌ Pre-computed comparison matrices and caching strategy
- ❌ Dedicated comparison page UI (alternative to chat interface)
- ❌ Vehicle cards with manufacturer logos and powertrain badges
- ❌ CSV export functionality
- ❌ Authentication/authorization
- ❌ Rate limiting

## Current Architecture

**Data Flow**: User natural language query → WhatCar.Api → Azure OpenAI (translate to OData) → WhatCar.ODataApi (execute query) → SQL Server → Results → React Frontend (render chart/table)

**Key Design Decisions**:

- OData as the query abstraction layer (not custom REST endpoints)
- Chat interface as primary UX (not search/comparison pages)
- LLM translates natural language to structured OData queries
- Frontend detects result structure and auto-selects chart type
- Resilience handlers accommodate slow aggregation queries (up to 90s)
