---
name: "macos-apphost-naming"
description: "Avoid `.App` executable assembly names for runnable .NET samples on macOS"
domain: "dotnet-samples"
confidence: "medium"
source: "earned while fixing Sample.Simplified startup"
---

# Context

macOS treats `.app` names specially. For runnable .NET projects, an executable assembly name that ends with `.App` can make `dotnet run` fail even though the underlying program and tests are otherwise healthy.

# Pattern

- Prefer executable assembly names that do **not** end with `.App` for console apps and samples that developers launch with `dotnet run`
- Keep the root namespace free to keep source namespaces stable; only the output assembly name needs to change
- Add one smoke test that launches the project with `dotnet run --no-build` and asserts a zero exit code plus a recognizable success string

# Example

- `samples/Sample.Simplified/Sample.Simplified.App/Sample.Simplified.App.csproj`
  - Keep `RootNamespace` as `Sample.Simplified.App`
  - Set `AssemblyName` to `Sample.Simplified.Demo`
  - Cover startup with `samples/Sample.Simplified/Sample.Simplified.Tests/EndToEnd/StartupSmokeTests.cs`

# Anti-Pattern

- Naming a runnable sample executable `Something.App` and relying only on in-process unit tests; that misses the launch failure path that `dotnet run` exercises on macOS
