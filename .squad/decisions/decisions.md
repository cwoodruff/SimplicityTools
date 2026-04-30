# Team Decisions Log

## Link Decision — Budget dimension mapping

- **Date:** 2026-04-29T21:22:50.867-04:00
- **Issue:** #14 — CLI budget command
- **Decision:** Map the four Complexity Budget dimensions to the existing `simplicity.json` filter thresholds so the command stays zero-config and immediately honors team overrides. Cognitive Load uses `maxOnboardingHours`, Operational Surface uses `prematureAbstractionRatioTarget`, Change Safety uses `maxMethodComplexity`, and Discoverability uses `primaryPathRatioTarget` as a minimum target.
- **Why:** These four thresholds already exist, are documented, and line up with the budget dimensions without expanding the configuration schema mid-sprint. This keeps the first-run experience clear: teams can tune one config file and see budget output change right away.
