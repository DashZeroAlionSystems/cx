## Project Map

- **Goal**: Curate a growing, multi-language library of high-quality, well-structured code snippets with metadata for AI training and voice-driven coding.

### Structure
- `ai_snippets/snippets/`: Source code snippets across languages
- `ai_snippets/metadata.jsonl`: JSONL entries mapping snippets to training metadata
- `ai_snippets/PROJECT_MAP.md`: High-level map, scope, and roadmap
- `ai_snippets/AGENTS.md`: Background agent routing and lifecycle

### Domains
- Backend architecture (hexagonal, service/repo)
- Frontend patterns (container/presentational)
- Systems (Rust command, Bash templates)
- Data/DB (SQL idempotent migrations)

### Roadmap (short-term)
- Add Batch 2: SQL + Bash templates
- Add Batch 3: Python FastAPI with Pydantic schema reuse
- Add Batch 4: Terraform reusable module (variables/outputs)
- Add Batch 5: Kafka consumer with clean offset handling

### Voice Command Targets
- "create repo/service layer in <lang>"
- "generate DTO with validation"
- "add strategy for <behavior>"
- "scaffold hexagonal use-case"
- "write idempotent migration for <table>"

### Quality Bar
- Clear separation of concerns
- Minimal dependencies, buildable examples
- Metadata aligned to schema for training
- Lint-safe, idiomatic style per language