#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")"/.. && pwd)"
cd "$ROOT_DIR"

# 1) Start infra (db + api) in background
if command -v docker >/dev/null 2>&1; then
	echo "[start] Bringing up docker compose stack..."
	docker compose up -d | cat
else
	echo "[warn] Docker not found; skipping compose up"
fi

# 2) Ensure Python deps for Agents API
if command -v python3 >/dev/null 2>&1; then
	echo "[start] Setting up Agents API venv..."
	python3 -m venv .venv
	source .venv/bin/activate
	pip install --upgrade pip >/dev/null
	pip install -r agents_api/requirements.txt >/dev/null
else
	echo "[error] python3 not found"
	exit 1
fi

# 3) Start Agents API
export TASKS_FILE="$ROOT_DIR/tasks_queue.jsonl"
echo "[start] Starting Agents API on :8000"
uvicorn agents_api.main:app --host 0.0.0.0 --port 8000 --reload | cat