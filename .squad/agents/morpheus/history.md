# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- SimplicityTools is the Simplicity-First .NET Toolkit, built to measure architecture in economic terms.
- The initial package graph is Metrics -> Filters/Tca -> CLI, with analyzers integrated alongside the toolkit.
- Zero-config CI signal is a core product promise.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00

## Learnings

- I own architecture, package boundaries, work decomposition, and review.
- 2026-04-29T07:03:10.371-04:00: The scaffold now lives in `src/`, `tests/`, and `samples/` under `SimplicityTools.sln`, with project references enforcing `Metrics -> Filters/Tca -> Cli` and analyzer coverage kept separate in `src/SimplicityTools.Analyzers`.
- 2026-04-29T07:03:10.371-04:00: Placeholder build-safe entry points exist at `src/SimplicityTools.Cli/Program.cs`, `samples/Sample.OverEngineered/Program.cs`, and `samples/Sample.Simplified/Program.cs`, and root validation runs with `dotnet build SimplicityTools.sln` plus `dotnet test SimplicityTools.sln`.

## Post-Scaffold

📌 Scribe merged Morpheus decision into decisions.md on 2026-04-29T11:11:12Z. Scaffold work complete.
