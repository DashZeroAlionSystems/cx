## Background Agents Guide

### Purpose
Coordinate background agents to continuously expand the snippet library and metadata.

### Task Queue
- File: `ai_snippets/tasks_queue.jsonl`
- Format: one JSON object per line
  - `id`: unique id
  - `type`: "snippet" | "metadata" | "docs" | "infra"
  - `priority`: 1 (highest) - 5 (lowest)
  - `status`: "pending" | "in_progress" | "review" | "done" | "blocked"
  - `assignee`: optional agent id
  - `inputs`: freeform context (prompts, links, examples)
  - `outputs`: file paths created/updated
  - `notes`: running commentary, decision logs

### Agent Protocol
- Pull highest-priority `pending` task
- Set `status` to `in_progress` with `assignee`
- Work in small, self-contained edits
- Update `outputs` with file paths
- When awaiting review, set `status` to `review` and summarize in `notes`
- If blocked, set `status` to `blocked` and note reason
- On completion, set `status` to `done`

### Quality Checklist
- Builds or compiles where applicable
- Lints clean
- Metadata JSONL updated and valid JSON per line
- Short rationale added to `notes`

### Voice Command Hooks
- "agent: add <pattern> in <lang>"
- "agent: validate metadata for <id>"
- "agent: extend docs for voice usage"