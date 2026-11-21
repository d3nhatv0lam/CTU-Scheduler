---
description: "Clean Architecture layer conventions, namespace boundaries, ReactiveUI MVVM, DI lifetimes, OperationResult error handling, and schedule logic preservation."
trigger: model_decision
---

# Architecture & Structural Constraints

## 1. Architectural Invariants (Strictly Enforced)

Enforced across all routine work. Adjust only with an explicit architectural decision, user approval, and documented rationale.

- **Business Dependency Direction**: `Desktop → Presentation → AppServices → Core ← Infrastructure`.
  - This is an architectural invariant, independent of whether layers currently reside within a single `CTUScheduler.csproj` (enforced via namespaces: `CTUScheduler.<Layer>.<Area>`) or are split into separate `.csproj` projects in the future.
  - If layers are separated into individual projects later, project references must continue enforcing this exact dependency direction.
  - Layer responsibilities remain unchanged regardless of project packaging.
- **Core Independence**:
  - `Core` contains only business domain logic, models, algorithms, and abstract interfaces.
  - Never depend on UI frameworks (Avalonia), MVVM libraries (ReactiveUI), concrete Infrastructure implementations (HTTP, PDF, Excel), or DI containers.
  - Pure domain or mathematical libraries are permitted if technically justified without violating framework independence.
- **Dependency Injection**:
  - Constructor injection is mandatory.
  - The Service Locator pattern is strictly forbidden (no static access via `App.Current.Services` or static accessors to resolve dependencies).
- **Preservation of Scheduling Logic**:
  - Protect the correctness and domain behavior of scheduling algorithms in `Core/Models/Academic/`, `IScheduleScorer`, and `ScheduleValidator`.
  - *Unrelated tasks*: Never modify or refactor scheduling algorithm files.
  - *Bug fixes*: Permitted within the minimal required scope; must include regression tests.
  - *Refactoring*: Permitted only when preserving 100% of business behavior and existing test outcomes.
  - *Algorithm / scoring / priority changes*: Require explicit user requirements and empirical verification.

## 2. Project Conventions (Default Patterns)

Applied by default; adaptable when there is a clear technical justification.

- **Composition Root**:
  - `Desktop` acts as the Composition Root and is permitted to reference `Infrastructure` to configure the DI container. The layered dependency diagram does not prohibit DI bootstrapping at the Composition Root.
  - Each layer encapsulates its DI registrations via extension methods (e.g., `AddAppServices()`, `AddInfrastructure()`).
- **DI Lifetimes**:
  - Defaults: `Singleton` for data stores (`PlannedCourseStore`, `TuitionFeeStore`, `CtuSessionStore`, `TeachingPlanStore`) and `AppState`; `Transient` for stateless Use Case services and ViewModels.
  - HTTP clients and delegating handlers follow `IHttpClientFactory` lifecycle management; do not impose standard lifetime rules onto them.
  - `Scoped` is not forbidden if a legitimate future use case arises (e.g., background worker scopes or isolated sessions).
- **Navigation**:
  - `INavigationRegionManager` is the default navigation convention; do not call `Router.Navigate` directly. This does not prevent upgrading navigation architecture when required.
- **Error Handling & OperationResult**:
  - `OperationResult` (in `Core/Models/Shared/Results/`) is the default mechanism for expected domain failures (e.g., schedule conflicts, full classes, invalid input data).
  - Do not force unexpected technical faults or catastrophic infrastructure failures into Result objects; standard Exceptions remain the appropriate mechanism for unrecoverable errors (network drops, disk I/O errors, crashes).
  - Catch specific exceptions; never use empty `catch {}` blocks or swallow exceptions silently.

## 3. Contextual Recommendations (Flexible Guidelines)

Applied flexibly based on situational context.

- **ReactiveUI & MVVM**:
  - ViewModels inherit from `ViewModelBase` or `SessionSyncViewModelBase`.
  - Use ReactiveUI Source Generators (`[Reactive]`, `[ObservableAsProperty]`).
  - Views contain no business logic (initialization only); ViewModels never reference Avalonia `Control` types directly.
  - Rx Subscriptions: Use `.DisposeWith(_disposables)` when a subscription lifecycle must bind to the ViewModel/Component lifecycle. Do not force onto self-completing observables or singletons with dedicated disposal lifecycles (`ICleanup`).
