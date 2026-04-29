# Project Context

- **Owner:** Chris Woody Woodruff
- **Project:** SimplicityTools
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Created:** 2026-04-29T06:47:51.656-04:00

## Core Context

- The toolkit is meant to teach Simplicity-First through practical output and clear examples.
- The global tool is `dotnet-simplicity`.
- Zero-config first-run experience is a product requirement, not just a docs goal.

## Recent Updates

📌 Team hired on 2026-04-29T06:47:51.656-04:00

## Learnings

- My initial focus is CLI experience, docs, and sample-driven guidance.

### 2026-04-29T07:32:23.826-04:00: HTML Report Design

**UX Decision:** The HTML report uses dark theme (#0D0D0D background) with brand red accent (#E31B23) to create professional visual hierarchy while remaining self-contained. All CSS is embedded; no external assets or CDN dependencies. This ensures CI/CD-safe generation and works offline.

**Metric Presentation:** Report includes six main sections (Executive Summary, Filter Verdicts, Metric Detail, Complexity Budget, Trend Analysis, Appendix). Verdicts present health status as badges with contextual colors (green/good, orange/warn, red/critical) so developers quickly understand where to focus.

**Simplicity Score:** Calculated from four penalty factors: premature abstraction ratio, unused dependencies, method complexity, and low primary path coverage. Score guides teams toward actionable improvement areas.

