#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$GitExecutable,
    [string]$FixtureRoot = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'coldwarheat-baseline-tests-' + [Guid]::NewGuid().ToString('N')),
    [string[]]$TestCases
)
$ErrorActionPreference = 'Stop'
$known = @('exact-working-bytes','wrong-head','dirty-tracked','staged-change','untracked-source','existing-destination','nested-destination','source-subdirectory','source-link','destination-parent-link','tracked-directory-link','tracked-symlink-mode','submodule-mode','metadata-collision','invalid-head','relative-destination','missing-git','config-include','config-bom-include','config-filter','commondir-filter-rejected','external-excludes','external-attributes','hooks-disabled','assume-unchanged','skip-worktree','partial-copy-failure','concurrent-source-change','concurrent-head-change','ambient-git-environment','git-metadata-link','unsafe-tracked-path','runner-existing-root','runner-unknown-case')
if (-not $TestCases) { $TestCases = $known }
if (@($TestCases | Where-Object { $_ -cnotin $known }).Count -gt 0 -or @($TestCases | Select-Object -Unique).Count -ne $TestCases.Count) { @{ passed=0; failed=1; error_code='UNSUPPORTED_TEST_CASE' } | ConvertTo-Json -Compress; exit 1 }
function Assert([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Assert-SafePath([string]$Path) {
    if (-not [IO.Path]::IsPathFullyQualified($Path) -or $Path.StartsWith('\\') -or $Path.StartsWith('//')) { throw 'Absolute local fixture path required' }
    $all = [Collections.Generic.List[string]]::new(); $cursor=[IO.Path]::GetFullPath($Path)
    while ($cursor) { $all.Add($cursor); $cursor=[IO.Path]::GetDirectoryName($cursor) }
    for ($i=$all.Count-1; $i -ge 0; $i--) {
        $item=Get-Item -LiteralPath $all[$i] -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Fixture ancestor link rejected' }
    }
}
try {
    Assert-SafePath $FixtureRoot
    $FixtureRoot=[IO.Path]::GetFullPath($FixtureRoot)
    Assert (-not (Test-Path -LiteralPath $FixtureRoot)) 'Fixture root already exists'
    Assert-SafePath $GitExecutable
    Assert (Test-Path -LiteralPath $GitExecutable -PathType Leaf) 'Git executable required'
} catch { @{ passed=0; failed=1; error_code='INVALID_FIXTURE_INPUT' } | ConvertTo-Json -Compress; exit 1 }
$exporter=[IO.Path]::GetFullPath([IO.Path]::Combine($PSScriptRoot,'..','Scripts','export-review-baseline.ps1'))
if (-not (Test-Path -LiteralPath $exporter)) { @{ passed=0; failed=1; results=@(@{ id='exact-working-bytes'; passed=$false; reason='Export implementation is absent; no byte-preserving baseline can be produced.' }) } | ConvertTo-Json -Depth 5; exit 1 }
[IO.Directory]::CreateDirectory($FixtureRoot) | Out-Null
$pwsh=[IO.Path]::Combine($PSHOME, $(if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }))
function Start-ProcessExact([string]$Exe,[string[]]$Arguments,[hashtable]$Environment=@{}) {
    $si=[Diagnostics.ProcessStartInfo]::new($Exe); $si.UseShellExecute=$false; $si.RedirectStandardOutput=$true; $si.RedirectStandardError=$true
    foreach ($arg in $Arguments) { $si.ArgumentList.Add($arg) }
    foreach ($key in @($si.Environment.Keys)) { if ($key -like 'GIT_*') { $si.Environment.Remove($key) | Out-Null } }
    $si.Environment['GIT_CONFIG_NOSYSTEM']='1'; $si.Environment['GIT_CONFIG_GLOBAL']=$(if ($IsWindows) { 'NUL' } else { '/dev/null' }); $si.Environment['GIT_TERMINAL_PROMPT']='0'
    foreach ($key in $Environment.Keys) { $si.Environment[$key]=[string]$Environment[$key] }
    $p=[Diagnostics.Process]::new(); $p.StartInfo=$si; $p.Start() | Out-Null
    return @{ process=$p; stdout=$p.StandardOutput.ReadToEndAsync(); stderr=$p.StandardError.ReadToEndAsync() }
}
function Finish-ProcessExact($Running) {
    if (-not $Running.process.WaitForExit(60000)) { $Running.process.Kill($true); throw 'Fixture process timeout' }
    $result=@{ exit=$Running.process.ExitCode; out=$Running.stdout.GetAwaiter().GetResult(); err=$Running.stderr.GetAwaiter().GetResult() }; $Running.process.Dispose(); return $result
}
function Git([string]$Root,[string[]]$Arguments) {
    $r=Finish-ProcessExact (Start-ProcessExact $GitExecutable (@('-C',$Root,'-c','core.autocrlf=false','-c','core.hooksPath=','-c','user.name=Baseline Fixture','-c','user.email=fixture@example.invalid','-c','commit.gpgSign=false')+$Arguments))
    Assert ($r.exit -eq 0) ('Fixture Git failed: '+$r.err)
    return $r.out.TrimEnd("`r","`n")
}
function New-Fixture([string]$Name) {
    $root=Join-Path $FixtureRoot $Name; $source=Join-Path $root 'source'; $destination=Join-Path $root 'output'
    [IO.Directory]::CreateDirectory((Join-Path $source 'folder')) | Out-Null
    [IO.Directory]::CreateDirectory((Join-Path $root 'empty-template')) | Out-Null
    [IO.File]::WriteAllText((Join-Path $source '.gitignore'), "ignored.tmp`n")
    [IO.File]::WriteAllBytes((Join-Path $source 'folder/a space [1] 中文.bin'), [byte[]]@(0,1,2,13,10,255))
    [IO.File]::WriteAllText((Join-Path $source 'plain.txt'), "fixture`n")
    Git $source @('init','--quiet',('--template='+ (Join-Path $root 'empty-template'))) | Out-Null
    Git $source @('add','--all') | Out-Null; Git $source @('commit','--quiet','-m','fixture baseline') | Out-Null
    return @{ root=$root; source=$source; destination=$destination; head=(Git $source @('rev-parse','HEAD')); git=$GitExecutable }
}
function Input-Digests([string]$Root) {
    # No production source is passed here. Fixtures retain all files and Git objects.
    return (@(Get-ChildItem -LiteralPath $Root -Force -File -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName+'='+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }) -join "`n")
}
function Export-Args($f) { return @('-NoLogo','-NoProfile','-NonInteractive','-File',$exporter,'-ProjectRoot',$f.source,'-Destination',$f.destination,'-GitExecutable',$f.git,'-ExpectedHead',$f.head) }
function Make-Link([string]$Link,[string]$Target) { New-Item -ItemType $(if ($IsWindows) {'Junction'} else {'SymbolicLink'}) -Path $Link -Target $Target | Out-Null }
$results=[Collections.Generic.List[object]]::new()
foreach ($name in $TestCases) {
    $lock=$null
    try {
        $f=New-Fixture $name; $errorCode=$null; $mustRetain=$false; $environment=@{}; $mutate=$null
        switch ($name) {
            'exact-working-bytes' { [IO.File]::WriteAllText((Join-Path $f.source 'ignored.tmp'),'must not export') }
            'wrong-head' { $f.head='a'*40; $errorCode='HEAD_MISMATCH' }
            'dirty-tracked' { [IO.File]::AppendAllText((Join-Path $f.source 'plain.txt'),'dirty'); $errorCode='SOURCE_NOT_CLEAN' }
            'staged-change' { [IO.File]::AppendAllText((Join-Path $f.source 'plain.txt'),'staged'); Git $f.source @('add','plain.txt') | Out-Null; $errorCode='SOURCE_NOT_CLEAN' }
            'untracked-source' { [IO.File]::WriteAllText((Join-Path $f.source 'untracked.txt'),'untracked'); $errorCode='SOURCE_NOT_CLEAN' }
            'existing-destination' { [IO.Directory]::CreateDirectory($f.destination) | Out-Null; [IO.File]::WriteAllText((Join-Path $f.destination 'keep.txt'),'preserve'); $errorCode='DESTINATION_EXISTS' }
            'nested-destination' { $f.destination=Join-Path $f.source 'output'; $errorCode='OVERLAPPING_ROOTS' }
            'source-subdirectory' { $f.source=Join-Path $f.source 'folder'; $errorCode='UNSUPPORTED_REPOSITORY' }
            'source-link' { $link=Join-Path $f.root 'source-link'; Make-Link $link $f.source; $f.source=$link; $errorCode='REPARSE_POINT_REJECTED' }
            'destination-parent-link' { $target=Join-Path $f.root 'outside'; [IO.Directory]::CreateDirectory($target) | Out-Null; $link=Join-Path $f.root 'link'; Make-Link $link $target; $f.destination=Join-Path $link 'output'; $errorCode='REPARSE_POINT_REJECTED' }
            'tracked-directory-link' { $moved=Join-Path $f.root 'real-folder'; Move-Item -LiteralPath (Join-Path $f.source 'folder') -Destination $moved; Make-Link (Join-Path $f.source 'folder') $moved; $errorCode='REPARSE_POINT_REJECTED' }
            'tracked-symlink-mode' { $blob=Git $f.source @('rev-parse','HEAD:plain.txt'); Git $f.source @('update-index','--add','--cacheinfo',('120000,'+$blob+',link.txt')) | Out-Null; Git $f.source @('commit','--quiet','-m','symlink fixture') | Out-Null; [IO.File]::WriteAllText((Join-Path $f.source 'link.txt'),"fixture`n"); $f.head=Git $f.source @('rev-parse','HEAD'); $errorCode='UNSUPPORTED_TRACKED_ENTRY' }
            'submodule-mode' { Git $f.source @('update-index','--add','--cacheinfo',('160000,'+$f.head+',sub')) | Out-Null; Git $f.source @('commit','--quiet','-m','gitlink fixture') | Out-Null; $f.head=Git $f.source @('rev-parse','HEAD'); $errorCode='UNSUPPORTED_TRACKED_ENTRY' }
            'metadata-collision' { [IO.File]::WriteAllText((Join-Path $f.source '.baseline-meta.json'),'collision'); Git $f.source @('add','--all') | Out-Null; Git $f.source @('commit','--quiet','-m','metadata fixture') | Out-Null; $f.head=Git $f.source @('rev-parse','HEAD'); $errorCode='RESERVED_METADATA_PATH' }
            'invalid-head' { $f.head='HEAD'; $errorCode='INVALID_EXPECTED_HEAD' }
            'relative-destination' { $f.destination='relative-output'; $errorCode='ABSOLUTE_LOCAL_PATH_REQUIRED' }
            'missing-git' { $f.git=Join-Path $f.root 'no-git'; $errorCode='INVALID_GIT_EXECUTABLE' }
            'config-include' { [IO.File]::AppendAllText((Join-Path $f.source '.git/config'),"`n[include]`n`tpath = ../outside.config`n"); $errorCode='UNSAFE_GIT_CONFIGURATION' }
            'config-bom-include' { $path=Join-Path $f.source '.git/config'; [IO.File]::WriteAllText($path,([char]0xfeff+"[include]`n`tpath = ../outside.config`n"+[IO.File]::ReadAllText($path))); $errorCode='UNSAFE_GIT_CONFIGURATION' }
            'config-filter' { [IO.File]::AppendAllText((Join-Path $f.source '.git/config'),"`n[filter `"test`"]`n`tclean = arbitrary-program`n"); $errorCode='UNSAFE_GIT_CONFIGURATION' }
            'commondir-filter-rejected' {
                [IO.File]::WriteAllText((Join-Path $f.source '.gitattributes'),"plain.txt filter=commondirprobe`n")
                Git $f.source @('add','--all') | Out-Null; Git $f.source @('commit','--quiet','-m','commondir fixture') | Out-Null
                $f.head=Git $f.source @('rev-parse','HEAD'); $f.common=Join-Path $f.root 'common'
                Copy-Item -LiteralPath (Join-Path $f.source '.git') -Destination $f.common -Recurse
                Git $f.source @('config','--file',(Join-Path $f.common 'config'),'filter.commondirprobe.clean','printf called > .git/commondir-filter-called; printf "fixture\n"') | Out-Null
                [IO.File]::WriteAllText((Join-Path $f.source '.git/commondir'),$f.common)
                [IO.File]::WriteAllText((Join-Path $f.source 'plain.txt'),"fixturf`n")
                $errorCode='UNSUPPORTED_REPOSITORY'
            }
            'external-excludes' { $outside=Join-Path $f.root 'outside-excludes'; [IO.File]::WriteAllText($outside,"hidden.txt`n"); [IO.File]::WriteAllText((Join-Path $f.source 'hidden.txt'),'hidden'); Git $f.source @('config','core.excludesFile',$outside) | Out-Null; $errorCode='SOURCE_NOT_CLEAN' }
            'external-attributes' { $outside=Join-Path $f.root 'outside-attributes'; [IO.File]::WriteAllText($outside,"plain.txt text`n"); [IO.File]::WriteAllText((Join-Path $f.source 'plain.txt'),"fixture`r`n"); Git $f.source @('config','core.attributesFile',$outside) | Out-Null; $errorCode='SOURCE_NOT_CLEAN' }
            'hooks-disabled' { [IO.Directory]::CreateDirectory((Join-Path $f.source '.git/hooks')) | Out-Null; $hook=Join-Path $f.source '.git/hooks/fsmonitor-test'; [IO.File]::WriteAllText($hook,"#!/bin/sh`necho called > .git/fsmonitor-called`nexit 0`n"); if (-not $IsWindows) { [IO.File]::SetUnixFileMode($hook, [IO.UnixFileMode]::UserRead -bor [IO.UnixFileMode]::UserWrite -bor [IO.UnixFileMode]::UserExecute) }; Git $f.source @('config','core.fsmonitor',$hook.Replace('\','/')) | Out-Null }
            'assume-unchanged' { Git $f.source @('update-index','--assume-unchanged','plain.txt') | Out-Null; [IO.File]::AppendAllText((Join-Path $f.source 'plain.txt'),'hidden dirty'); $errorCode='UNSUPPORTED_INDEX_FLAGS' }
            'skip-worktree' { Git $f.source @('update-index','--skip-worktree','plain.txt') | Out-Null; $errorCode='UNSUPPORTED_INDEX_FLAGS' }
            'partial-copy-failure' { $mutate='lock' }
            'concurrent-source-change' { $mutate='bytes' }
            'concurrent-head-change' { $mutate='head' }
            'ambient-git-environment' { $environment=@{ GIT_DIR=(Join-Path $f.root 'wrong-git'); GIT_WORK_TREE=(Join-Path $f.root 'wrong-tree'); GIT_INDEX_FILE=(Join-Path $f.root 'wrong-index'); GIT_CONFIG_COUNT='1'; GIT_CONFIG_KEY_0='core.fsmonitor'; GIT_CONFIG_VALUE_0='arbitrary-program' } }
            'git-metadata-link' { $moved=Join-Path $f.root 'real-objects'; Move-Item -LiteralPath (Join-Path $f.source '.git/objects') -Destination $moved; Make-Link (Join-Path $f.source '.git/objects') $moved; $errorCode='REPARSE_POINT_REJECTED' }
            'unsafe-tracked-path' { $blob=Git $f.source @('rev-parse','HEAD:plain.txt'); Git $f.source @('-c','core.protectNTFS=false','update-index','--add','--cacheinfo',('100644,'+$blob+',NUL.txt')) | Out-Null; $errorCode='UNSAFE_TRACKED_PATH' }
        }
        if ($mutate) {
            $bytes=[byte[]]::new(16MB); $bytes[0]=7
            foreach ($n in 1..8) { [IO.File]::WriteAllBytes((Join-Path $f.source ('large-'+$n+'.bin')),$bytes) }
            Git $f.source @('add','--all') | Out-Null; Git $f.source @('commit','--quiet','-m','large fixture') | Out-Null; $f.head=Git $f.source @('rev-parse','HEAD')
            $errorCode=$(if ($mutate -eq 'head') { 'HEAD_MISMATCH' } elseif ($mutate -eq 'lock') { 'IO_FAILURE' } else { 'SOURCE_NOT_CLEAN' }); $mustRetain=$true
        }
        # These test the runner through its real public boundary, in another process.
        if ($name -in @('runner-existing-root','runner-unknown-case')) {
            $args=@('-NoLogo','-NoProfile','-NonInteractive','-File',$PSCommandPath,'-GitExecutable',$GitExecutable,'-FixtureRoot',$f.root)
            if ($name -eq 'runner-unknown-case') { $args+=@('-TestCases','../escape') }
            $r=Finish-ProcessExact (Start-ProcessExact $pwsh $args); $body=$r.out | ConvertFrom-Json
            Assert ($r.exit -ne 0 -and $body.failed -eq 1) 'Runner must refuse unsafe fixture request'
            $results.Add(@{id=$name;passed=$true}); continue
        }
        $before=$(if ($lock -or $name -in @('source-link','tracked-directory-link','git-metadata-link')) { $null } else { Input-Digests $f.source })
        $commonBefore=$(if ($name -eq 'commondir-filter-rejected') { Input-Digests $f.common } else { $null })
        $running=Start-ProcessExact $pwsh (Export-Args $f) $environment
        if ($mutate) {
            $timer=[Diagnostics.Stopwatch]::StartNew()
            while (-not (Test-Path -LiteralPath (Join-Path $f.destination '.baseline-incomplete.json'))) {
                if ($running.process.HasExited -or $timer.Elapsed.TotalSeconds -gt 30) { throw 'Could not observe incomplete marker before concurrent mutation' }
                Start-Sleep -Milliseconds 2
            }
            if ($mutate -eq 'head') { Git $f.source @('commit','--quiet','--allow-empty','-m','concurrent commit') | Out-Null }
            elseif ($mutate -eq 'lock') { $lock=[IO.File]::Open((Join-Path $f.source 'plain.txt'),[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::None) }
            else { [IO.File]::AppendAllText((Join-Path $f.source 'plain.txt'),'concurrent mutation') }
        }
        $r=Finish-ProcessExact $running
        if ($lock) { $lock.Dispose(); $lock=$null }
        if ($name -eq 'commondir-filter-rejected') {
            Assert (-not (Test-Path -LiteralPath (Join-Path $f.source '.git/commondir-filter-called'))) 'Commondir allowed an unchecked clean filter to execute'
            Assert ((Input-Digests $f.common) -ceq $commonBefore) 'Exporter changed common Git metadata'
        }
        $body=$r.out | ConvertFrom-Json
        if ($errorCode) {
            Assert ($r.exit -ne 0) 'Unsafe export unexpectedly succeeded'
            Assert ($body.error_code -eq $errorCode) ('Expected '+$errorCode+' but got '+$body.error_code+'; stderr='+$r.err)
            Assert (-not (Test-Path -LiteralPath (Join-Path $f.destination '.baseline-meta.json'))) 'Failure left success manifest'
            if ($mustRetain) { Assert (Test-Path -LiteralPath (Join-Path $f.destination '.baseline-incomplete.json')) 'Partial output must retain incomplete marker'; Assert (-not $body.complete) 'Partial result marked complete' }
            elseif ($name -ne 'existing-destination') { Assert (-not (Test-Path -LiteralPath $f.destination)) 'Preflight failure created destination' }
            if ($name -eq 'existing-destination') { Assert ([IO.File]::ReadAllText((Join-Path $f.destination 'keep.txt')) -eq 'preserve') 'Existing output changed' }
        } else {
            Assert ($r.exit -eq 0 -and $body.complete) ('Expected successful export: '+$r.out+' '+$r.err)
            $meta=Get-Content -LiteralPath (Join-Path $f.destination '.baseline-meta.json') -Raw | ConvertFrom-Json -AsHashtable
            Assert ($meta.complete -and $meta.head -eq $f.head -and $meta.copied_files -eq 3) 'Manifest does not identify exact baseline'
            Assert ($meta.sha256['plain.txt'] -eq 'e80b71cd14d3cbd65f4173abcbfcf01a545dbca32a72d575108b553a648cc96f') 'Fixture digest differs'
            Assert (-not (Test-Path -LiteralPath (Join-Path $f.destination '.git'))) 'Git metadata leaked'
            Assert (-not (Test-Path -LiteralPath (Join-Path $f.destination 'ignored.tmp'))) 'Ignored file leaked'
            Assert ((Get-ChildItem -LiteralPath $f.destination -Force -Recurse -File).Count -eq 5) 'Unexpected export file inventory'
            foreach ($relative in $meta.sha256.Keys) { Assert ((Get-FileHash -LiteralPath (Join-Path $f.destination $relative) -Algorithm SHA256).Hash.ToLowerInvariant() -eq (Get-FileHash -LiteralPath (Join-Path $f.source $relative) -Algorithm SHA256).Hash.ToLowerInvariant()) ('Byte mismatch: '+$relative) }
        }
        if ($null -ne $before -and -not $mutate) { Assert ((Input-Digests $f.source) -ceq $before) 'Exporter changed source working or Git bytes' }
        $results.Add(@{ id=$name; passed=$true; expected_error=$errorCode })
    } catch { $results.Add(@{ id=$name; passed=$false; reason=$_.Exception.Message }) }
    finally { if ($lock) { $lock.Dispose() } }
}
$failed=@($results | Where-Object { -not $_.passed }).Count
@{ passed=$results.Count-$failed; failed=$failed; fixture_root=$FixtureRoot; runtime=$PSVersionTable.PSVersion.ToString(); results=@($results) } | ConvertTo-Json -Depth 6
if ($failed) { exit 1 }; exit 0
