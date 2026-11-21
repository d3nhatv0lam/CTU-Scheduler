---
description: "Guidelines for task planning, Implementation Plan Artifacts, long-running multi-phase workflow management, and persistent workspace plans in docs/plans/."
trigger: model_decision
---

# Long-Running Task Planning & Artifact Management

## 1. Two-Tier Planning Mechanism

- **Tier 1 — Implementation Plan Artifact (Interactive Review)**:
  - Applies to: Multi-layer, multi-file, architectural, high-risk, or ambiguous tasks.
  - Purpose: Present a structured implementation plan for the user to review, critique, and approve (Proceed button) before touching code.
- **Tier 2 — Workspace Plan (Durable State Tracking)**:
  - Applies to: Long-running, multi-phase tasks spanning across sessions or context boundaries.
  - Storage location: `docs/plans/<task-name>.md` inside the repository.
  - Purpose: Durable source of truth recording task progress, architectural decisions, checkpoints, and remaining work across chat sessions.

## 2. Activation Criteria

- **Simple, Clear Tasks** (typo fixes, renaming, localized single-file bug fixes): Execute directly after a one-sentence intent summary. Never create artifacts or workspace plans for trivial tasks.
- **Complex Tasks**: Create an Implementation Plan Artifact before coding. If the task spans multiple sessions, maintain a corresponding workspace plan under `docs/plans/`.

## 3. Workspace Plan Structure

A standard workspace plan under `docs/plans/` contains:
1. Objective, scope, and acceptance criteria.
2. Architectural decisions and technical rationale.
3. Ordered task list grouped into clear phases.
4. Affected files and components.
5. Risks, constraints, and open questions.
6. Verification commands (build/test) and expected outcomes.
7. Progress checklist and remaining items.

## 4. Context Management & State Drift Recovery

- **Resuming Protocol**:
  - When resuming a task from a prior session, the agent **must re-read the active workspace plan in `docs/plans/` and inspect current source code**, never relying exclusively on conversation history.
  - Maintain a concise progress summary in active context.
- **Situation A: Missing Workspace Plan**:
  - If requested to resume a task but no plan exists in `docs/plans/`:
  - Never speculate or invent past decisions.
  - Inspect git history (`git log`, `git status`, recent diffs) and related code.
  - Transparently report to the user what was determined and what remains unknown before proceeding.
- **Situation B: State Drift (Code Differs from Plan)**:
  - If the workspace plan records a state that contradicts actual source code:
  - Stop and do not blindly follow the outdated checklist.
  - Reconcile using `git diff`, current code, and build/test status.
  - Update the workspace plan to match ground reality before starting the next phase.

## 5. Phase Checkpoints

- Update the plan file upon completing each meaningful phase.
- Record actual implementation results, build/test output, deviations, and remaining tasks.
- Never mark a task complete without verifying all acceptance criteria.
