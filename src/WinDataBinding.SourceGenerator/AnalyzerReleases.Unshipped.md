; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WGD001 | WinDataBinding | Warning | Circular reference skipped
WGD002 | WinDataBinding | Error | Binding model type must be partial
WGD003 | WinDataBinding | Error | Open generic types are not supported
WGD004 | WinDataBinding | Error | Containing type must be partial
WGD006 | WinDataBinding | Warning | A hand-written constructor must set the source field
