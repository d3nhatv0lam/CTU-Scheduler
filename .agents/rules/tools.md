---
description: "Guidelines for shell execution, RTK proxy usage, build and test verification loop, and code navigation using CodeGraph or XERJ with fallback mechanisms."
trigger: model_decision
---

# Tools, Code Search & Verification

## 1. Verification Loop (Definition of Done)

Strictly distinguish between Build and Test. Successful compilation verifies syntax and type safety; only passing tests verify correct behavioral logic.

- **Code Modification Tasks (C# / AXAML)**:
  - Build is mandatory: `rtk dotnet build --no-restore` (or `dotnet build --no-restore`).
  - Running affected tests is mandatory: `rtk dotnet test` (or `dotnet test`).
  - Zero Warnings Policy: Never leave new compiler errors or warnings behind.
- **Rule / Documentation / Configuration Tasks**:
  - Validate YAML frontmatter syntax, content consistency, and trigger definitions.
  - Do not run code build or tests unless source code was touched.
- **Honest Reporting**:
  - Report success only for steps actually executed in the session. Never claim tests passed if only build ran.
- **No Autonomous Commit in Tool Loop**:
  - The verification loop ends at running build/tests. Leave changes unstaged in the working tree for user inspection; do not proceed to run `git commit`.

## 2. RTK CLI Proxy (Optional with Transparent Fallback)

- RTK (`rtk`) is an optional tool for filtering and compressing command output before it enters the LLM context.
- **Usage**:
  - Automatically prefix supported shell commands with `rtk` when available:
    ```bash
    rtk git status
    rtk git diff
    rtk dotnet build CTUScheduler/CTUScheduler.csproj --no-restore
    rtk dotnet test tests/CTUScheduler.Tests/CTUScheduler.Tests.csproj
    ```
- **Transparent Fallback**:
  - If `rtk` is missing, encounters errors, or is unsupported in the environment, transparently fall back to standard commands (`dotnet`, `git`).
  - Never interrupt, fail, or abandon a task due to a missing RTK executable.

## 3. Codebase Navigation & Fallbacks

Never assume specialized developer tools exist across all machines:

- **CodeGraph**:
  - Primary tool for symbol graphs, call hierarchies, and inter-layer dependencies inside CTU-Scheduler.
  - Invoke via MCP tool `codegraph_explore` or shell command `codegraph explore "<query>"`.
  - *Fallback*: If `.codegraph/` is missing or the tool is unavailable, fall back to `read_file` and standard source search (`git grep`, grep/ripgrep).
- **XERJ (Code Search & Reference Coding)**:
  - Use for plain-English code searches (`xerj search --prefix ctu-scheduler "<query>"`), symbol definitions (`xerj def "<symbol>" --prefix ctu-scheduler`), and reference schedule corpora (`xerj code timetable-refs "<problem>"`).
  - Always cite specific `file:line` references when reporting findings.
  - *Fallback*: If `xerj` is uninstalled or unindexed, fall back to standard text search and official project documentation.
