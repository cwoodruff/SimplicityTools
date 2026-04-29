# Squad Team

> Simplicity-First .NET Toolkit squad for SimplicityTools

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. Does not generate domain artifacts. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Morpheus | Lead | `.squad/agents/morpheus/charter.md` | ✅ Active |
| Trinity | Core Dev | `.squad/agents/trinity/charter.md` | ✅ Active |
| Switch | Analyzer Dev | `.squad/agents/switch/charter.md` | ✅ Active |
| Tank | Tester | `.squad/agents/tank/charter.md` | ✅ Active |
| Link | DevRel | `.squad/agents/link/charter.md` | ✅ Active |
| Scribe | Session Logger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | `.squad/agents/ralph/charter.md` | 🔄 Monitor |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage additions and isolated test fixes
- Boilerplate, scaffolding, and small focused features
- Documentation cleanup and README updates

**🟡 Needs review — route to @copilot but keep human squad review in the loop:**
- Medium features with clear acceptance criteria
- Refactoring work backed by tests
- Routine package and dependency maintenance

**🔴 Not suitable — route to squad member instead:**
- Architecture decisions and package boundaries
- Cross-package coordination work
- Roslyn analyzer policy and diagnostic design
- Performance-sensitive or economics-model calibration work

## Project Context

- **Owner:** Chris Woody Woodruff
- **Stack:** C# 14, .NET 10, Roslyn, MSBuild, NuGet, CLI tooling
- **Description:** Open-source measurement and enforcement toolkit that maps architectural complexity to Simplicity-First and Total Cost of Architecture signals.
- **Created:** 2026-04-29T06:47:51.656-04:00
