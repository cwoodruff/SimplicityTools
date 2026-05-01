---
name: "dotnet-sample-apphost"
description: "Keep repo-local .NET sample apps zero-config by avoiding native apphosts when macOS integrity enforcement can kill them"
domain: "dotnet-samples"
confidence: "medium"
source: "earned"
---

## Context
Use this when a repo-local .NET sample console app runs correctly via `dotnet exec <app>.dll` but dies immediately when launched through `dotnet run` or the generated native executable on macOS.

## Patterns
1. Prove whether the failure is in app logic or host packaging by comparing the generated native apphost with `dotnet exec` on the built DLL.
2. If the DLL path works and the native apphost is the only failing path, prefer `<UseAppHost>false</UseAppHost>` in the sample app project.
3. Keep this scoped to samples and local demos unless there is a shipping requirement for a native launcher.
4. Validate with build + run + test so the zero-config first-run promise stays intact.

## Examples
- `samples/Sample.Simplified/Sample.Simplified.App/Sample.Simplified.App.csproj`

## Anti-Patterns
- Chasing application code changes when the process is being killed before `Main()`.
- Keeping a native apphost for a sample when the managed host path is the stable cross-machine option.
