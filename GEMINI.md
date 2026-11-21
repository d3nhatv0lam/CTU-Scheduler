# CTU Scheduler — Gemini Entry Point

This project uses [AGENTS.md](AGENTS.md) as its canonical agent instructions entry point.

## Navigation & Tools
- **Architecture & Conventions**: Adhere strictly to the Clean Architecture and coding rules configured in [.agents/rules/architecture.md](.agents/rules/architecture.md) and [.agents/rules/coding.md](.agents/rules/coding.md).
- **CodeGraph**: Primary tool for tracing call paths, symbol definitions, and inter-layer dependencies inside CTU-Scheduler (`codegraph_explore` MCP tool or `codegraph explore` shell command).
- **XERJ**: For plain-English search across the repository (`xerj search --prefix ctu-scheduler "<query>"`) and external reference code (`xerj code timetable-refs "<problem>"`).
- **Verification**: Run `rtk dotnet build` and relevant tests before completing tasks. See [.agents/rules/tools.md](.agents/rules/tools.md) for full tool and fallback guidelines.
