[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectRoot,
    [Parameter(Mandatory)][string]$CandidateRoot,
    [Parameter(Mandatory)][string]$ManifestPath,
    [Parameter(Mandatory)][string]$ReportRelativePath,
    [switch]$CheckPublished
)
$ErrorActionPreference = 'Stop'
$script:rejectedCode = 'INVALID_INPUT'
function Reject-Snapshot([string]$Code) { $script:rejectedCode = $Code; throw 'Snapshot rejected' }
function Assert-NoReparsePoint([string]$Path) {
    $ancestors = [Collections.Generic.List[string]]::new()
    $cursor = $Path
    while ($cursor) {
        $ancestors.Add($cursor)
        $cursor = [IO.Path]::GetDirectoryName($cursor)
    }
    for ($i = $ancestors.Count - 1; $i -ge 0; $i--) {
        $item = Get-Item -LiteralPath $ancestors[$i] -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { Reject-Snapshot 'REPARSE_POINT_REJECTED' }
    }
}
function Get-CheckedRoot([string]$Root) {
    $full = [IO.Path]::GetFullPath($Root)
    if ($full -notmatch '^[A-Za-z]:\\') { Reject-Snapshot 'LOCAL_PATH_REQUIRED' }
    if ($full.TrimEnd([IO.Path]::DirectorySeparatorChar) -eq [IO.Path]::GetPathRoot($full).TrimEnd([IO.Path]::DirectorySeparatorChar)) { Reject-Snapshot 'INVALID_ROOT' }
    Assert-NoReparsePoint $full
    if (-not (Test-Path -LiteralPath $full -PathType Container)) { Reject-Snapshot 'INVALID_ROOT' }
    return $full.TrimEnd([IO.Path]::DirectorySeparatorChar)
}
function Get-CheckedRelativePath([string]$Root, [object]$Relative) {
    if ($Relative -isnot [string] -or [string]::IsNullOrWhiteSpace($Relative) -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '[<>:"|?*\x00-\x1f]') { Reject-Snapshot 'UNSAFE_RELATIVE_PATH' }
    $segments = $Relative.Replace('\', '/').Split('/')
    if (@($segments | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0) { Reject-Snapshot 'UNSAFE_RELATIVE_PATH' }
    $full = [IO.Path]::GetFullPath([IO.Path]::Combine($Root, ($segments -join [IO.Path]::DirectorySeparatorChar)))
    if (-not $full.StartsWith($Root + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { Reject-Snapshot 'UNSAFE_RELATIVE_PATH' }
    Assert-NoReparsePoint $full
    return $full
}
function Assert-UniqueJsonKeys([System.Text.Json.JsonElement]$Element) {
    if ($Element.ValueKind -eq [System.Text.Json.JsonValueKind]::Object) {
        $keys = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($property in $Element.EnumerateObject()) {
            if (-not $keys.Add($property.Name)) { Reject-Snapshot 'DUPLICATE_JSON_KEY' }
            Assert-UniqueJsonKeys $property.Value
        }
    } elseif ($Element.ValueKind -eq [System.Text.Json.JsonValueKind]::Array) {
        foreach ($value in $Element.EnumerateArray()) { Assert-UniqueJsonKeys $value }
    }
}
function Read-StrictJson([string]$Path) {
    $full = [IO.Path]::GetFullPath($Path)
    if ($full -notmatch '^[A-Za-z]:\\') { Reject-Snapshot 'LOCAL_PATH_REQUIRED' }
    Assert-NoReparsePoint $full
    $text = Get-Content -LiteralPath $Path -Raw
    try { $document = [System.Text.Json.JsonDocument]::Parse([string]$text) } catch { Reject-Snapshot 'INVALID_JSON' }
    try { Assert-UniqueJsonKeys $document.RootElement } finally { $document.Dispose() }
    return ,(ConvertFrom-Json -InputObject $text -AsHashtable -NoEnumerate -Depth 64)
}
function Assert-ReportShape([object]$Report) {
    if ($Report -isnot [Collections.IDictionary] -or $Report['validated_sha256'] -isnot [Collections.IDictionary] -or $Report['validated_sha256'].Count -eq 0) { Reject-Snapshot 'INVALID_REPORT' }
    foreach ($field in @('new_cases_passed', 'previous_cases_passed', 'total_cases_passed')) {
        if (($Report[$field] -isnot [int] -and $Report[$field] -isnot [long]) -or $Report[$field] -lt 0) { Reject-Snapshot 'INVALID_REPORT' }
    }
    if ($Report['new_cases_passed'] + $Report['previous_cases_passed'] -ne $Report['total_cases_passed'] -or $Report['results'] -isnot [Array] -or $Report['results'].Count -ne $Report['new_cases_passed']) { Reject-Snapshot 'INVALID_REPORT' }
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($result in $Report['results']) {
        if ($result -isnot [Collections.IDictionary] -or $result['id'] -isnot [string] -or [string]::IsNullOrWhiteSpace($result['id']) -or -not $ids.Add($result['id']) -or $result['passed'] -isnot [bool] -or -not $result['passed']) { Reject-Snapshot 'INVALID_REPORT' }
    }
    foreach ($digest in $Report['validated_sha256'].Values) { if ($digest -isnot [string] -or $digest -notmatch '^[a-fA-F0-9]{64}$') { Reject-Snapshot 'INVALID_REPORT' } }
}
try {
$ProjectRoot = Get-CheckedRoot $ProjectRoot
$CandidateRoot = Get-CheckedRoot $CandidateRoot
if ($CheckPublished -and $ProjectRoot.Equals($CandidateRoot, [StringComparison]::OrdinalIgnoreCase)) { Reject-Snapshot 'DISTINCT_ROOTS_REQUIRED' }
$reportPath = Get-CheckedRelativePath $CandidateRoot $ReportRelativePath
$manifest = Read-StrictJson $ManifestPath
if ($manifest -isnot [Array] -or $manifest.Count -eq 0) { Reject-Snapshot 'INVALID_MANIFEST' }
$paths = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($relative in $manifest) {
    $file = Get-CheckedRelativePath $CandidateRoot $relative
    if (-not $paths.Add($file)) { Reject-Snapshot 'DUPLICATE_MANIFEST' }
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { Reject-Snapshot 'MISSING_CANDIDATE_FILE' }
}
$items = @(Get-ChildItem -LiteralPath $CandidateRoot -Recurse -Force)
foreach ($item in $items) {
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { Reject-Snapshot 'REPARSE_POINT_REJECTED' }
    if (-not $item.PSIsContainer -and -not $paths.Contains($item.FullName)) { Reject-Snapshot 'UNLISTED_CANDIDATE_FILE' }
}
$report = Read-StrictJson $reportPath
Assert-ReportShape $report
$references = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($relative in $report['validated_sha256'].Keys) {
    $file = Get-CheckedRelativePath $CandidateRoot $relative
    if (-not $references.Add($file)) { Reject-Snapshot 'DUPLICATE_REFERENCE' }
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { $file = Get-CheckedRelativePath $ProjectRoot $relative }
    $actual = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -cne $report['validated_sha256'][$relative].ToLowerInvariant()) { Reject-Snapshot 'HASH_MISMATCH' }
}
if ($CheckPublished) {
    foreach ($relative in $manifest) {
        $candidateFile = Get-CheckedRelativePath $CandidateRoot $relative
        $publishedFile = Get-CheckedRelativePath $ProjectRoot $relative
        if (-not (Test-Path -LiteralPath $publishedFile -PathType Leaf)) { Reject-Snapshot 'MISSING_PUBLISHED_FILE' }
        if ((Get-FileHash -LiteralPath $candidateFile -Algorithm SHA256).Hash -cne (Get-FileHash -LiteralPath $publishedFile -Algorithm SHA256).Hash) { Reject-Snapshot 'PUBLISHED_BYTES_DIFFER' }
    }
}
@{ verified = $true; files_verified = $manifest.Count; report_hashes_verified = $report['validated_sha256'].Count; published_checked = [bool]$CheckPublished; reported_cases = $report['total_cases_passed']; verification_scope = 'byte snapshot and report structure only; does not rerun game rules or inspect Git' } | ConvertTo-Json -Compress
} catch { @{ verified = $false; error_code = $script:rejectedCode } | ConvertTo-Json -Compress; exit 1 }
exit 0
