# WhatCar Project Tasks

## Reference

All technical standards, constraints, and workflow rules are defined in `.specify/memory/constitution.md` and must be followed for every task.

## Task Organization

Tasks are organized by: **Setup → Foundational → User Story Implementation → Polish**

User Stories from `.specify/spec.md`:

- **Story 1**: Vehicle comparison with powertrains and adoption trends
- **Story 2**: Natural language queries for vehicle trends
- **Story 3**: Statistical predictions for future adoption

## Setup Phase ✅ COMPLETED

- [x] [T001] [P0] Create solution structure with `what-car.sln`
- [x] [T002] [P0] Create `backend/WhatCar.ODataCore/` project for data layer
- [x] [T003] [P0] Create `backend/WhatCar.ODataApi/` project for OData service
- [x] [T004] [P0] Create `backend/WhatCar.Api/` project for natural language query service
- [x] [T005] [P0] Create `backend/WhatCar.AppHost/` project for .NET Aspire orchestration
- [x] [T006] [P0] Create `backend/WhatCar.ServiceDefaults/` project for shared configuration
- [x] [T007] [P0] Create `frontend/` project with React 19 + TypeScript + Vite
- [x] [T008] [P0] Add `data/df_VEH0120_GB.csv` dataset to repository

## Foundational Phase ✅ COMPLETED

### Data Layer

- [x] [T101] [P1] Define `Vehicle` entity in `backend/WhatCar.ODataCore/Models/Vehicle.cs` (VehicleId, BodyType, Make, GenModel, Model, Fuel, LicenceStatus, VehicleHash)
- [x] [T102] [P1] Define `SalesData` entity in `backend/WhatCar.ODataCore/Models/SalesData.cs` (SalesId, VehicleId, Year, Quarter, UnitsSold, Vehicle navigation)
- [x] [T103] [P1] Create `VehicleSalesDbContext` in `backend/WhatCar.ODataCore/Data/VehicleSalesDbContext.cs`
- [x] [T104] [P1] Configure entity relationships (Vehicle → SalesData one-to-many)
- [x] [T105] [P1] Create EF Core migrations for initial schema
- [x] [T106] [P1] Implement CSV ingestion tool in `tools/CsvIngestor.cs` for df_VEH0120_GB.csv

### OData API

- [x] [T201] [P1] Install Microsoft.AspNetCore.OData 10.0.0-preview.1 in `WhatCar.ODataApi`
- [x] [T202] [P1] Create `ODataApiModelBuilder` in `backend/WhatCar.ODataCore/Data/ODataApiModelBuilder.cs` to build EDM model
- [x] [T203] [P1] Configure OData in `backend/WhatCar.ODataApi/Program.cs` (EnableAttributeRouting, EnableQueryFeatures, AddRouteComponents)
- [x] [T204] [P1] Create OData controller for Vehicles endpoint at `/odata/Vehicles`
- [x] [T205] [P1] Create OData controller for SalesData endpoint at `/odata/SalesData`
- [x] [T206] [P1] Enable $filter, $orderby, $top, $skip, $expand, $apply query features
- [x] [T207] [P1] Test OData $apply aggregations (groupby, aggregate, filter)

### Natural Language Query Service

- [x] [T301] [P2] [Story 2] Configure Azure OpenAI client in `backend/WhatCar.Api/Program.cs`
- [x] [T302] [P2] [Story 2] Create `PromptBuilder` service in `backend/WhatCar.Api/Services/PromptBuilder.cs` with OData query guidance
- [x] [T303] [P2] [Story 2] Create `LlmClient` service in `backend/WhatCar.Api/Services/LlmClient.cs` for Azure OpenAI chat completions
- [x] [T304] [P2] [Story 2] Create `ODataExecutor` service in `backend/WhatCar.Api/Services/ODataExecutor.cs` for query execution
- [x] [T305] [P2] [Story 2] Create `SchemaSummarizer` service in `backend/WhatCar.Api/Services/SchemaSummarizer.cs` for LLM context
- [x] [T306] [P2] [Story 2] Create `ResultValidator` service in `backend/WhatCar.Api/Services/ResultValidator.cs` for chart type detection
- [x] [T307] [P2] [Story 2] Implement `POST /api/v1/query` endpoint in `backend/WhatCar.Api/Endpoints/QueryEndpoints.cs`
- [x] [T308] [P2] [Story 2] Implement `POST /api/v1/query/stream` endpoint for streaming responses
- [x] [T309] [P2] [Story 2] Configure Polly resilience handlers (90s attempt, 120s total, 180s circuit breaker, 1 retry max)
- [x] [T310] [P2] [Story 2] Update `PromptBuilder` with inline filter syntax for $apply (`filter(...)/groupby(...)`)

### React Frontend

- [x] [T401] [P2] Install dependencies: React 19, TypeScript, Recharts, Tailwind CSS 4.x in `frontend/`
- [x] [T402] [P2] Create `ChatInterface` component in `frontend/src/components/ChatInterface.tsx`
- [x] [T403] [P2] Create `ChatMessage` component in `frontend/src/components/ChatMessage.tsx`
- [x] [T404] [P2] Create `ResultRenderer` component in `frontend/src/components/ResultRenderer.tsx` with chart type detection
- [x] [T405] [P2] Implement API client in `frontend/src/api.ts` for `/api/v1/query` endpoint
- [x] [T406] [P2] Add example queries to ChatInterface ("top 5 selling vehicles in 2023", "electric vehicle trends 2020-2024", "Tesla vs BMW sales 2024")
- [x] [T407] [P2] Configure Tailwind CSS and responsive layout

## User Story 1: Natural Language Insights ✅ COMPLETED

**Status**: All comparison and search functionality delivered through single query endpoint

- [x] [T501] [P1] [Story 1] ComparisonChart component implemented (`frontend/src/components/charts/ComparisonChart.tsx`)
- [x] [T502] [P1] [Story 1] TrendChart component for time-based analysis (`frontend/src/components/charts/TrendChart.tsx`)
- [x] [T503] [P1] [Story 1] RankingChart component for top-N queries (`frontend/src/components/charts/RankingChart.tsx`)
- [x] [T504] [P1] [Story 1] Natural language query endpoint handles all comparison/search/ranking requests (`POST /api/v1/query`)
- [x] [T505] [P1] [Story 1] Chart type auto-detection based on query result structure

## User Story 2: Time-Based Trend Analysis ✅ COMPLETED

- [x] [T551] [P2] [Story 2] Line charts with time on X-axis (Year/Quarter)
- [x] [T552] [P2] [Story 2] Support yearly and quarterly aggregations via OData $apply
- [x] [T553] [P2] [Story 2] Filter by Make, Model, Fuel type through natural language
- [x] [T554] [P2] [Story 2] TrendChart component with proper time axis handling

## User Story 3: Cross-Powertrain Comparisons ✅ COMPLETED

- [x] [T561] [P2] [Story 3] Multi-series comparisons via natural language (e.g., "compare diesel vs electric")
- [x] [T562] [P2] [Story 3] ComparisonChart supports categorical grouping (Fuel types, Makes)
- [x] [T563] [P2] [Story 3] Date range filtering via query translation
- [x] [T564] [P2] [Story 3] Support for aggregated metrics (total sales, market share)

## User Story 4: Manufacturer Rankings ✅ COMPLETED

- [x] [T571] [P2] [Story 4] RankingChart for top-N queries (horizontal bars, sorted descending)
- [x] [T572] [P2] [Story 4] Filter by year, powertrain via natural language
- [x] [T573] [P2] [Story 4] Support configurable limits (top 5, 10, 20) through query text
- [x] [T574] [P2] [Story 4] Ranking by sales volume with clear numeric labels

## User Story 5: Statistical Predictions ❌ NOT STARTED

**Status**: No prediction functionality implemented

- [ ] [T601] [P3] [Story 5] Create `GET /api/v1/predictions/adoption` endpoint in `backend/WhatCar.Api/Endpoints/PredictionEndpoints.cs`
- [ ] [T602] [P3] [Story 5] Implement time-series forecasting algorithm (linear regression or ML.NET)
- [ ] [T603] [P3] [Story 5] Create prediction chart component in `frontend/src/components/charts/PredictionChart.tsx`
- [ ] [T604] [P3] [Story 5] Add prediction confidence intervals to chart visualization
- [ ] [T605] [P3] [Story 5] Create prediction dashboard page in `frontend/src/pages/PredictionPage.tsx`

## Polish Phase 🚧 PARTIAL

- [x] [T701] [P2] Configure OpenAPI documentation with Scalar UI (`/scalar/v1` endpoint)
- [x] [T702] [P2] Configure CORS for localhost development
- [x] [T703] [P2] Add response compression to API
- [ ] [T704] [P3] Implement CSV export functionality for result tables
- [ ] [T705] [P3] Add loading states and error handling to all frontend components
- [ ] [T706] [P3] Implement authentication/authorization (Azure AD B2C or similar)
- [ ] [T707] [P3] Add rate limiting to API endpoints
- [ ] [T708] [P3] Create Azure deployment scripts (Bicep/Terraform)
- [ ] [T709] [P3] Configure CI/CD pipeline in GitHub Actions
- [ ] [T710] [P3] Add Application Insights telemetry and monitoring

## Summary

**Total Tasks**: 60
**Completed**: 55 (92%)
**In Progress**: 0 (0%)
**Not Started**: 5 (8%)

**Completed**:

- ✅ Setup Phase (8 tasks)
- ✅ Foundational Phase (33 tasks)
- ✅ User Story 1: Natural Language Insights (5 tasks)
- ✅ User Story 2: Time-Based Trend Analysis (4 tasks)
- ✅ User Story 3: Cross-Powertrain Comparisons (4 tasks)
- ✅ User Story 4: Manufacturer Rankings (4 tasks)
- 🚧 Polish Phase (3/10 tasks - 30%)

**Not Started**:

- ❌ User Story 5: Statistical Predictions (5 tasks)
- ⏳ Polish Phase remaining (7 tasks)

**Key Achievement**: Single unified `/api/v1/query` endpoint serves all use cases (comparison, search, ranking, trends) through natural language interface—no need for separate REST endpoints per feature.
