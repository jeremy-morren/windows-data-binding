; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WGD001 | WinDataBinding | Warning | Circular reference skipped
WGD002 | WinDataBinding | Error | Binding model type must be partial
WGD003 | WinDataBinding | Error | Generic types are not supported
WGD004 | WinDataBinding | Error | Containing type must be partial
WGD005 | WinDataBinding | Warning | Custom strong ID templates are not supported
