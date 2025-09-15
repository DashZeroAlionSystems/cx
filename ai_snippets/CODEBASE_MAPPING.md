# Codebase Mapping for Snippet Extraction

## Current Snippets (ai_snippets/snippets/)
- `go_hexagonal_example.go` - Go ports/adapters pattern
- `rust_command_pattern.rs` - Rust command pattern with queue
- `react_container_presentational.tsx` - React container/presentational split
- `csharp_aspnet_cx_api.cs` - C# ASP.NET Core minimal API with Postgres
- `react_cx_api_client.ts` - React client with hooks for CX API
- `sql_cx_users.sql` - Postgres migration for CX users
- `sql_idempotent_migration.sql` - Idempotent SQL migration template

## CX Container Server Domain (1220+ C# files)
**Location**: `src/CX.Container/CX.Container.Server/Domain/`

### High-Value Snippet Candidates:

#### 1. Domain-Driven Design Patterns
- **Entities**: `Projects/Project.cs`, `Messages/Message.cs`, `Profiles/Profile.cs`
- **Value Objects**: `Percentages/Percent.cs`
- **Domain Events**: `*/DomainEvents/*.cs` (ProjectCreated, MessageUpdated, etc.)
- **Repositories**: `*/Services/*Repository.cs`
- **Features/Commands**: `*/Features/*.cs` (GetProject, AddProject, UpdateMessage, etc.)

#### 2. CQRS & Clean Architecture
- **DTOs**: `*/Dtos/*.cs` (ProjectDto, MessageDto, etc.)
- **Mappers**: `*/Mappings/*Mapper.cs`
- **Models**: `*/Models/*.cs` (ProjectForCreation, MessageForUpdate, etc.)

#### 3. API Controllers & Middleware
- **Controllers**: `Controllers/*.cs`
- **Middleware**: `Middleware/*.cs`
- **Configuration**: `Startup.cs`, `Program.cs`

### Snippet Extraction Tasks:
1. **DDD Entity Pattern** - Extract Project.cs as example
2. **Domain Event Pattern** - Extract MessageCreated.cs
3. **Repository Pattern** - Extract ProjectRepository.cs
4. **CQRS Command/Query** - Extract GetProject.cs and AddProject.cs
5. **DTO Mapping** - Extract ProjectMapper.cs
6. **API Controller** - Extract ProjectsController.cs

## Python Utilities (11 files)
**Location**: `src/CX.Container/Py/` and `src/CX.Container/CX.Container.Server/Py/`

### Snippet Candidates:
- `docxtotext.py` - Document conversion utility
- `anythingtomarkdown.py` - Universal markdown converter
- `pdftojpg.py` - PDF to image conversion
- `docxtopdf.py` - Document format conversion
- `pdfplumber_console.py` - PDF text extraction

### Snippet Extraction Tasks:
1. **Document Processing Pipeline** - Extract docxtotext.py
2. **File Format Converter** - Extract anythingtomarkdown.py
3. **PDF Processing** - Extract pdfplumber_console.py

## React Workflow Builder (5 files)
**Location**: `src/CX.Container/CX.Container.Server/React/WorkflowBuilder/`

### Snippet Candidates:
- `core.js` - Core workflow logic
- `ui.js` - UI components
- `io.js` - Input/output handling
- `main.js` - Main application entry
- `examples.js` - Example workflows

### Snippet Extraction Tasks:
1. **Workflow Engine** - Extract core.js
2. **React Component Architecture** - Extract ui.js
3. **Data Flow Pattern** - Extract io.js

## Database Schemas (7 files)
**Location**: Various client domains

### Snippet Candidates:
- `v2.8.sql` - Versioned schema migration
- `schema.sql` - Base schema definition
- `SakeNetwerk_*.sql` - Client-specific schemas

### Snippet Extraction Tasks:
1. **Versioned Schema Migration** - Extract v2.8.sql
2. **Multi-tenant Schema** - Extract SakeNetwerk patterns

## Infrastructure & DevOps (18+ files)
**Location**: `ci/`, `charts/`, `docker-compose.yml`

### Snippet Candidates:
- Azure Pipelines templates
- Helm charts
- Docker configurations
- GitVersion configuration

### Snippet Extraction Tasks:
1. **CI/CD Pipeline** - Extract Azure Pipelines template
2. **Kubernetes Deployment** - Extract Helm chart
3. **Docker Multi-stage Build** - Extract Dockerfile patterns

## Priority Extraction Queue

### High Priority (Core Patterns)
1. DDD Entity with Domain Events
2. CQRS Command/Query separation
3. Repository pattern with dependency injection
4. API Controller with validation
5. Document processing pipeline

### Medium Priority (Architecture)
1. Clean Architecture layers
2. CQRS with MediatR
3. Domain service patterns
4. API versioning
5. Error handling middleware

### Low Priority (Utilities)
1. File conversion utilities
2. Configuration management
3. Logging patterns
4. Testing utilities

## Voice Command Mapping
- "extract DDD entity" → Project.cs
- "extract repository pattern" → ProjectRepository.cs
- "extract CQRS command" → AddProject.cs
- "extract API controller" → ProjectsController.cs
- "extract domain event" → ProjectCreated.cs
- "extract document processor" → docxtotext.py
- "extract workflow engine" → core.js