### Long-Running Task Planning & Artifact Management

**1. When to create a plan**

- Simple, localized tasks: execute directly without unnecessary planning.
- Multi-file, multi-layer, architectural, risky, or ambiguous tasks: create a structured Implementation Plan Artifact before implementation.
- Use Antigravity's native planning workflow (`/plan`) when appropriate.

**2. When to persist a workspace plan**

For tasks that are expected to span multiple phases, sessions, or substantial context usage, maintain a persistent Markdown plan inside the repository, preferably under `docs/plans/`.

The workspace plan is the durable source of task progress and decisions; the Artifact is the reviewable deliverable for the user.

**3. Plan structure**

Include:
- Objective, scope, and acceptance criteria.
- Architectural decisions and rationale.
- Ordered tasks and dependencies.
- Relevant files and affected components.
- Risks, constraints, and unresolved questions.
- Verification commands and expected results.
- Progress checklist and remaining work.

**4. Context management**

- Never rely exclusively on conversation history for long-running tasks.
- Before continuing a paused or resumed task, read the current workspace plan and inspect the relevant source files.
- Retrieve only the sections and files needed for the current phase instead of loading the entire plan repeatedly.
- Maintain a concise progress summary in the active conversation context.
- If context is insufficient, re-read the relevant artifact or workspace plan rather than guessing or reconstructing missing decisions.

**5. Progress updates**

- Update task status after completing each meaningful phase.
- Record actual implementation results, test/build outcomes, deviations, and remaining tasks.
- Never mark a task complete without verifying its acceptance criteria.
- Preserve completed decisions and history; do not silently discard unresolved issues.

**6. Avoid unnecessary artifacts**

Do not create persistent plans or artifacts for trivial tasks, one-line fixes, or straightforward questions. Use the smallest planning mechanism appropriate to the task.