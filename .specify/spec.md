# UK Vehicle Sales Analytics - Specification

## Vision

Democratize access to UK vehicle registration data through natural language queries and intelligent visualizations, enabling anyone—journalists, researchers, car buyers, policy makers—to discover trends without technical expertise.

## Target Users

- **Automotive journalists** researching market trends and writing data-driven stories
- **Policy analysts** tracking EV adoption and powertrain transitions
- **Car buyers** comparing vehicle popularity and resale value trends
- **Market researchers** analyzing competitive landscape and manufacturer performance
- **Enthusiasts** exploring historical data and making predictions

## User Stories

### Story 1: Natural Language Insights (Priority: High)

**As a user**, I want to **ask questions in plain English** about vehicle trends (e.g., "What were the top-selling EVs in 2024?" or "Compare Tesla vs BMW sales over the last 5 years") so that **I can get instant visual answers without learning query languages**.

**Acceptance Criteria**:

- User types natural language question in chat interface
- System translates query to database operations using AI
- Results displayed as appropriate chart (trend, comparison, ranking) or table
- User can refine query conversationally
- Response time <10s for complex queries

### Story 2: Time-Based Trend Analysis (Priority: High)

**As a user**, I want to **see how vehicle sales changed over time** (by year, quarter, or custom range) so that **I can understand adoption patterns, seasonality, and market shifts**.

**Acceptance Criteria**:

- Line charts show time on X-axis, sales volume on Y-axis
- Support yearly and quarterly aggregations
- Filter by Make, Model, Fuel type, or combinations
- Show year-over-year growth percentages
- Export data as CSV

### Story 3: Cross-Powertrain Comparisons (Priority: Medium)

**As a user**, I want to **compare petrol vs diesel vs electric vs hybrid adoption** over time so that **I can track the energy transition in the UK automotive market**.

**Acceptance Criteria**:

- Multi-series line chart with one line per powertrain
- Filter by date range
- Show market share percentages (not just absolute numbers)
- Highlight inflection points (when EVs surpassed diesel, etc.)

### Story 4: Manufacturer Rankings (Priority: Medium)

**As a user**, I want to **see top N manufacturers or models** by sales volume (overall or by powertrain) so that **I can identify market leaders and emerging players**.

**Acceptance Criteria**:

- Horizontal bar chart, sorted descending by volume
- Filter by year, powertrain, and limit (top 5, 10, 20)
- Show each item's market share percentage
- Click to drill down (e.g., manufacturer → models)

### Story 5: Future Predictions (Priority: Low - Not Yet Implemented)

**As a user**, I want to **see predicted future sales** based on historical trends so that **I can anticipate market direction and plan accordingly**.

**Acceptance Criteria**:

- Time-series forecasting using statistical models
- Show confidence intervals
- Allow user to adjust assumptions (growth rate, policy changes)
- Clearly label predictions as estimates

## Functional Requirements

### Data Management

- Load UK vehicle registration data from CSV files (Make, Model, Fuel, Year, Quarter, Units Sold)
- Validate data integrity on import (no negatives, reasonable year ranges)
- Support incremental updates (new quarters added as available)

### Query Interface

- Chat-based UI as primary interaction model
- Natural language → structured query translation via GPT-4o-mini
- Support filter, aggregation, grouping, sorting, top-N operations
- Provide example queries to guide users
- Display query execution time and row count

### Visualization

- Auto-detect appropriate chart type based on query result structure
- Trend charts for time-series data
- Comparison charts for categorical data
- Ranking charts for top-N lists
- Table view as fallback
- All charts responsive and accessible (WCAG AA)

### API

- `POST /api/v1/query` - Natural language query endpoint
- `POST /api/v1/query/stream` - Streaming variant for large results
- OpenAPI documentation for developers
- CORS configuration for frontend integration

## Non-Functional Requirements

### Performance

- Simple queries: <200ms response time
- Complex aggregations: <10s response time
- Page load: <2s on 3G connection
- Support 1000+ concurrent users (future scaling)

### Reliability

- 99.5% uptime target (production)
- Graceful degradation when AI service unavailable
- Retry logic for transient failures
- Circuit breaker for repeated errors

### Security

- HTTPS only in production
- Parameterized queries (no SQL injection)
- XSS protection via React + CSP headers
- Secrets in Azure Key Vault
- Future: authentication for saved queries and user preferences

### Usability

- Mobile-friendly responsive design
- Accessible to screen readers and keyboard navigation
- Clear error messages (no technical jargon)
- Example queries visible on first load

## Out of Scope (Current Release)

- User accounts and authentication
- Saved queries and bookmarks
- Email alerts for new data updates
- Advanced statistical models (regression, clustering)
- Real-time data streaming (quarterly updates sufficient)
- Vehicle specifications (engine size, price, features)
- Integration with third-party data sources

## Success Metrics

- **Engagement**: Average 5+ queries per user session
- **Performance**: 95% of queries complete within SLA (<10s)
- **Accuracy**: 90%+ of AI-translated queries execute successfully
- **Adoption**: 100+ monthly active users within 6 months
- **Satisfaction**: 4+ star rating from user feedback

## Dependencies

- UK vehicle registration dataset (quarterly CSV updates)
- Azure AI Foundry (GPT-4o-mini deployment)
- Azure App Service (hosting)
- Azure SQL Database (data storage)
- Azure Redis Cache (performance optimization)

## Technical Constraints

All technical decisions must comply with `.specify/memory/constitution.md`, including:

- React + Vite + Tailwind CSS (frontend)
- ASP.NET Core Minimal API (backend)
- SQL Server (database)
- GPT-4o-mini (AI translation)
- Redis (distributed caching)
