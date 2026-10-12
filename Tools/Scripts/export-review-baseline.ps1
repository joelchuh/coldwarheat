#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectRoot,
    [Parameter(Mandatory)][string]$Destination,
    [Parameter(Mandatory)][string]$GitExecutable,
    [Parameter(Mandatory)][string]$ExpectedHead
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$script:rejectedCode = 'IO_FAILURE'
$script:destinationCreated = $false
$pathComparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
$utf8 = [Text.UTF8Encoding]::new($false, $true)
function Reject-Export([string]$Code) { $script:rejectedCode = $Code; throw 'Baseline export rejected' }
function Assert-NoLink([string]$Path) {
    # Check ancestors root-first; never enumerate through an unchecked link.
    $ancestors = [Collections.Generic.List[string]]::new(); $cursor = $Path
    while ($cursor) { $ancestors.Add($cursor); $cursor = [IO.Path]::GetDirectoryName($cursor) }
    for ($i = $ancestors.Count - 1; $i -ge 0; $i--) {
        $item = Get-Item -LiteralPath $ancestors[$i] -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { Reject-Export 'REPARSE_POINT_REJECTED' }
    }
}
function Get-LocalPath([string]$Path) {
    if (-not [IO.Path]::IsPathFullyQualified($Path) -or $Path.StartsWith('\\') -or $Path.StartsWith('//') -or $Path -match '[\x00-\x1f]') { Reject-Export 'ABSOLUTE_LOCAL_PATH_REQUIRED' }
    $full = [IO.Path]::GetFullPath($Path)
    if ($IsWindows) {
        if ($full -notmatch '^[A-Za-z]:\\' -or $full.Substring(3) -match '[:<>"|?*]') { Reject-Export 'ABSOLUTE_LOCAL_PATH_REQUIRED' }
        if ([IO.DriveInfo]::new([IO.Path]::GetPathRoot($full)).DriveType -ne [IO.DriveType]::Fixed) { Reject-Export 'LOCAL_FIXED_VOLUME_REQUIRED' }
    }
    $segments = $full.Replace('\','/').Split('/')
    foreach ($part in $segments) {
        if ($part -match '[ .]$' -or $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)') { Reject-Export 'UNSAFE_LOCAL_PATH' }
    }
    Assert-NoLink $full
    return $full.TrimEnd([IO.Path]::DirectorySeparatorChar)
}
function Assert-RegularTree([string]$Root) {
    $pending = [Collections.Generic.Stack[string]]::new(); $pending.Push($Root)
    while ($pending.Count) {
        $current = $pending.Pop(); Assert-NoLink $current
        foreach ($item in Get-ChildItem -LiteralPath $current -Force) {
            if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) { Reject-Export 'REPARSE_POINT_REJECTED' }
            if ($item.PSIsContainer) { $pending.Push($item.FullName) }
        }
    }
}
function Get-TrackedPath([string]$Root,[string]$Relative) {
    if ([string]::IsNullOrWhiteSpace($Relative) -or [IO.Path]::IsPathRooted($Relative) -or $Relative -match '[\\<>:"|?*\x00-\x1f]') { Reject-Export 'UNSAFE_TRACKED_PATH' }
    $parts = $Relative.Split('/')
    foreach ($part in $parts) {
        if ($part -in @('', '.', '..') -or $part -ieq '.git' -or $part -match '[ .]$' -or $part -match '^(?i:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(?:\.|$)') { Reject-Export 'UNSAFE_TRACKED_PATH' }
    }
    if ($parts[0] -in @('.baseline-meta.json','.baseline-incomplete.json','.baseline-success.pending.json')) { Reject-Export 'RESERVED_METADATA_PATH' }
    $full = [IO.Path]::GetFullPath([IO.Path]::Combine($Root, ($parts -join [IO.Path]::DirectorySeparatorChar)))
    if (-not $full.StartsWith($Root + [IO.Path]::DirectorySeparatorChar, $pathComparison)) { Reject-Export 'UNSAFE_TRACKED_PATH' }
    Assert-NoLink $full
    return $full
}
function Invoke-ReadGit([string[]]$Arguments) {
    $start = [Diagnostics.ProcessStartInfo]::new($GitExecutable)
    $start.UseShellExecute = $false; $start.RedirectStandardOutput = $true; $start.RedirectStandardError = $true
    $start.StandardOutputEncoding = $utf8; $start.StandardErrorEncoding = $utf8
    foreach ($key in @($start.Environment.Keys)) { if ($key -like 'GIT_*') { $start.Environment.Remove($key) | Out-Null } }
    $start.Environment['GIT_CONFIG_NOSYSTEM'] = '1'
    $start.Environment['GIT_CONFIG_GLOBAL'] = $(if ($IsWindows) { 'NUL' } else { '/dev/null' })
    $start.Environment['GIT_OPTIONAL_LOCKS'] = '0'; $start.Environment['GIT_TERMINAL_PROMPT'] = '0'
    $start.Environment['GIT_NO_REPLACE_OBJECTS'] = '1'
    $start.Environment['GIT_ATTR_NOSYSTEM'] = '1'
    $nullDevice = if ($IsWindows) { 'NUL' } else { '/dev/null' }
    foreach ($arg in (@('--no-optional-locks','-c',('safe.directory='+$ProjectRoot),'-c','core.fsmonitor=false','-c','core.untrackedCache=false','-c','core.hooksPath=','-c',('core.excludesFile='+$nullDevice),'-c',('core.attributesFile='+$nullDevice),'-c',('core.worktree='+$ProjectRoot),'-C',$ProjectRoot,('--git-dir='+$script:gitDirectory),('--work-tree='+$ProjectRoot)) + $Arguments)) { $start.ArgumentList.Add($arg) }
    $process = [Diagnostics.Process]::new(); $process.StartInfo = $start
    try {
        $process.Start() | Out-Null
        $stdout = $process.StandardOutput.ReadToEndAsync(); $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(30000)) { $process.Kill($true); Reject-Export 'GIT_TIMEOUT' }
        $output = $stdout.GetAwaiter().GetResult(); $null = $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne 0) { Reject-Export 'GIT_READ_FAILED' }
        return $output
    } finally { $process.Dispose() }
}
function Assert-ExpectedHead {
    $head = (Invoke-ReadGit @('rev-parse','--verify','HEAD')).Trim()
    if ($head -cne $ExpectedHead) { Reject-Export 'HEAD_MISMATCH' }
}
function Assert-Clean {
    if ((Invoke-ReadGit @('status','--porcelain=v1','-z','--untracked-files=all','--ignore-submodules=all')).Length -ne 0) { Reject-Export 'SOURCE_NOT_CLEAN' }
}
function Write-NewUtf8([string]$Path,[string]$Text) {
    Assert-NoLink $Path
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try { $bytes = $utf8.GetBytes($Text); $stream.Write($bytes,0,$bytes.Length); $stream.Flush($true) } finally { $stream.Dispose() }
}
function Get-Sha256([string]$Path) { return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant() }
try {
    if ($ExpectedHead -cnotmatch '^(?:[a-f0-9]{40}|[a-f0-9]{64})$') { Reject-Export 'INVALID_EXPECTED_HEAD' }
    $ProjectRoot = Get-LocalPath $ProjectRoot; $Destination = Get-LocalPath $Destination; $GitExecutable = Get-LocalPath $GitExecutable
    foreach ($root in @($ProjectRoot,$Destination)) {
        if (-not $root -or $root -eq [IO.Path]::GetPathRoot($root).TrimEnd([IO.Path]::DirectorySeparatorChar)) { Reject-Export 'INVALID_ROOT' }
    }
    if (-not (Test-Path -LiteralPath $GitExecutable -PathType Leaf)) { Reject-Export 'INVALID_GIT_EXECUTABLE' }
    if ($ProjectRoot.Equals($Destination,$pathComparison) -or $Destination.StartsWith($ProjectRoot+[IO.Path]::DirectorySeparatorChar,$pathComparison) -or $ProjectRoot.StartsWith($Destination+[IO.Path]::DirectorySeparatorChar,$pathComparison)) { Reject-Export 'OVERLAPPING_ROOTS' }
    if (Test-Path -LiteralPath $Destination) { Reject-Export 'DESTINATION_EXISTS' }
    if (-not (Test-Path -LiteralPath ([IO.Path]::GetDirectoryName($Destination)) -PathType Container)) { Reject-Export 'DESTINATION_PARENT_MISSING' }
    $script:gitDirectory = Join-Path $ProjectRoot '.git'
    Assert-NoLink $script:gitDirectory
    if (-not (Test-Path -LiteralPath $script:gitDirectory -PathType Container)) { Reject-Export 'UNSUPPORTED_REPOSITORY' }
    # Git follows commondir before reading config; reject redirection before any Git process.
    if (Test-Path -LiteralPath (Join-Path $script:gitDirectory 'commondir')) { Reject-Export 'UNSUPPORTED_REPOSITORY' }
    Assert-RegularTree $script:gitDirectory
    $configPath = Join-Path $script:gitDirectory 'config'
    $config = [IO.File]::ReadAllText($configPath,$utf8)
    # Includes and filters can cross the declared filesystem/process boundary.
    # Repository extensions/worktree configuration and object alternates are deliberately unsupported.
    if ($config -match '(?im)^\s*\[\s*(?:include|includeif|filter|extensions)(?:[\s.\]])' -or (Test-Path -LiteralPath (Join-Path $script:gitDirectory 'objects/info/alternates')) -or (Test-Path -LiteralPath (Join-Path $script:gitDirectory 'config.worktree'))) { Reject-Export 'UNSAFE_GIT_CONFIGURATION' }
    $configDigest = Get-Sha256 $configPath
    $indexPath = Join-Path $script:gitDirectory 'index'; $indexDigest = Get-Sha256 $indexPath
    Assert-ExpectedHead
    $inventory = Invoke-ReadGit @('ls-files','--stage','-z')
    $flags = Invoke-ReadGit @('ls-files','-v','-z')
    foreach ($flag in $flags.Split([char]0,[StringSplitOptions]::RemoveEmptyEntries)) { if (-not $flag.StartsWith('H ')) { Reject-Export 'UNSUPPORTED_INDEX_FLAGS' } }
    $entries = [Collections.Generic.List[object]]::new()
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($row in $inventory.Split([char]0,[StringSplitOptions]::RemoveEmptyEntries)) {
        if ($row -cnotmatch '^(100644|100755) [a-f0-9]+ 0\t(.+)$') { Reject-Export 'UNSUPPORTED_TRACKED_ENTRY' }
        $relative = $Matches[2]
        $source = Get-TrackedPath $ProjectRoot $relative; $target = Get-TrackedPath $Destination $relative
        if (-not $names.Add($relative)) { Reject-Export 'DUPLICATE_TRACKED_PATH' }
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { Reject-Export 'MISSING_TRACKED_FILE' }
        $entries.Add(@{ relative=$relative; source=$source; target=$target })
    }
    if ($entries.Count -eq 0) { Reject-Export 'EMPTY_TRACKED_INVENTORY' }
    Assert-Clean
    Assert-NoLink $Destination
    if (Test-Path -LiteralPath $Destination) { Reject-Export 'DESTINATION_EXISTS' }
    New-Item -ItemType Directory -Path $Destination -ErrorAction Stop | Out-Null
    $script:destinationCreated = $true
    $markerPath = Join-Path $Destination '.baseline-incomplete.json'
    Write-NewUtf8 $markerPath ((@{ schema_version=1; complete=$false; head=$ExpectedHead; success_manifest='.baseline-meta.json'; description='Export started. Treat as incomplete unless .baseline-meta.json exists, parses, declares complete=true, and all listed SHA256 values match.' } | ConvertTo-Json -Compress)+"`n")
    $markerDigest = Get-Sha256 $markerPath
    $hashes = [ordered]@{}
    foreach ($entry in $entries) {
        Assert-NoLink $entry.source; Assert-NoLink $entry.target
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($entry.target)) | Out-Null
        Assert-NoLink $entry.target
        $inputStream = [IO.File]::Open($entry.source,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
        try {
            $outputStream = [IO.File]::Open($entry.target,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try { $inputStream.CopyTo($outputStream); $outputStream.Flush($true) } finally { $outputStream.Dispose() }
        } finally { $inputStream.Dispose() }
        $hashes[$entry.relative] = Get-Sha256 $entry.target
    }
    foreach ($entry in $entries) {
        Assert-NoLink $entry.source; Assert-NoLink $entry.target
        if ((Get-Sha256 $entry.source) -cne $hashes[$entry.relative] -or (Get-Sha256 $entry.target) -cne $hashes[$entry.relative]) { Reject-Export 'SOURCE_CHANGED' }
    }
    Assert-NoLink $configPath; Assert-NoLink $indexPath; Assert-RegularTree $script:gitDirectory
    Assert-ExpectedHead
    if ((Get-Sha256 $configPath) -cne $configDigest -or (Get-Sha256 $indexPath) -cne $indexDigest -or (Invoke-ReadGit @('ls-files','--stage','-z')) -cne $inventory -or (Invoke-ReadGit @('ls-files','-v','-z')) -cne $flags) { Reject-Export 'SOURCE_CHANGED' }
    Assert-Clean
    Assert-RegularTree $Destination
    $exportFiles = @(Get-ChildItem -LiteralPath $Destination -Force -Recurse -File)
    if ($exportFiles.Count -ne ($entries.Count+1) -or (Get-Sha256 $markerPath) -cne $markerDigest) { Reject-Export 'DESTINATION_CHANGED' }
    $manifest = [ordered]@{ schema_version=1; complete=$true; description='Exact tracked working-file bytes; no Git metadata or untracked files. Tool evidence only, not game validation or independent review.'; head=$ExpectedHead; copied_files=$entries.Count; source_clean_before_after=$true; sha256=$hashes }
    $pending = Join-Path $Destination '.baseline-success.pending.json'
    Write-NewUtf8 $pending (($manifest | ConvertTo-Json -Depth 8)+"`n")
    # Rename only this newly created file; do not overwrite or clean up any path.
    Assert-NoLink $Destination
    [IO.File]::Move($pending,(Join-Path $Destination '.baseline-meta.json'))
    @{ complete=$true; head=$ExpectedHead; copied_files=$entries.Count; manifest=(Join-Path $Destination '.baseline-meta.json'); source_clean_before_after=$true } | ConvertTo-Json -Compress
    exit 0
} catch {
    @{ complete=$false; error_code=$script:rejectedCode; destination_created=$script:destinationCreated; partial_output_retained=$script:destinationCreated } | ConvertTo-Json -Compress
    exit 1
}
