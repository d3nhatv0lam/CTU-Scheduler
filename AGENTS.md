# CTU Scheduler — Agent Core Guidelines

## 1. Project Overview & Identity
- **Repository**: CTU-Scheduler (Desktop application for Can Tho University course scheduling and timetable generation).
- **Core Stack**: .NET 10 · C# latest · Avalonia 12 · ReactiveUI · Microsoft.Extensions.DependencyInjection · Serilog.
- **Solution Layout**: Single primary project `CTUScheduler` (divided by namespaces: `Core`, `Infrastructure`, `AppServices`, `Presentation`), `CTUScheduler.Desktop` (bootstrapper executable), and `tests/CTUScheduler.Tests`.

## 2. Decision-Making & Behavioral Policy
- **Autonomous Technical Decisions**:
  - Autonomously choose standard, low-risk technical solutions following existing project patterns.
  - Ask the user only when: requirements are ambiguous, a major architectural tradeoff is involved, a new dependency is required, or a breaking change is proposed.
- **Scope Discipline**:
  - Modify only files directly related to the assigned task.
  - Never perform unrelated refactoring. Note out-of-scope improvements at the end of the response.
- **Evidence & Objectivity**:
  - Verify codebase assertions using CodeGraph, XERJ, or direct file reads before stating facts.
  - Always cite specific sources as `file:line` or `Folder/File.cs`. Never invent or assume implementations.

## 3. Specialized Rules Organization
Detailed rules are modularized under `.agents/rules/` and activated conditionally via YAML frontmatter triggers:

- [.agents/rules/architecture.md](.agents/rules/architecture.md): Layer dependency invariants (`Desktop → Presentation → AppServices → Core ← Infrastructure`), Core independence, Composition Root, ReactiveUI MVVM, DI lifetimes, `OperationResult`, and schedule algorithm preservation.
- [.agents/rules/coding.md](.agents/rules/coding.md): C# standards (following `.editorconfig`), C# 13 `Span<T>` handling, xUnit/FluentAssertions/NSubstitute testing conventions, Serilog structured logging, and performance threading.
- [.agents/rules/tools.md](.agents/rules/tools.md): Verification Loop (Definition of Done: build + test), RTK CLI proxy (optional with transparent fallback), and CodeGraph / XERJ navigation with standard fallbacks.
- [.agents/rules/planning.md](.agents/rules/planning.md): Complex task planning, Implementation Plan Artifacts, persistent workspace plans in `docs/plans/`, and state drift recovery protocols.
- [.agents/rules/communication.md](.agents/rules/communication.md): Direct, anti-AI communication style, no filler, and natural language matching (Vietnamese by default when prompted in Vietnamese).
