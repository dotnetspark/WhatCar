# UK Vehicle Sales Analytics - Constitution

## Purpose

This application analyzes UK vehicle registration data to provide trend insights, comparisons, and predictions through natural language queries and visualizations.

## Core Principles

### I. Data Integrity

**The database is the single source of truth.** All features serve the data, not the other way around. Data transformations must be reproducible, documented, and versioned. No feature ships without verified data integrity. Raw CSV data is for initial seeding only—once loaded, the database governs.

### II. Natural Language First

**Users speak, we translate.** Complex queries are expressed in plain English, not technical syntax. The system translates natural language to structured queries using AI. Users should never need to learn OData, SQL, or any query language. If a user can't ask it in a sentence, we've failed.

### III. Visual Clarity Over Complexity

**Charts must tell the story without explanation.** Every visualization should be immediately understandable. No dual-axis charts that confuse interpretation. Time trends show change over time. Comparisons show relative differences. Rankings show order. Tables are the fallback when structure is unclear. All visualizations must meet WCAG AA accessibility standards.

### IV. Performance Acceptable, Not Perfect

**Fast enough beats perfectly optimized.** Simple queries should feel instant (<200ms). Complex aggregations can take up to 10 seconds—users will wait for valuable insights. Initial page load must be under 2 seconds. Prioritize shipping working features over micro-optimizations. Apply the YAGNI principle: build only what's needed.

### V. Scale Pragmatically

**Design for growth, but don't over-engineer.** Stateless API design enables horizontal scaling when needed. Distributed caching (Redis) handles hot data. Database queries should be efficient but don't optimize prematurely. Load testing validates claims before scaling up. Connection pooling and async I/O are non-negotiable.

## Technology Standards

### Stack

- **Frontend**: React + Vite + Tailwind CSS
- **Backend**: ASP.NET Core Minimal API
- **AI**: GPT-4o-mini deployed to Azure AI Foundry
- **Caching**: Redis (distributed)
- **Database**: SQL Server
- **Deployment**: Azure (App Service, SQL Database, managed services)

### Non-Negotiables

- TypeScript strict mode for frontend code
- Parameterized queries only (no SQL injection risk)
- HTTPS in production, no exceptions
- API versioning from day one (`/api/v1/...`)
- Secrets in Azure Key Vault, never in code
- Browser support: modern evergreen browsers only (Chrome, Firefox, Safari, Edge - last 2 versions)

## Data Standards

### Schema Design

- UK vehicle registration data: Makes, Models, Fuel types, Years, Quarters, Units Sold
- Normalized relational schema (Vehicles ↔ SalesData)
- Database versioning via Entity Framework Core migrations
- All timestamps use UTC
- Data validation on import: no negative values, reasonable year ranges (1990-2030), required fields enforced

### Data Quality

- CSV imports must validate schema before processing
- Missing or malformed data logged as warnings, not errors
- Bad rows are skipped with clear logging
- Manual spot-checks required before production data updates
- Yearly aggregations must match quarterly sums (integrity check)

## API Standards

### Natural Language Query Endpoints

- `POST /api/v1/query` - Accepts natural language, returns structured data
- `POST /api/v1/query/stream` - Streaming variant for large results
- Both endpoints translate user questions to database queries via GPT-4o-mini
- Results include: data payload, suggested chart type, and metadata

### Query Translation Rules

- User query → AI prompt → OData query → SQL execution → JSON response
- AI must validate queries before execution (no destructive operations)
- Invalid queries return helpful error messages, not cryptic failures
- Query timeout: 90 seconds for complex aggregations, fail gracefully after

### Response Standards

- Compression enabled (gzip/brotli)
- CORS configured appropriately per environment
- JSON responses only
- Chart type hints: `trend`, `comparison`, `ranking`, `table`
- Include row count and execution time in metadata

## User Experience Standards

### Interaction Model

**Chat-based interface is the primary UX.** Users type natural language questions, system responds with visualizations and data. Example queries are shown to guide users. Message history preserved during session.

### Visualization Rules

- **Trend Charts**: Time on X-axis (Year/Quarter), metric on Y-axis (Units Sold, Market Share)
- **Comparison Charts**: Categories on X-axis (Make, Fuel Type), metric on Y-axis (Total Sales)
- **Ranking Charts**: Top-N items, horizontal bars sorted by value descending
- **Tables**: Default fallback when data structure is ambiguous
- All charts responsive, mobile-friendly, and accessible

### Planned Features (Not Yet Implemented)

- Autocomplete vehicle search
- Pre-built query templates ("Show me top 10 EVs in 2024")
- Multi-vehicle comparison pages
- Statistical predictions (time-series forecasting)
- CSV export for results

## Performance Standards

- **Simple queries**: <200ms p95 latency (filter, sort, single-table queries)
- **Complex queries**: <10s p95 latency (multi-table joins, aggregations, grouping)
- **Page load**: <2s on 3G connection
- **Bundle size**: <500KB gzipped for frontend
- **Resilience**: Retry failed queries once max; circuit breaker on repeated failures

## Deployment Standards

- **Environments**: Dev (local), Staging (Azure), Production (Azure)
- **CI/CD**: GitHub Actions for automated build, test, deploy
- **Database migrations**: Automated but reviewed before production
- **Rollback**: Must be possible within 5 minutes of deployment
- **Monitoring**: Application Insights for telemetry, custom metrics for business KPIs
- **Secrets**: Azure Key Vault for all sensitive configuration

## Security Standards

- **HTTPS only** in production
- **Authentication**: Planned but not required for initial release
- **SQL injection**: Parameterized queries enforced, no string concatenation
- **XSS protection**: React default escaping + CSP headers
- **CSRF protection**: Enabled on all state-changing endpoints
- **Dependencies**: Automated vulnerability scanning in CI/CD

## Quality Standards

- **Code review**: Required for all pull requests, no self-merge
- **Linting**: ESLint (frontend), dotnet format (backend) enforced in CI
- **Testing**: Encouraged but not mandatory (pragmatic approach)
- **Accessibility**: WCAG 2.1 AA compliance for all UI components
- **Mobile support**: Responsive design, touch-friendly (min 44x44px tap targets)

## Governance

This constitution supersedes all other documents and practices. When conflicts arise, constitution wins. Feature additions must justify value against complexity. Breaking changes require migration plans and deprecation notices. Architecture decisions recorded in ADR format.

**For implementation details and tactical guidance, see** `.specify/memory/guidance.md`

---

**Version**: 4.0.0 | **Ratified**: 2025-12-04 | **Last Amended**: 2025-12-04
