# CTU Scheduler — Project Conventions

## Stack
.NET 10 · C# latest · Avalonia 12 · ReactiveUI · MVVM · Microsoft.Extensions.DI · Serilog

---

## Agent Behavior Rules

> These rules govern HOW the agent reasons and acts — read these before the coding rules below.

### 1. Plan before coding

Before writing any code, the agent MUST:
1. **Restate** the task in one sentence to confirm understanding
2. **Identify** which layers and files will be touched
3. **List** the steps in order
4. **Flag** tradeoffs or risks (see §3)

Skip planning only for trivial single-file edits (typo fix, rename, one-liner).

### 2. Architecture integrity

- **Follow the layer rules** defined in "Architecture & Layer Rules" below — no exceptions without explicit user approval
- **Never bypass the architecture** to save time (e.g. calling Infrastructure directly from Presentation, adding logic to a View, using a service locator)
- If a task **cannot be done cleanly** within the current architecture, STOP and report the gap — present options — before proceeding
- If you discover an **architectural gap or inconsistency** while working, report it even if it's not blocking the current task
- **Prefer the boring, consistent solution** over a clever shortcut that fits poorly into the existing structure

### 3. Tradeoffs — always surface, never silently choose

When a decision has meaningful tradeoffs, present them explicitly before acting:

| Situation | What to do |
|---|---|
| Multiple valid approaches exist | List options with pros/cons; ask which to use |
| A clean solution would take significantly longer | State the time/complexity cost; offer a simpler interim |
| Adding a dependency solves the problem but adds weight | Name the dependency and ask for approval |
| A workaround exists but creates tech debt | Implement it, but leave a `// ponytail: <reason>` comment and mention it |

Do NOT silently pick the "easier" approach when a better one exists — the user decides, not the agent.

### 4. When uncertain — ask, don't assume

- If requirements are ambiguous, ask **one focused question** before starting
- If you're unsure which layer a new piece of code belongs to, ask
- If a rule in this file conflicts with an instruction the user just gave, flag the conflict and ask which takes precedence

### 5. Objectivity & evidence

**When making claims about the codebase:**
- Do not guess — use `codegraph_explore` or `xerj search` to verify before stating
- Cite source when asserting something about existing code: `file.cs:line` or `Folder/File.cs`
- If you cannot find evidence, say so explicitly: *"I didn't find an existing implementation — here's my reasoning..."*

**When recommending a technical approach:**
- Distinguish between: *"this is how the project already does it"* vs *"this is my recommendation"*
- If recommending something that differs from current project patterns, say why and acknowledge the inconsistency
- For significant technical decisions (architecture change, new dependency, algorithm choice), briefly state the reasoning — not just the conclusion

**What this rule does NOT require:**
- Citations for well-known language/framework facts (e.g. "async void can't be awaited")
- Justification for every minor stylistic choice that follows existing project patterns

### 6. Scope discipline

- Only touch files directly related to the task
- Do not refactor unrelated code while implementing a feature ("while I'm here…" changes go into a separate proposal)
- If you notice something worth fixing but out of scope, mention it at the end: *"Side note: I noticed X — worth a separate task?"*

### 7. Verification before completion

Before declaring any coding task done:
- Run `rtk dotnet build` (or `dotnet build`) to verify zero compilation errors or new warnings.
- Run relevant unit tests (`rtk dotnet test`) when touching tests or core logic.
- Never leave broken builds or unverified code behind.

---

## Architecture & Layer Rules

- Dependency flow (one-way): `Desktop → Presentation → AppServices → Core ← Infrastructure`
- `Core` MUST NOT reference Avalonia, ReactiveUI, DI containers, or HTTP clients — BCL only
- `Infrastructure` is I/O only — no business logic
- `ViewModel` MUST NOT reference Avalonia `Control` types directly
- `View` code-behind is init-only — no logic, no service calls
- `View` MUST NOT call services directly — only through ViewModel

---

## Naming

- Class suffix rules (always apply):
  - ViewModel → `*ViewModel`
  - Service → `*Service`, interface → `I*Service`
  - State store → `*Store`
  - Repository → `*Repository`
  - Extension class → `*Extensions`
  - Helper class → `*Helper`
- Interface always prefixed with `I`
- Private field: `_camelCase`
- Async method: suffix `Async`
- Enum member: `PascalCase` (not ALL_CAPS)
- Namespace must match folder path: `CTUScheduler.<Layer>.<Area>.<SubArea>`

---

## C# Rules

- Syntax & naming: Follow `.editorconfig` (file-scoped namespaces, `var` usage, naming conventions)
- Use `record` for immutable domain models
- Use `IReadOnlyList<T>` (not `List<T>`) in public APIs
- Never use `!` (null-forgiving operator) without a comment explaining why it's safe
- Use `ConfigureAwait(false)` in Core and Infrastructure (non-UI) async methods
- Never use `async void` unless required by an event handler signature or working around a third-party UI framework limitation (document the reason with a comment)
- Never use `.Result` or `.Wait()` — always `await`
- XML doc required on all `public`/`internal` members in `Core` and `AppServices`

---

## ReactiveUI / MVVM

- All ViewModels inherit from `ViewModelBase`
- Use `[Reactive]` source generator for reactive properties
- Use `[ObservableAsProperty]` for computed/derived properties
- Prefer `canExecute` observable on `ReactiveCommand` when the command has a meaningful enabled/disabled state
- Subscribe to `ThrownExceptions` on commands that perform async or error-prone work; pure UI commands (focus, scroll) may skip this
- All Rx subscriptions MUST be `.DisposeWith(_disposables)`
- Navigate via `INavigationRegionManager` — never call `Router.Navigate` directly
- Region names are constants in `Presentation/Shared/Models/Identifiers/`
- Use `IActivatableViewModel` + `WhenActivated` when subscriptions must be tied to view activation/deactivation; use `CompositeDisposable` alone for simpler cases
- Implement `IDisposable` with `CompositeDisposable` for ViewModel lifetime management

---

## Avalonia / AXAML

- Compiled bindings are enabled globally — always declare `x:DataType`
- Never hardcode colors — use `StaticResource`/`DynamicResource` from Semi.Avalonia tokens
- Never use `FindControl<T>()` in business logic
- Complex styles go in a separate `<Style>` block or dedicated `.axaml` file, not inline
- Custom shared controls go in `Presentation/Shared/Controls/`

---

## Dependency Injection

- Each layer registers its own DI via extension method (e.g. `AddAppServices()`, `AddInfrastructure()`)
- Lifetimes: Singleton = state stores/settings · Scoped = session context · Transient = ViewModels
- Never use service locator (`App.Current.Services` or static accessors)
- Always inject via constructor

---

## Logging (Serilog)

- Inject `ILogger<T>` via constructor
- Always use structured logging — message template with named properties, never string interpolation or concatenation
- Log levels: Debug=dev detail · Info=normal events · Warning=unexpected but non-fatal · Error=handled exception · Critical=app cannot continue

---

## Error Handling

- Use the project's `OperationResult` type (in `Core/Models/Shared/Results/`) for expected failures — do not introduce external Result libraries
- Catch specific exception types (`HttpRequestException`, `OperationCanceledException`, `InvalidDataException`) — avoid broad `catch (Exception)` outside top-level handlers
- Never use empty `catch {}` blocks — log at minimum
- Do not swallow exceptions silently

---

## Testing

- Test name format: `MethodName_StateUnderTest_ExpectedBehavior`
- Structure: Arrange / Act / Assert (separated by blank lines)
- Test only public API — never test private methods directly
- Tests must have no side effects (no real I/O, no real network)
- All `public` methods in `Core` must have unit tests

---

## Comments & Documentation

### XML doc (`///`)

Write XML doc on `public`/`internal` members in `Core` and `AppServices` only when the name alone is insufficient — non-obvious behavior, parameter constraints, return semantics, thrown exceptions, algorithms, units, or value ranges.

Skip when the name already says everything (`IsLoading`, `Dispose()`). Never restate the name as a summary.

Use standard .NET tags: `<summary>`, `<param>`, `<returns>`, `<exception>`, `<remarks>`. Mixed Vietnamese/English is acceptable — Vietnamese for domain/business concepts, English for technical terms.

### Inline comments (`//`)

Comment the **why**, not the **what** — if the code itself is readable, don't narrate it. Write a comment when: the step is non-obvious, there's a magic number, a workaround has a reason, or a constraint can't be expressed in code.

Mark intentional shortcuts with `// ponytail: <reason>`.

### What NOT to comment

- Commented-out code → delete it, git history keeps it
- `// TODO` with no tracking → open a GitHub issue, reference it as `// TODO: #42`
- `// end of method` or section dividers in short files

---

## Git

- Commit format (Conventional Commits): `<type>(<scope>): <subject>`
- Types: `feat` · `fix` · `refactor` · `perf` · `test` · `docs` · `chore` · `style`
- Scopes: `core` · `infra` · `appservices` · `presentation` · `scheduling` · `ui` · `ci` · `deps`
- Branches: `feature/` · `fix/` · `refactor/` · `chore/` · `docs/`
- PR must pass CI, have a description, and have at least 1 reviewer approval

---

## Performance

- Heavy computation runs on `Task.Run()` — never block the UI thread
- Always use `Dispatcher.UIThread.InvokeAsync()` for UI updates from background threads
- Always Dispose `Bitmap`/imaging objects after use
- Prefer `IHttpClientFactory` for injected HTTP clients; a `static readonly HttpClient` with a custom `SocketsHttpHandler` (long-lived, singleton-scoped) is also acceptable — never `new HttpClient()` per-request
- Avoid closure captures of large objects in Rx chains
