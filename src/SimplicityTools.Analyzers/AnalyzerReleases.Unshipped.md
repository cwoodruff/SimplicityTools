; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SF0001 | SimplicityFirst.HalfRule | Info | Interface has single implementation
SF0002 | SimplicityFirst.HalfRule | Info | Package reference has no symbol usage
SF0003 | SimplicityFirst.TwoAmTest | Warning | Method is too complex for fast understanding
SF0004 | SimplicityFirst.PrimaryPathFirst | Warning | Method call chain is too deep
SF0005 | SimplicityFirst.TwoAmTest | Warning | Constructor takes too many parameters
SF0006 | SimplicityFirst.HalfRule | Info | Generic parameter has only one specialization
SF0007 | SimplicityFirst.PrimaryPathFirst | Warning | Supporting file is referenced more than the primary path
