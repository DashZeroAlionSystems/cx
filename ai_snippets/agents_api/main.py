from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import List, Optional, Literal
from pathlib import Path
import json

TASKS_FILE = Path(__file__).resolve().parents[1] / "tasks_queue.jsonl"

app = FastAPI(title="Background Agents API", version="0.1.0")

class Task(BaseModel):
	id: str
	type: Literal["snippet", "metadata", "docs", "infra"]
	priority: int
	status: Literal["pending", "in_progress", "review", "done", "blocked"]
	assignee: Optional[str] = None
	inputs: dict
	outputs: List[str] = []
	notes: str = ""

class ClaimRequest(BaseModel):
	assignee: str

class UpdateRequest(BaseModel):
	status: Optional[Task.model_fields['status'].annotation] = None
	outputs: Optional[List[str]] = None
	notes: Optional[str] = None
	assignee: Optional[str] = None


def read_tasks() -> List[Task]:
	if not TASKS_FILE.exists():
		return []
	items: List[Task] = []
	with TASKS_FILE.open() as f:
		for line in f:
			line = line.strip()
			if not line:
				continue
			items.append(Task(**json.loads(line)))
	return items


def write_tasks(tasks: List[Task]) -> None:
	with TASKS_FILE.open("w") as f:
		for t in tasks:
			f.write(json.dumps(t.model_dump()) + "\n")


@app.get("/tasks", response_model=List[Task])
async def list_tasks(status: Optional[str] = None, type: Optional[str] = None):
	tasks = read_tasks()
	if status:
		tasks = [t for t in tasks if t.status == status]
	if type:
		tasks = [t for t in tasks if t.type == type]
	# sort by priority then id for stability
	return sorted(tasks, key=lambda t: (t.priority, t.id))


@app.post("/tasks/{task_id}/claim", response_model=Task)
async def claim_task(task_id: str, body: ClaimRequest):
	tasks = read_tasks()
	for t in tasks:
		if t.id == task_id:
			if t.status not in ("pending", "blocked", "review"):
				raise HTTPException(409, detail="Task not claimable")
			t.assignee = body.assignee
			t.status = "in_progress"
			write_tasks(tasks)
			return t
	raise HTTPException(404, detail="Task not found")


@app.patch("/tasks/{task_id}", response_model=Task)
async def update_task(task_id: str, body: UpdateRequest):
	tasks = read_tasks()
	for idx, t in enumerate(tasks):
		if t.id == task_id:
			data = t.model_dump()
			payload = body.model_dump(exclude_unset=True)
			data.update({k: v for k, v in payload.items() if v is not None})
			updated = Task(**data)
			tasks[idx] = updated
			write_tasks(tasks)
			return updated
	raise HTTPException(404, detail="Task not found")


class FollowUpResponse(BaseModel):
	created: bool
	task: Task


@app.post("/tasks/{task_id}/followup", response_model=FollowUpResponse)
async def create_followup(task_id: str):
	tasks = read_tasks()
	original = None
	for t in tasks:
		if t.id == task_id:
			original = t
			break
	if original is None:
		raise HTTPException(404, detail="Task not found")

	# Define ready-to-open semantics
	if original.status not in ("pending", "review"):
		raise HTTPException(409, detail="Task not ready to open")

	followup_id = f"{original.id}-continue"
	for t in tasks:
		if t.id == followup_id:
			return FollowUpResponse(created=False, task=t)

	followup = Task(
		id=followup_id,
		type=original.type,
		priority=max(1, original.priority),
		status="pending",
		assignee=None,
		inputs={"prompt": f"Follow-up: continue work for {original.id}"},
		outputs=[],
		notes="auto-generated follow-up based on ready-to-open state"
	)
	tasks.append(followup)
	write_tasks(tasks)
	return FollowUpResponse(created=True, task=followup)