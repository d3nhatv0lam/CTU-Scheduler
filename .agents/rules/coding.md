---
description: "C# implementation practices, xUnit/FluentAssertions testing conventions, Serilog logging, and performance guidelines."
trigger: glob
globs: "**/CTUScheduler*/**/*.cs, **/CTUScheduler*/**/*.axaml, **/tests/**/*.cs, **/benchmarks/**/*.cs"
---

# Coding Conventions & Quality Guidelines

> [!IMPORTANT]
> **Scope**: Applies strictly to handwritten source code. Automatically ignore compiler-generated files (`*.g.cs`, `*.AssemblyInfo.cs`), build output directories (`bin/**`, `obj/**`, `artifacts/**`), and vendor/third-party assets.

## 1. C# Implementation Standards

- **Syntax & Formatting**:
  - `.editorconfig` is the single source of truth (file-scoped namespaces, `I*` interface naming, `_camelCase` private fields, `var` usage rules, automatic unused `using` removal).
- **Types & Collections**:
  - `record`: Preferred for immutable domain models, DTOs, and state snapshots. Use standard `class` when managing mutable entity state.
  - `IReadOnlyList<T>`: Default contract for public APIs exposing read-only indexed collections.
  - `IList<T>`, `ISet<T>`: Used when the API contract genuinely requires element mutation or uniqueness guarantees.
  - `Span<T>` / `ReadOnlySpan<T>`: Adhere to `ref struct` rules under C# 13 / .NET 10. Reserved for contiguous memory processing and high-performance parsing. Permitted inside `async` methods provided variables do not cross suspension points (`await`, `yield`).
- **Async Safety**:
  - `ConfigureAwait(false)`: Apply in non-UI layers (Core, Infrastructure, AppServices) when appropriate to prevent unnecessary context capture; do not apply blindly where context synchronization is required.
  - Never use `async void` (except for UI event handler signatures mandated by the framework; must be documented with a comment).
  - Never use `.Result` or `.Wait()` — always `await`.
  - Null-safety: Avoid `!` (null-forgiving operator) unless accompanied by an explanatory comment.

## 2. Unit Testing

- **Testing Stack**: `xUnit` + `FluentAssertions` + `NSubstitute` (as configured in `tests/CTUScheduler.Tests/CTUScheduler.Tests.csproj`).
- **Naming Convention**: `MethodName_StateUnderTest_ExpectedBehavior`.
- **Structure**: Clear Arrange / Act / Assert blocks separated by blank lines.
- **Scope**: Focus unit tests on scheduling algorithms, business rules, validators, state machines, converters, and edge cases. Do not mandate boilerplate tests for trivial getters/setters or simple DTOs.
- **Isolation**: Unit tests must produce zero external side effects (no real network calls, no real filesystem mutations).

## 3. Structured Logging (Serilog)

- Inject `ILogger<T>` via constructor.
- Static message templates with named properties are mandatory:
  ```csharp
  Log.Information("Loaded {CourseCount} courses for semester {SemesterId}", count, semesterId);
  ```
- String interpolation and concatenation are strictly prohibited inside logging message templates.
- *Note*: String interpolation (`$""`) remains completely valid in regular C# logic outside logging calls.

## 4. Performance & Threading

- **CPU-Bound Work** (schedule permutation solving, scoring matrices): Run on `Task.Run()` to keep the UI thread responsive.
- **Asynchronous I/O** (HTTP network requests, file reading, PDF parsing): Use standard `async/await` without wrapping in redundant `Task.Run()`.
- **UI Thread Dispatching from Rx**: Use `.ObserveOn(RxApp.MainThreadScheduler)`. Use `Dispatcher.UIThread.InvokeAsync()` or `Post()` only when interacting directly with Avalonia controls outside observable streams.
- **Resource Management**: Dispose imaging objects (`Bitmap`) promptly. Prevent closure captures of large objects inside long-lived Rx subscriptions.

## 5. Comments & Documentation

- **Comment "Why", Not "What"**: Document non-obvious steps, magic numbers, technical workarounds, or constraints unexpressed in code.
- Mark intentional shortcuts with `// ponytail: <rationale and upgrade path>`.
- **XML Docs (`///`)**: Provide on public/internal members in `Core` and `AppServices` only when names do not convey parameter constraints, exceptions, or algorithms. Skip when self-explanatory (`IsLoading`, `Dispose()`).
