# Skill: Repository-Level Copilot Documentation

**Date discovered:** 2026-05-28  
**Context:** SimplicityTools project  
**Owner:** Morpheus  
**Status:** Validated

## Problem

AI assistant sessions (Copilot, Claude, etc.) spend 20–30% of onboarding time reverse-engineering repository architecture, build commands, and conventions when this information is scattered across multiple files (workflows, project configs, READMEs, team decisions).

## Pattern: The `.github/copilot-instructions.md` Contract

Create a single, centralized reference document at `.github/copilot-instructions.md` that covers:

### Mandatory Sections

1. **Overview**
   - One-sentence description of what the project does
   - Tech stack and release strategy
   - Key architectural principle(s)

2. **High-Level Architecture**
   - Package/module graph (diagrams or ASCII art)
   - Dependency direction
   - Release group organization
   - Test structure

3. **Build, Test, and Lint Commands**
   - Copy-paste ready commands (no explanation needed)
   - Grouped by context: local dev, validation, CI/CD
   - Include timeouts where relevant ("CLI tests take 5+ minutes")

4. **Key Conventions and Patterns**
   - Version source of truth and derivation rules
   - Release tag formats and process outline
   - Metadata standards (licensing, icons, tags)
   - Test organization and coverage expectations
   - Any heuristics or inference rules (e.g., primary-path detection)

5. **Repository-Specific Constraints**
   - Platform quirks (macOS apphost signing, CI matrix limitations)
   - Architectural boundaries that are non-negotiable
   - Zero-config or minimal-config promises

6. **Common Workflows**
   - "I need to add a new metric"
   - "I need to add an analyzer"
   - "I need to release a package"
   - Checklist format works well

7. **Troubleshooting**
   - Predictable failures and quick fixes
   - Avoid encyclopedic; keep to 3–5 common issues

8. **Related Documentation**
   - Links to deeper docs (full API reference, configuration schemas)
   - Links to team decisions (`.squad/decisions.md`)

### Optional Sections

- Examples of common patterns in the codebase
- Deprecated practices to avoid
- Performance considerations
- Deployment or infrastructure notes

## How to Write It

- **Audience:** Next Copilot session or new junior contributor
- **Tone:** Direct, prescriptive, assume no prior context
- **Length:** 500–1000 lines; dense with facts, light on narrative
- **Structure:** Markdown with clear section headers; table format for reference data
- **Commands:** Every command should be copy-paste ready (not pseudocode)
- **Links:** Reference actual file paths and line ranges where applicable

## Validation Checklist

Before publishing:

- [ ] **Architecture section captures the full dependency graph** — Can a reader trace what depends on what?
- [ ] **Build commands actually work** — Copy-paste each one locally and verify success
- [ ] **Conventions section covers non-obvious facts** — Does it explain why the repo is organized the way it is?
- [ ] **Constraints are explicit** — Are platform quirks, versioning rules, and architectural boundaries clearly named?
- [ ] **Workflows are actionable** — Can a Copilot session follow them end-to-end without asking clarifying questions?
- [ ] **Troubleshooting addresses real failures** — Are these things that actually happened, not hypothetical?

## Example Sections

### Architecture Section (Good)
```markdown
### Package Dependency Graph

Metrics (core, no external deps)
  ├→ Filters (depends on Metrics)
  │  └→ Tca (depends on Filters, Metrics)
  └→ Cli (depends on Metrics, Filters, Tca)

Analyzers (no deps; independent release)

**Key constraint:** Metrics, Filters, and Tca version together.
```

### Build Commands Section (Good)
```markdown
**Run all tests:**
dotnet test SimplicityTools.sln --nologo --no-build --verbosity minimal

**Run CLI tests only (slower; 5+ minutes):**
dotnet test tests/SimplicityTools.Cli.Tests/SimplicityTools.Cli.Tests.csproj
```

### Convention Section (Good)
```markdown
**File:** Directory.Build.props
- **Property:** SimplicityToolsReleaseVersion (currently 0.4.0)
- **What it controls:**
  - Local package defaults: 0.4.0-local
  - CI validation: 0.4.0-ci.<run-number>
  - Release baseline for manual workflow dispatch
  - Docs-site footer version display

**Never hardcode versions in individual project files.**
```

## Why This Works

1. **Encodes non-obvious knowledge** — Architectural decisions and constraints become explicit rather than tribal
2. **Surfaces through search** — AI assistants and contributors can find it immediately
3. **Couples architecture to documentation** — When the architecture changes, the doc is right there demanding updates
4. **Reduces review cycles** — Lead can point to specific sections instead of explaining verbally
5. **Accelerates onboarding** — New Copilot sessions or junior contributors don't waste time reverse-engineering

## When to Update

- After any architectural decision (new release group, versioning change, build process update)
- After any constraint change (platform support, CI limits, zero-config promise revision)
- After discovering that Copilot sessions repeatedly ask the same question (add it to troubleshooting)
- Quarterly health check: run commands, verify they still work

## Anti-Patterns to Avoid

❌ **Don't:** Make it a comprehensive project history  
✅ **Do:** Focus on facts that affect current work

❌ **Don't:** Include narrative explanation of why decisions were made  
✅ **Do:** State the decision, link to `.squad/decisions.md` if deeper rationale matters

❌ **Don't:** List every file in the repo  
✅ **Do:** List only key files that are essential for understanding architecture

❌ **Don't:** Copy documentation from README or full docs  
✅ **Do:** Link to those docs and summarize the parts relevant to Copilot work

---

## References

- **SimplicityTools implementation:** `.github/copilot-instructions.md` (600+ lines, covers five packages, release groups, test patterns, build commands)
- **Related:** `.squad/decisions.md` (captures the "why" for architectural decisions)

