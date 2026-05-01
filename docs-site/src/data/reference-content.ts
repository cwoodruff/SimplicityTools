export type RelatedLink = {
  eyebrow: string;
  label: string;
  href: string;
  description: string;
  external?: boolean;
};

export type CommandExample = {
  label: string;
  description: string;
  code: string;
  lang: string;
};

export type CommandDetail = {
  slug: string;
  title: string;
  summary: string;
  purpose: string;
  bestFor: string;
  writesFiles: string;
  exitBehavior: string;
  command: string;
  outputIntro: string;
  outputHighlights: string[];
  sampleOutput?: string;
  operationalNotes: string[];
  examples: CommandExample[];
  relatedLinks: RelatedLink[];
};

export type FilterSignal = {
  name: string;
  description: string;
};

export type FilterDetail = {
  slug: string;
  title: string;
  summary: string;
  question: string;
  showsUpIn: string;
  passThreshold: string;
  primaryUse: string;
  signals: FilterSignal[];
  interpretation: string;
  interpretationBullets: string[];
  nextSteps: string[];
  relatedLinks: RelatedLink[];
};

export type AnalyzerExample = {
  code: string;
  lang: string;
  caption: string;
};

export type AnalyzerDetail = {
  slug: string;
  id: string;
  title: string;
  summary: string;
  category: string;
  severity: string;
  codeFix: boolean;
  whenItFiresSummary: string;
  ruleMessage: string;
  whyItMatters: string;
  whenItFires: string[];
  badExample: AnalyzerExample;
  goodExample: AnalyzerExample;
  codeFixSummary?: string;
  codeFixNotes: string[];
  sourceHref: string;
  sourceLabel: string;
  codeFixHref?: string;
  codeFixLabel?: string;
  issueHref: string;
  issueNumber: number;
  relatedLinks: RelatedLink[];
};

const issue55 = 'https://github.com/cwoodruff/SimplicityTools/issues/55';
const repoBase = 'https://github.com/cwoodruff/SimplicityTools/blob/sprint/8-astro-website';

export const commandDetails: Record<string, CommandDetail> = {
  analyze: {
    slug: 'analyze',
    title: 'analyze',
    summary: 'Collect a current snapshot of a solution without writing files so teams can learn the shape before they automate anything.',
    purpose: 'Use analyze for the first read. It prints the current solution shape, complexity, and primary-path signal without creating artifacts.',
    bestFor: 'First-run learning, local spot checks, and CI jobs that only need the current picture.',
    writesFiles: 'No. It prints a summary to stdout only.',
    exitBehavior: 'Returns 0 on success. Returns 1 for usage errors or analysis failures.',
    command: 'dotnet simplicity analyze path/to/YourSolution.sln',
    outputIntro: 'The summary is intentionally compact, but it still covers the core structural, code, and onboarding signals.',
    outputHighlights: [
      'Total projects and countable source files.',
      'Primary-path file count plus abstraction layer count.',
      'Single-implementation interfaces, external dependencies, and unused dependencies.',
      'Average method complexity and estimated onboarding time.',
      'A warning instead of a hard failure when simplicity.json is missing.'
    ],
    sampleOutput: `Warning: simplicity.json was not found in '/path/to/solution'. Using built-in defaults for TCA inputs and filter thresholds.\nSimplicity Snapshot (2026-05-01)\n----------------------------------------\nProjects: 2\nTotal files: 23\nPrimary path files: 5\nAbstraction layers: 1\nSingle-impl interfaces: 0\nExternal deps: 0 (0 unused)\nAvg complexity: 1.4\nEst. onboarding: 0h`,
    operationalNotes: [
      'The structural project count includes all C# projects, but semantic and primary-path passes skip test projects for abstraction, dependency, and primary-path heuristics.',
      'Primary-path detection prefers explicit [PrimaryPath] annotations and falls back to Controllers, Endpoints, Handlers, and Pages folders plus reference-based heuristics.',
      'analyze is the safest command to run everywhere because it does not depend on a baseline or write output folders.'
    ],
    examples: [
      {
        label: 'Healthy first pass',
        description: 'Run it on a compact sample before pointing the tool at a production solution.',
        code: 'dotnet simplicity analyze samples/Sample.Simplified/Sample.Simplified.sln',
        lang: 'bash'
      },
      {
        label: 'Baseline-free CI read',
        description: 'Use analyze on every branch even before you decide whether diff should block merges.',
        code: 'dotnet simplicity analyze YourSolution.sln',
        lang: 'bash'
      }
    ],
    relatedLinks: [
      { eyebrow: 'Next step', label: 'baseline', href: '/docs/commands/baseline/', description: 'Capture the current shape once the snapshot reflects what the team accepts today.' },
      { eyebrow: 'Next step', label: 'budget', href: '/docs/commands/budget/', description: 'Turn the same snapshot into a target check against your configured thresholds.' },
      { eyebrow: 'Context', label: 'Getting started', href: '/getting-started/', description: 'See the recommended first-run flow before you add CI gates.' }
    ]
  },
  report: {
    slug: 'report',
    title: 'report',
    summary: 'Generate a self-contained HTML report with metrics, filter verdicts, budget status, and optional trend analysis.',
    purpose: 'Use report when the audience is broader than the terminal. The HTML output packages the current snapshot into a shareable artifact.',
    bestFor: 'Stakeholder reviews, CI artifacts, and team discussions where a terminal transcript is the wrong format.',
    writesFiles: 'Yes. Always writes ./simplicity-report/index.html relative to the current working directory.',
    exitBehavior: 'Returns 0 on success. Returns 1 if analysis or report generation fails.',
    command: 'dotnet simplicity report path/to/YourSolution.sln',
    outputIntro: 'The command prints only the destination path, but the generated report includes the full narrative surface.',
    outputHighlights: [
      'Executive summary and metric cards for the current snapshot.',
      'Filter verdicts with summaries, violations, and next moves.',
      'Complexity budget scorecard rendered from simplicity.json thresholds.',
      'Trend analysis when .simplicity-history/ contains at least two readable snapshots.',
      'A fully self-contained HTML file with inline styles and no external assets.'
    ],
    sampleOutput: 'Report generated to ./simplicity-report/index.html',
    operationalNotes: [
      'The watch command ignores the simplicity-report directory so generating reports does not create watch loops.',
      'Trend history is file-based today. You must save snapshot JSON files into .simplicity-history/ yourself; there is no dedicated snapshot archive command yet.',
      'Because the output path is fixed, CI jobs usually upload the whole simplicity-report directory as an artifact.'
    ],
    examples: [
      {
        label: 'Shareable artifact',
        description: 'Generate a report after a local analysis pass and open it in a browser.',
        code: 'dotnet simplicity report samples/Sample.OverEngineered/Sample.OverEngineered.sln',
        lang: 'bash'
      },
      {
        label: 'Trend-ready workflow',
        description: 'Keep historical snapshots if you want the trend section to render a meaningful history line.',
        code: 'mkdir -p .simplicity-history\ncp .simplicity-baseline.json .simplicity-history/2026-05-01.json\ndotnet simplicity report YourSolution.sln',
        lang: 'bash'
      }
    ],
    relatedLinks: [
      { eyebrow: 'Output', label: 'budget', href: '/docs/commands/budget/', description: 'The same thresholds shown in the report also drive the CLI budget view.' },
      { eyebrow: 'Integration', label: 'CI/CD analyzer guide', href: '/integration/ci-cd/', description: 'See how teams usually publish the HTML output from CI.' },
      { eyebrow: 'Context', label: 'Samples hub', href: '/samples/', description: 'Use the teaching samples when you need a fast report demo.' }
    ]
  },
  baseline: {
    slug: 'baseline',
    title: 'baseline',
    summary: 'Write the current snapshot to a baseline file so future runs can measure drift instead of relying on memory.',
    purpose: 'baseline sets the comparison contract. Once you capture it, diff can tell you whether the solution got better, worse, or simply changed.',
    bestFor: 'Sprint boundaries, approved refactoring checkpoints, and the moment a team decides to protect current gains in CI.',
    writesFiles: 'Yes. Writes <solution-directory>/.simplicity-baseline.json and overwrites any existing file.',
    exitBehavior: 'Returns 0 on success. Returns 1 for usage errors or write failures.',
    command: 'dotnet simplicity baseline path/to/YourSolution.sln',
    outputIntro: 'baseline prints the same snapshot summary as analyze, then confirms where the baseline file was written.',
    outputHighlights: [
      'Indented camelCase JSON representing the current SimplicitySnapshot.',
      'A confirmation line that points to the exact baseline file path.',
      'No separate approval flow; rerunning the command intentionally replaces the baseline.',
      'The saved file is what diff reads later in local runs and CI.'
    ],
    sampleOutput: `Simplicity Snapshot (2026-05-01)\n----------------------------------------\nProjects: 2\nTotal files: 23\nPrimary path files: 5\nAbstraction layers: 1\nSingle-impl interfaces: 0\nExternal deps: 0 (0 unused)\nAvg complexity: 1.4\nEst. onboarding: 0h\n\nBaseline written to /path/to/.simplicity-baseline.json`,
    operationalNotes: [
      'Commit the generated baseline if you expect CI to enforce diff --fail-on-regression.',
      'The file always lives beside the solution, not in the current working directory unless that is also the solution directory.',
      'Treat baseline updates like contract changes: review them explicitly instead of letting them drift in with unrelated work.'
    ],
    examples: [
      {
        label: 'Create the first gate',
        description: 'Run once locally when the team agrees the current shape is the floor to protect.',
        code: 'dotnet simplicity baseline YourSolution.sln\ngit add .simplicity-baseline.json',
        lang: 'bash'
      }
    ],
    relatedLinks: [
      { eyebrow: 'Next step', label: 'diff', href: '/docs/commands/diff/', description: 'Compare the current solution to the saved baseline and optionally fail on regression.' },
      { eyebrow: 'Reference', label: 'Configuration', href: '/docs/configuration/', description: 'Tune filter thresholds before you decide what budget or regression means.' },
      { eyebrow: 'Workflow', label: 'Getting started', href: '/getting-started/', description: 'See where baseline fits in the normal adoption sequence.' }
    ]
  },
  diff: {
    slug: 'diff',
    title: 'diff',
    summary: 'Compare the current snapshot to the committed baseline and optionally fail the run when regression crosses the configured rules.',
    purpose: 'diff is the regression gate. It turns “did this get worse?” into a repeatable contract instead of a code-review debate.',
    bestFor: 'Pull request checks, local verification before pushing, and approval conversations around intentional complexity increases.',
    writesFiles: 'No. It reads .simplicity-baseline.json and prints a textual comparison.',
    exitBehavior: 'Returns 0 on success, or 1 when --fail-on-regression is supplied and the report detects regression. Missing baseline files also fail.',
    command: 'dotnet simplicity diff path/to/YourSolution.sln [--fail-on-regression]',
    outputIntro: 'The diff output is built for humans first: it shows every important delta and then declares whether those changes count as regression.',
    outputHighlights: [
      'Baseline file path plus baseline and current snapshot dates.',
      'Metric deltas for projects, files, abstractions, dependencies, complexity, and onboarding time.',
      'Filter score deltas for TwoAmTest, HalfRule, and PrimaryPathFirst.',
      'A regression footer that either reports no regressions or lists each triggering rule.',
      'Optional non-zero exit code when --fail-on-regression is present and the contract is violated.'
    ],
    sampleOutput: `Simplicity Diff\n---------------\nBaseline file: /path/to/.simplicity-baseline.json\nBaseline snapshot: 2026-05-01\nCurrent snapshot: 2026-05-01\n\nMetric delta\n- Total projects: 2 -> 2 (0)\n- Total files: 23 -> 23 (0)\n- Abstraction layers: 1 -> 1 (0)\n- Unused dependencies: 0 -> 0 (0)\n- Average method complexity: 1.35 -> 1.35 (0.00)\n\nFilter score delta\n- TwoAmTest: 1.00 -> 1.00 (0.00)\n- HalfRule: 1.00 -> 1.00 (0.00)\n- PrimaryPathFirst: 0.79 -> 0.79 (0.00)\n\nRegression status: no regressions detected.`,
    operationalNotes: [
      'Current regression rules are fixed in code: PrematureAbstractionRatio > +0.05, AverageMethodComplexity > +0.50, any increase in unused dependencies when the current count is non-zero, or any filter score drop worse than -0.10.',
      'diff fails fast with a clear message when the baseline file is missing and tells you to run baseline first.',
      'Use --fail-on-regression in CI only after the team agrees that the baseline file represents an intentional contract.'
    ],
    examples: [
      {
        label: 'Local comparison',
        description: 'See every delta without breaking your shell workflow.',
        code: 'dotnet simplicity diff YourSolution.sln',
        lang: 'bash'
      },
      {
        label: 'Pull request gate',
        description: 'Use the non-zero exit code to block merges when complexity grows beyond the allowed rules.',
        code: 'dotnet simplicity diff YourSolution.sln --fail-on-regression',
        lang: 'bash'
      }
    ],
    relatedLinks: [
      { eyebrow: 'Prerequisite', label: 'baseline', href: '/docs/commands/baseline/', description: 'Capture the saved comparison point before using diff.' },
      { eyebrow: 'Integration', label: 'CI/CD guide', href: '/integration/ci-cd/', description: 'Wire diff into GitHub Actions, Azure Pipelines, or GitLab CI.' },
      { eyebrow: 'Follow-up', label: 'watch', href: '/docs/commands/watch/', description: 'Use watch locally when you want live feedback while fixing a regression.' }
    ]
  },
  budget: {
    slug: 'budget',
    title: 'budget',
    summary: 'Render a four-dimension scorecard that compares the current snapshot to the thresholds in simplicity.json.',
    purpose: 'budget gives teams a shared target. Instead of debating raw metrics, they can see which dimension is closest to or beyond the agreed limit.',
    bestFor: 'Sprint planning, architecture reviews, and coaching conversations about whether a codebase is still inside acceptable operating limits.',
    writesFiles: 'No. It prints an ASCII scorecard to stdout.',
    exitBehavior: 'Returns 0 on success. Returns 1 for usage errors or analysis failures.',
    command: 'dotnet simplicity budget path/to/YourSolution.sln',
    outputIntro: 'The scorecard translates several metrics into four dimensions that teams can reason about quickly.',
    outputHighlights: [
      'Status line showing how many of the four dimensions are within budget.',
      'ASCII bars that visualize how much of each budget has been used.',
      'Actual values and configured targets for Cognitive Load, Operational Surface, Change Safety, and Discoverability.',
      'A Next move line that points at the most constrained or over-budget dimension.'
    ],
    sampleOutput: `Complexity Budget\n-----------------\nStatus: 3/4 dimension(s) within budget.\nBars show configured budget used. Values above 100% are over budget.\n\nCognitive Load      [----------]     0%  WITHIN BUDGET\n  Onboarding time: 0.0h (target <= 40.0h)\nOperational Surface [----------]     0%  WITHIN BUDGET\n  Premature abstraction ratio: 0.00 (target <= 0.25)\nChange Safety       [###-------]    27%  WITHIN BUDGET\n  Average method complexity: 1.35 (target <= 5.00)\nDiscoverability     [##########]   276%  OVER BUDGET\n  Primary path ratio: 0.22 (target >= 0.60)`,
    operationalNotes: [
      'The four dimensions map directly to configuration keys: maxOnboardingHours, prematureAbstractionRatioTarget, maxMethodComplexity, and primaryPathRatioTarget.',
      'The current collector still emits 0h onboarding time in normal CLI collection, so Cognitive Load often reads 0.0h unless you provide another snapshot source in your own code.',
      'budget is most effective when the team treats threshold changes as an explicit decision, not as a way to silence bad output.'
    ],
    examples: [
      {
        label: 'Planning conversation',
        description: 'Check the budget before approving a feature that adds new projects or layers.',
        code: 'dotnet simplicity budget YourSolution.sln',
        lang: 'bash'
      }
    ],
    relatedLinks: [
      { eyebrow: 'Reference', label: 'Configuration', href: '/docs/configuration/', description: 'Understand the exact keys that control the four budget dimensions.' },
      { eyebrow: 'Concept', label: 'Filters', href: '/docs/filters/', description: 'See how the teaching filters complement the budget scorecard.' },
      { eyebrow: 'Output', label: 'report', href: '/docs/commands/report/', description: 'Generate the same budget story as part of the HTML report.' }
    ]
  },
  watch: {
    slug: 'watch',
    title: 'watch',
    summary: 'Monitor a solution continuously and re-run analysis after file changes so simplification work gets immediate feedback.',
    purpose: 'watch is the refactoring loop. It keeps the current solution shape visible while you are actively changing code.',
    bestFor: 'Simplification sessions, teaching demos, and iterative cleanup where you want to see the effect of each edit quickly.',
    writesFiles: 'No. It watches the solution tree and streams refreshed output.',
    exitBehavior: 'Returns 0 when stopped cleanly. Returns 1 for usage errors; runtime watch failures are printed to stderr.',
    command: 'dotnet simplicity watch path/to/YourSolution.sln',
    outputIntro: 'watch starts with an initial snapshot, then prints refreshed snapshots and filter verdicts after debounced file changes.',
    outputHighlights: [
      'Full solution path in the initial “Watching …” banner.',
      'Initial snapshot plus filter verdicts immediately after startup.',
      'Updated snapshot sections after file changes, using a 500 ms debounce.',
      'Change display that shows which file changed, including rename old -> new formatting.',
      'Missing-config warnings shown once until a simplicity.json file appears again.'
    ],
    sampleOutput: `Watching /path/to/YourSolution.sln\nPress Ctrl+C to stop.\n\nInitial snapshot\n----------------\nSimplicity Snapshot (2026-05-01)\n----------------------------------------\nProjects: 2\nTotal files: 23\nPrimary path files: 5\nAbstraction layers: 1\n\nFilter Verdicts\n---------------\nTwoAmTest: PASS (1.00)\nHalfRule: PASS (1.00)\nPrimaryPathFirst: PASS (0.79)`,
    operationalNotes: [
      'The watcher ignores bin, obj, .git, .vs, and simplicity-report to avoid self-triggering noise.',
      'Configuration is reloaded on every pass, so threshold changes in simplicity.json show up without restarting the process.',
      'watch is meant for local feedback, not CI. Use diff for merge gates and report for persistent artifacts.'
    ],
    examples: [
      {
        label: 'Refactoring loop',
        description: 'Keep watch running in one terminal while you simplify code in another.',
        code: 'dotnet simplicity watch samples/Sample.Simplified/Sample.Simplified.sln',
        lang: 'bash'
      }
    ],
    relatedLinks: [
      { eyebrow: 'Companion', label: 'diff', href: '/docs/commands/diff/', description: 'Use diff to confirm the same improvements against the committed baseline.' },
      { eyebrow: 'Companion', label: 'report', href: '/docs/commands/report/', description: 'Generate a shareable artifact once the live session produces a better result.' },
      { eyebrow: 'Support', label: 'Troubleshooting markdown', href: 'https://github.com/cwoodruff/SimplicityTools/blob/main/docs/troubleshooting.md', description: 'Use the repository troubleshooting guide when file watching or IDE state becomes noisy.', external: true }
    ]
  }
};

export const filterDetails: Record<string, FilterDetail> = {
  twowamtest: {
    slug: 'twowamtest',
    title: 'TwoAmTest',
    summary: 'A teach-first filter that asks whether a tired engineer can still find, understand, and safely change the primary flow under pressure.',
    question: 'If a production incident lands at 2 AM, can someone trace the main path, reason about the code, and ship a fix without getting lost?',
    showsUpIn: 'CLI watch output, HTML reports, and programmatic FilterVerdict evaluation.',
    passThreshold: 'The overall verdict passes at the configured passingScore (0.70 by default).',
    primaryUse: 'Diagnosability and incident response coaching.',
    signals: [
      { name: 'Discoverability', description: 'Scores the number of primary-path files against a target of five or fewer.' },
      { name: 'Diagnosability', description: 'Scores average method complexity against a target of five or lower.' },
      { name: 'Fixability', description: 'Scores abstraction layers per project against a target ratio of three or lower.' },
      { name: 'Cognitive load', description: 'Scores estimated onboarding time against a target of roughly one work week (40 hours).' }
    ],
    interpretation: 'A low TwoAmTest score means the team is paying an incident-response tax before they even start fixing the actual defect.',
    interpretationBullets: [
      'If Discoverability drops, the primary path is spread across too many files.',
      'If Diagnosability drops, the average method complexity is drifting too high for fast reasoning.',
      'If Fixability drops, each project carries too many layers to edit confidently under pressure.',
      'If Cognitive load drops, the solution shape is expensive for any new engineer to absorb.'
    ],
    nextSteps: [
      'Collapse low-value abstraction layers around routine change paths.',
      'Break large methods into smaller units until average complexity trends back toward five or lower.',
      'Move the primary flow into fewer, more obvious files and annotate it explicitly when needed.'
    ],
    relatedLinks: [
      { eyebrow: 'Command', label: 'watch', href: '/docs/commands/watch/', description: 'The live watch view prints TwoAmTest verdicts after every meaningful file change.' },
      { eyebrow: 'Analyzer', label: 'SF0003', href: '/analyzers/sf0003/', description: 'High method complexity is one of the clearest reasons a TwoAmTest score drops.' },
      { eyebrow: 'Analyzer', label: 'SF0005', href: '/analyzers/sf0005/', description: 'Bloated constructors are another strong signal that fixing code will be harder than it should be.' }
    ]
  },
  halfrule: {
    slug: 'halfrule',
    title: 'HalfRule',
    summary: 'A filter that asks whether the codebase is accumulating indirection and dependencies faster than the problem actually demands.',
    question: 'Are we shipping at least half as much value as the abstraction and dependency surface we are creating to support it?',
    showsUpIn: 'CLI watch output, HTML reports, and programmatic FilterVerdict evaluation.',
    passThreshold: 'The overall verdict passes at the configured passingScore (0.70 by default).',
    primaryUse: 'Abstraction discipline and dependency pruning.',
    signals: [
      { name: 'Premature abstraction', description: 'Scores single-implementation interface growth and penalizes ceremony without real polymorphism.' },
      { name: 'Dependency accumulation', description: 'Scores unused dependency count so dead packages do not compound over time.' },
      { name: 'Dependency sprawl', description: 'Scores external dependency count per project against a target ratio of eight or fewer.' }
    ],
    interpretation: 'A low HalfRule score means the solution is spending complexity on wrappers, packages, or generic surfaces that are not buying real flexibility.',
    interpretationBullets: [
      'If Premature abstraction drops, simplify single-use interfaces and one-off generic abstractions.',
      'If Dependency accumulation drops, remove package references that contribute no used symbols.',
      'If Dependency sprawl drops, consolidate packages so each project carries fewer external concerns.'
    ],
    nextSteps: [
      'Inline or delete abstractions that only have one concrete path through the system.',
      'Remove unused package references before they become accepted background noise.',
      'Review whether generic parameters and extra projects are solving a present problem or just defending against speculation.'
    ],
    relatedLinks: [
      { eyebrow: 'Analyzer', label: 'SF0001', href: '/analyzers/sf0001/', description: 'Single-implementation interfaces are the classic HalfRule smell.' },
      { eyebrow: 'Analyzer', label: 'SF0002', href: '/analyzers/sf0002/', description: 'Unused package references create cost without helping the product.' },
      { eyebrow: 'Analyzer', label: 'SF0006', href: '/analyzers/sf0006/', description: 'Single-specialization generic parameters are another form of speculative indirection.' }
    ]
  },
  primarypathfirst: {
    slug: 'primarypathfirst',
    title: 'PrimaryPathFirst',
    summary: 'A filter that asks whether the main business flow is still obvious, concentrated, and more important than the supporting scaffolding around it.',
    question: 'Can a new contributor trace the real product path quickly, or has the supporting code become more visible than the business flow itself?',
    showsUpIn: 'CLI watch output, HTML reports, and programmatic FilterVerdict evaluation.',
    passThreshold: 'The overall verdict passes at the configured passingScore (0.70 by default).',
    primaryUse: 'Discoverability and flow clarity.',
    signals: [
      { name: 'Primary path concentration', description: 'Scores the percentage of code that sits on the main flow against the configured target (0.60 by default).' },
      { name: 'Abstraction dilution', description: 'Scores abstraction layers per primary-path file against a target ratio of roughly one layer per three primary-path files.' },
      { name: 'Project count', description: 'Scores total project count against a target of five or fewer projects.' }
    ],
    interpretation: 'A low PrimaryPathFirst score means teams have to learn the supporting machinery before they can learn the product flow.',
    interpretationBullets: [
      'If concentration drops, too little of the codebase is on the primary path.',
      'If abstraction dilution drops, the business flow is wrapped in too many layers.',
      'If project count drops, the solution shape itself is hiding the path behind too many boundaries.'
    ],
    nextSteps: [
      'Mark the real path with [PrimaryPath] attributes where the conventions are not enough.',
      'Move core orchestration into fewer, more direct files and folders.',
      'Merge or remove low-value projects that exist mainly to preserve ceremony.'
    ],
    relatedLinks: [
      { eyebrow: 'Analyzer', label: 'SF0004', href: '/analyzers/sf0004/', description: 'Deep call chains are one of the cleanest signs that the primary path is buried.' },
      { eyebrow: 'Analyzer', label: 'SF0007', href: '/analyzers/sf0007/', description: 'When support files are referenced more than the primary path, discoverability has already flipped.' },
      { eyebrow: 'Guide', label: 'csproj reference', href: '/integration/csproj-reference/', description: 'Use explicit analyzer installation and primary-path annotations when you need the IDE and CLI to agree.' }
    ]
  }
};

export const analyzerDetails: Record<string, AnalyzerDetail> = {
  sf0001: {
    slug: 'sf0001',
    id: 'SF0001',
    title: 'Interface has single implementation',
    summary: 'Flags interfaces that introduce indirection without delivering polymorphism because the solution only has one concrete implementation.',
    category: 'SimplicityFirst.HalfRule',
    severity: 'Warning',
    codeFix: true,
    whenItFiresSummary: 'Exactly one non-abstract class or struct implements the interface in source.',
    ruleMessage: 'Interface {0} has exactly one non-abstract implementation: {1}. Remove the interface and use the concrete type directly.',
    whyItMatters: 'An interface with one implementation is usually ceremony. It makes navigation, DI setup, and onboarding harder without buying substitution power.',
    whenItFires: [
      'The analyzer looks only at source-defined interfaces, then finds concrete classes or structs that implement them.',
      'It reports only when there is exactly one non-abstract implementation. Zero or multiple implementations are left alone.',
      'The rule belongs to the HalfRule category because it measures speculative abstraction.'
    ],
    badExample: {
      code: `public interface IPricingService\n{\n    decimal Calculate(Order order);\n}\n\npublic sealed class DefaultPricingService : IPricingService\n{\n    public decimal Calculate(Order order) => order.Total * 0.95m;\n}\n\npublic sealed class CheckoutHandler(IPricingService pricingService)\n{\n    public decimal Quote(Order order) => pricingService.Calculate(order);\n}`,
      lang: 'csharp',
      caption: 'The interface exists, but the codebase only ever uses DefaultPricingService.'
    },
    goodExample: {
      code: `public sealed class DefaultPricingService\n{\n    public decimal Calculate(Order order) => order.Total * 0.95m;\n}\n\npublic sealed class CheckoutHandler(DefaultPricingService pricingService)\n{\n    public decimal Quote(Order order) => pricingService.Calculate(order);\n}`,
      lang: 'csharp',
      caption: 'The direct dependency makes the real shape obvious and removes one layer of indirection.'
    },
    codeFixSummary: 'The code fix rewrites interface references to the concrete implementation, removes the interface declaration, and preserves dependent interface members where possible.',
    codeFixNotes: [
      'Use the lightbulb in the IDE when the interface exists only as ceremony.',
      'Review the result if the interface participates in a larger hierarchy; the fixer preserves direct dependent members but cannot invent missing abstraction value.',
      'This is one of the two diagnostics with an in-repo code fix today.'
    ],
    sourceHref: `${repoBase}/src/SimplicityTools.Analyzers/SingleImplementationInterfaceAnalyzer.cs`,
    sourceLabel: 'SingleImplementationInterfaceAnalyzer.cs',
    codeFixHref: `${repoBase}/src/SimplicityTools.Analyzers/CodeFixes/SingleImplementationInterfaceCodeFixProvider.cs`,
    codeFixLabel: 'SingleImplementationInterfaceCodeFixProvider.cs',
    issueHref: issue55,
    issueNumber: 55,
    relatedLinks: [
      { eyebrow: 'Guide', label: 'Analyzer install guide', href: '/integration/csproj-reference/', description: 'See the correct PackageReference shape for enabling the rule in another solution.' },
      { eyebrow: 'Filter', label: 'HalfRule', href: '/docs/filters/halfrule/', description: 'This diagnostic is a concrete way the HalfRule surfaces speculative abstraction.' }
    ]
  },
  sf0002: {
    slug: 'sf0002',
    id: 'SF0002',
    title: 'Package reference has no symbol usage',
    summary: 'Flags PackageReference entries that contribute no detected symbol usage in C# source so dependency cost does not accumulate silently.',
    category: 'SimplicityFirst.HalfRule',
    severity: 'Warning',
    codeFix: true,
    whenItFiresSummary: 'A PackageReference maps to assemblies in the compilation, but no symbols from those assemblies are used in countable C# source files.',
    ruleMessage: 'PackageReference {0} has no detected symbol usage in C# source. Remove the dependency or justify it with source usage.',
    whyItMatters: 'Unused packages add restore time, transitive risk, and maintenance cost. The cleanest dependency is the one you do not carry.',
    whenItFires: [
      'The analyzer reads PackageReference items from the project file and maps them to referenced assemblies.',
      'It scans countable source files for used symbols and treats packages with zero symbol usage as unused.',
      'The rule reports at compilation end so it can evaluate the full project graph instead of guessing per syntax node.'
    ],
    badExample: {
      code: `<ItemGroup>\n  <PackageReference Include="Serilog" Version="4.0.0" />\n  <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />\n</ItemGroup>`,
      lang: 'xml',
      caption: 'If no Serilog symbols are used in the project, this reference is dead weight.'
    },
    goodExample: {
      code: `<ItemGroup>\n  <PackageReference Include="Microsoft.Extensions.Logging" Version="9.0.0" />\n</ItemGroup>`,
      lang: 'xml',
      caption: 'The project keeps only the package that contributes real code usage.'
    },
    codeFixSummary: 'The code fix removes the targeted PackageReference node from the project file without rewriting unrelated XML.',
    codeFixNotes: [
      'Run a normal build after applying the fix so any non-symbol usage contracts become obvious.',
      'Analyzer-only, build-only, or source-generator dependencies may need a deliberate justification instead of a blind delete.',
      'This is the second in-repo diagnostic with an automatic code fix.'
    ],
    sourceHref: `${repoBase}/src/SimplicityTools.Analyzers/UnusedDependencyAnalyzer.cs`,
    sourceLabel: 'UnusedDependencyAnalyzer.cs',
    codeFixHref: `${repoBase}/src/SimplicityTools.Analyzers/CodeFixes/UnusedDependencyCodeFixProvider.cs`,
    codeFixLabel: 'UnusedDependencyCodeFixProvider.cs',
    issueHref: issue55,
    issueNumber: 55,
    relatedLinks: [
      { eyebrow: 'Guide', label: 'csproj reference', href: '/integration/csproj-reference/', description: 'Keep analyzer installation isolated while pruning package sprawl in application projects.' },
      { eyebrow: 'Filter', label: 'HalfRule', href: '/docs/filters/halfrule/', description: 'Unused dependencies are a direct HalfRule smell.' }
    ]
  },
  sf0003: {
    slug: 'sf0003',
    id: 'SF0003',
    title: 'Method is too complex for fast understanding',
    summary: 'Flags ordinary methods whose cyclomatic complexity exceeds 10 so teams do not normalize “hard to reason about” as a routine cost.',
    category: 'SimplicityFirst.TwoAmTest',
    severity: 'Warning',
    codeFix: false,
    whenItFiresSummary: 'An ordinary source method calculates above the fixed cyclomatic complexity threshold of 10.',
    ruleMessage: 'Method {0} has cyclomatic complexity {1}, which exceeds the limit of 10',
    whyItMatters: 'Under pressure, complex methods slow diagnosis and increase the chance of editing the wrong branch. That is exactly the problem TwoAmTest is supposed to catch.',
    whenItFires: [
      'Only ordinary methods are analyzed; constructors and other method kinds are handled elsewhere or ignored.',
      'The analyzer calculates cyclomatic complexity from the method declaration syntax and reports when the score exceeds 10.',
      'The diagnostic is advisory today. The right fix depends on extracting intent, not just splitting lines mechanically.'
    ],
    badExample: {
      code: `public string ResolveStatus(Order order, User user, bool isPriority)\n{\n    if (order is null) return "missing";\n    if (!user.IsActive) return "blocked";\n\n    if (order.IsPaid)\n    {\n        if (order.IsPacked)\n        {\n            if (order.IsShipped)\n            {\n                return order.IsDelayed ? "investigate" : "complete";\n            }\n\n            return isPriority ? "expedite" : "dispatch";\n        }\n\n        return order.HasBackOrder ? "hold" : "pack";\n    }\n\n    return order.RequiresReview ? "review" : "collect-payment";\n}`,
      lang: 'csharp',
      caption: 'The reader has to simulate several branches before they can answer a routine question.'
    },
    goodExample: {
      code: `public string ResolveStatus(Order order, User user, bool isPriority)\n{\n    if (order is null) return "missing";\n    if (!user.IsActive) return "blocked";\n\n    return order.IsPaid\n        ? ResolvePaidOrderStatus(order, isPriority)\n        : ResolveUnpaidOrderStatus(order);\n}\n\nprivate static string ResolvePaidOrderStatus(Order order, bool isPriority)\n{\n    if (!order.IsPacked) return order.HasBackOrder ? "hold" : "pack";\n    if (!order.IsShipped) return isPriority ? "expedite" : "dispatch";\n    return order.IsDelayed ? "investigate" : "complete";\n}`,
      lang: 'csharp',
      caption: 'The branching still exists, but the path names are easier to scan and change under pressure.'
    },
    codeFixNotes: [
      'Start by naming the decision points, not by blindly extracting random blocks.',
      'If the method is orchestration, consider moving sub-decisions into narrower collaborators.',
      'Use watch or diff to confirm the simplification improves the broader solution signal, not just one method.'
    ],
    sourceHref: `${repoBase}/src/SimplicityTools.Analyzers/HighComplexityAnalyzer.cs`,
    sourceLabel: 'HighComplexityAnalyzer.cs',
    issueHref: issue55,
    issueNumber: 55,
    relatedLinks: [
      { eyebrow: 'Filter', label: 'TwoAmTest', href: '/docs/filters/twowamtest/', description: 'Method complexity is one of the main reasons a TwoAmTest verdict falls.' },
      { eyebrow: 'Command', label: 'watch', href: '/docs/commands/watch/', description: 'Use watch during refactoring sessions to see if the broader signal improves as complexity drops.' }
    ]
  },
  sf0004: {
    slug: 'sf0004',
    id: 'SF0004',
    title: 'Method call chain is too deep',
    summary: 'Flags source methods that route through more than eight abstraction layers because the primary path has been buried under wrappers.',
    category: 'SimplicityFirst.PrimaryPathFirst',
    severity: 'Warning',
    codeFix: false,
    whenItFiresSummary: 'The computed source-level call graph shows a method depth above the fixed threshold of 8.',
    ruleMessage: 'Method {0} passes through {1} abstraction layers, exceeding the limit of 8',
    whyItMatters: 'A deep call chain makes it hard to see where the product behavior actually lives. The deeper the path, the more ceremony a reader must traverse before reaching the point of change.',
    whenItFires: [
      'The analyzer builds a call graph from source methods and resolves unique dispatch targets for interface and override calls when possible.',
      'It reports only when the computed depth exceeds eight layers.',
      'Because it reasons over the whole compilation, the diagnostic fires at compilation end instead of per node.'
    ],
    badExample: {
      code: `public sealed class CheckoutController(CheckoutApplicationService app)\n{\n    public Task<Result> PostAsync(Request request) => app.HandleAsync(request);\n}\n\npublic sealed class CheckoutApplicationService(CheckoutCoordinator coordinator)\n{\n    public Task<Result> HandleAsync(Request request) => coordinator.RunAsync(request);\n}\n\npublic sealed class CheckoutCoordinator(CheckoutPipeline pipeline)\n{\n    public Task<Result> RunAsync(Request request) => pipeline.ExecuteAsync(request);\n}\n\n// ... Validator -> Mapper -> RepositoryFacade -> Repository -> SqlGateway ...`,
      lang: 'csharp',
      caption: 'The business path is technically present, but most of it is spent traversing wrappers.'
    },
    goodExample: {
      code: `public sealed class CheckoutController(CheckoutHandler handler)\n{\n    public Task<Result> PostAsync(Request request) => handler.HandleAsync(request);\n}\n\npublic sealed class CheckoutHandler(OrderRepository repository, PaymentGateway gateway)\n{\n    public async Task<Result> HandleAsync(Request request)\n    {\n        var order = await repository.LoadAsync(request.OrderId);\n        return await gateway.AuthorizeAsync(order);\n    }\n}`,
      lang: 'csharp',
      caption: 'The reader reaches the real flow quickly, with supporting concerns still available but not piled on top.'
    },
    codeFixNotes: [
      'Look for wrapper classes that simply forward the same arguments to the next layer.',
      'Prefer collapsing the path toward a direct handler over adding another “shared abstraction” to hide it.',
      'Pair this rule with explicit [PrimaryPath] annotations when conventions are not enough to tell the truth.'
    ],
    sourceHref: `${repoBase}/src/SimplicityTools.Analyzers/AbstractionLayerDepthAnalyzer.cs`,
    sourceLabel: 'AbstractionLayerDepthAnalyzer.cs',
    issueHref: issue55,
    issueNumber: 55,
    relatedLinks: [
      { eyebrow: 'Filter', label: 'PrimaryPathFirst', href: '/docs/filters/primarypathfirst/', description: 'Deep call chains are one of the clearest ways to bury the primary path.' },
      { eyebrow: 'Guide', label: 'IDE setup', href: '/integration/ide-setup/', description: 'Enable the analyzer package so the call-chain warning shows up during normal development.' }
    ]
  },
  sf0005: {
    slug: 'sf0005',
    id: 'SF0005',
    title: 'Constructor takes too many parameters',
    summary: 'Flags classes with constructors above seven parameters because large parameter lists usually mean the type owns too much work.',
    category: 'SimplicityFirst.TwoAmTest',
    severity: 'Warning',
    codeFix: false,
    whenItFiresSummary: 'A source class has an explicit instance constructor with more than seven parameters.',
    ruleMessage: 'Constructor on {0} takes {1} parameters, exceeding the limit of 7',
    whyItMatters: 'A class that needs a crowd of collaborators is often hiding multiple responsibilities. That raises cognitive load before the first line of behavior even runs.',
    whenItFires: [
      'The analyzer looks at explicit instance constructors on source classes only.',
      'Any constructor with more than seven parameters triggers the warning.',
      'The diagnostic is intentionally simple: the parameter count is a strong enough smell to start the design conversation.'
    ],
    badExample: {
      code: `public sealed class CheckoutWorkflow(\n    IOrderRepository orders,\n    IPaymentGateway payments,\n    IInventoryService inventory,\n    INotificationService notifications,\n    ILogger<CheckoutWorkflow> logger,\n    IClock clock,\n    IFeatureFlags flags,\n    IAuditWriter audit)\n{\n}`,
      lang: 'csharp',
      caption: 'The constructor advertises a type that is coordinating too many concerns at once.'
    },
    goodExample: {
      code: `public sealed class CheckoutServices(\n    IOrderRepository orders,\n    IPaymentGateway payments,\n    IInventoryService inventory)\n{\n}\n\npublic sealed class CheckoutWorkflow(\n    CheckoutServices services,\n    INotificationService notifications,\n    ILogger<CheckoutWorkflow> logger)\n{\n}`,
      lang: 'csharp',
      caption: 'Group stable collaborators around a real responsibility instead of handing every dependency directly to the orchestration type.'
    },
    codeFixNotes: [
      'Do not hide the smell behind a parameter object unless that object represents a real cohesive concept.',
      'If the constructor belongs to orchestration code, see whether some behavior should move into a narrower handler or service.',
      'Use the warning as a design review checkpoint, not as a demand to satisfy an arbitrary number.'
    ],
    sourceHref: `${repoBase}/src/SimplicityTools.Analyzers/ConstructorParameterCountAnalyzer.cs`,
    sourceLabel: 'ConstructorParameterCountAnalyzer.cs',
    issueHref: issue55,
    issueNumber: 55,
    relatedLinks: [
      { eyebrow: 'Filter', label: 'TwoAmTest', href: '/docs/filters/twowamtest/', description: 'Large constructor surfaces make it harder to understand and change code under pressure.' },
      { eyebrow: 'Command', label: 'budget', href: '/docs/commands/budget/', description: 'Use the budget view to keep the broader change-safety and cognitive-load conversation grounded.' }
    ]
  },
  sf0006: {
    slug: 'sf0006',
    id: 'SF0006',
    title: 'Generic parameter has only one specialization',
    summary: 'Flags generic types or methods whose type parameter is only ever bound to one concrete type in source, signaling abstraction without flexibility.',
    category: 'SimplicityFirst.HalfRule',
    severity: 'Warning',
    codeFix: false,
    whenItFiresSummary: 'A generic definition is specialized in source, but a given type parameter is only ever bound to one concrete type.',
    ruleMessage: 'Generic parameter {0} on {1} is only specialized as {2}. Remove the generic parameter or use the concrete type directly.',
    whyItMatters: 'Generics are powerful when multiple real specializations exist. When there is only one, they hide the real contract behind a pretend flexibility story.',
    whenItFires: [
      'The analyzer collects generic type and method definitions from source, then records how their type parameters are specialized across source usage.',
      'It reports a parameter only when exactly one concrete specialization is found for that slot.',
      'The warning applies to generic methods and generic types alike.'
    ],
    badExample: {
      code: `public interface IRepository<TDocument>\n{\n    Task<TDocument?> LoadAsync(Guid id);\n}\n\npublic sealed class SqlOrderRepository : IRepository<Order>\n{\n    public Task<Order?> LoadAsync(Guid id) => Task.FromResult<Order?>(null);\n}\n\npublic sealed class CheckoutHandler(IRepository<Order> repository)\n{\n}`,
      lang: 'csharp',
      caption: 'If TDocument is only ever Order, the generic parameter is storytelling, not flexibility.'
    },
    goodExample: {
      code: `public interface IOrderRepository\n{\n    Task<Order?> LoadAsync(Guid id);\n}\n\npublic sealed class SqlOrderRepository : IOrderRepository\n{\n    public Task<Order?> LoadAsync(Guid id) => Task.FromResult<Order?>(null);\n}`,
      lang: 'csharp',
      caption: 'The concrete contract makes the real shape obvious and removes one axis of pretend generality.'
    },
    codeFixNotes: [
      'Check whether the team is planning multiple specializations soon or just preserving a hypothetical future.',
      'If the abstraction is shared across package boundaries, simplify carefully and communicate the contract change.',
      'This rule often pairs with SF0001 when generic interfaces also have one implementation.'
    ],
    sourceHref: `${repoBase}/src/SimplicityTools.Analyzers/SingleSpecializationGenericParameterAnalyzer.cs`,
    sourceLabel: 'SingleSpecializationGenericParameterAnalyzer.cs',
    issueHref: issue55,
    issueNumber: 55,
    relatedLinks: [
      { eyebrow: 'Filter', label: 'HalfRule', href: '/docs/filters/halfrule/', description: 'Single-specialization generics are another form of speculative abstraction.' },
      { eyebrow: 'Guide', label: 'Library usage', href: '/docs/library-usage/', description: 'Use the library docs to decide when an API surface should stay generic across package boundaries.' }
    ]
  },
  sf0007: {
    slug: 'sf0007',
    id: 'SF0007',
    title: 'Supporting file is referenced more than the primary path',
    summary: 'Flags supporting files whose inbound reference count exceeds the most referenced primary-path file, signaling that the scaffolding has become easier to see than the real flow.',
    category: 'SimplicityFirst.PrimaryPathFirst',
    severity: 'Warning',
    codeFix: false,
    whenItFiresSummary: 'A non-primary-path source file has more inbound references than the highest referenced primary-path file.',
    ruleMessage: 'File {0} has {1} inbound references, exceeding the highest primary-path file count of {2}',
    whyItMatters: 'If more code points at the support structure than at the actual business path, the product behavior is no longer the dominant story in the system.',
    whenItFires: [
      'The analyzer first identifies primary-path files by [PrimaryPath] annotation; if none exist, it falls back to Controllers, Endpoints, Handlers, and Pages folders.',
      'It counts inbound type references per source file and compares non-primary-path files to the strongest primary-path reference count.',
      'It reports only when a supporting file exceeds that primary-path ceiling.'
    ],
    badExample: {
      code: `public sealed class PolicyRegistry { }\npublic sealed class CheckoutHandler(PolicyRegistry registry) { }\npublic sealed class RefundHandler(PolicyRegistry registry) { }\npublic sealed class RenewalHandler(PolicyRegistry registry) { }\npublic sealed class TrialConversionHandler(PolicyRegistry registry) { }`,
      lang: 'csharp',
      caption: 'The support object becomes the real center of gravity while the business handlers look secondary.'
    },
    goodExample: {
      code: `using SimplicityTools.Metrics;\n\n[PrimaryPath]\npublic sealed class CheckoutHandler(OrderRepository orders, PaymentGateway gateway)\n{\n    public Task<Result> HandleAsync(Request request) => gateway.AuthorizeAsync(request.ToOrder());\n}\n\npublic sealed class RetryPolicy\n{\n    public static bool ShouldRetry(int attempts) => attempts < 3;\n}`,
      lang: 'csharp',
      caption: 'The real path is explicit and direct, while support code stays obviously supportive.'
    },
    codeFixNotes: [
      'Add [PrimaryPath] annotations if the conventions are not enough to tell the tooling what really matters.',
      'Move orchestration back into primary-path handlers instead of centralizing every decision in support registries or framework glue.',
      'Treat this as a whole-flow signal, not as a demand to eliminate every shared utility.'
    ],
    sourceHref: `${repoBase}/src/SimplicityTools.Analyzers/NonPrimaryPathOverReferencedAnalyzer.cs`,
    sourceLabel: 'NonPrimaryPathOverReferencedAnalyzer.cs',
    issueHref: issue55,
    issueNumber: 55,
    relatedLinks: [
      { eyebrow: 'Filter', label: 'PrimaryPathFirst', href: '/docs/filters/primarypathfirst/', description: 'This rule is the sharpest analyzer expression of the PrimaryPathFirst filter.' },
      { eyebrow: 'Guide', label: 'csproj reference', href: '/integration/csproj-reference/', description: 'Install the analyzer package and use [PrimaryPath] explicitly when conventions are not enough.' }
    ]
  }
};

export const commandCards = Object.values(commandDetails).map((detail) => ({
  eyebrow: 'CLI command',
  label: detail.title,
  href: `/docs/commands/${detail.slug}/`,
  description: detail.summary
}));

export const filterCards = Object.values(filterDetails).map((detail) => ({
  eyebrow: 'Filter',
  label: detail.title,
  href: `/docs/filters/${detail.slug}/`,
  description: detail.summary
}));

export const analyzerCards = Object.values(analyzerDetails).map((detail) => ({
  eyebrow: detail.id,
  label: detail.title,
  href: `/analyzers/${detail.slug}/`,
  description: detail.summary
}));
