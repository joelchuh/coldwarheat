[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$UnityEditor,
    [string]$FixtureRoot = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'coldwarheat-unity-workflow-tests', [Guid]::NewGuid().ToString('N'))
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot '../Scripts/unity-editmode-support.psm1') -Force
if (-not [IO.Path]::IsPathFullyQualified($FixtureRoot) -or $FixtureRoot -notmatch '^[A-Za-z]:[\\/]') { throw 'LOCAL_ABSOLUTE_FIXTURE_REQUIRED' }
$FixtureRoot=[IO.Path]::GetFullPath($FixtureRoot)
if (Test-Path -LiteralPath $FixtureRoot) { throw 'NEW_FIXTURE_REQUIRED' }
$cursor=$FixtureRoot
while ($cursor) {
    if ((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'LINK_FIXTURE_REJECTED' }
    $cursor=[IO.Path]::GetDirectoryName($cursor)
}
New-Item -ItemType Directory -Path $FixtureRoot | Out-Null
$checks=[Collections.Generic.List[object]]::new()
function Check([string]$Name,[scriptblock]$Body) {
    try { & $Body; $checks.Add(@{name=$Name;passed=$true}) }
    catch { $checks.Add(@{name=$Name;passed=$false;error=$_.Exception.Message}) }
}
function Expect-Error([string]$Code,[scriptblock]$Body) {
    $message=$null
    try { & $Body | Out-Null } catch { $message=$_.Exception.Message }
    if ($message -cne $Code) { throw "Expected $Code, got $message" }
}
function Assert([bool]$Condition,[string]$Message) { if (-not $Condition) { throw $Message } }
function Fixture([string]$Name) {
    $root=Join-Path $FixtureRoot $Name
    foreach ($folder in @('Assets','Packages','ProjectSettings','Tools/Configuration')) { New-Item -ItemType Directory -Path (Join-Path $root $folder) -Force | Out-Null }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../../ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $root 'ProjectSettings/ProjectVersion.txt')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../Configuration/unity-editmode.json') -Destination (Join-Path $root 'Tools/Configuration/unity-editmode.json')
    [IO.File]::WriteAllText((Join-Path $root 'Packages/manifest.json'),'{}')
    return $root
}
function Plan([string]$Root,[string]$Run='sample') { New-UnityVerificationPlan -ProjectRoot $Root -UnityEditor $UnityEditor -RunId $Run -TimeoutSeconds 180 }
function XmlFile([string]$Name,[string]$Text) {
    $path=Join-Path $FixtureRoot ($Name+'.xml');[IO.File]::WriteAllText($path,$Text);return $path
}
function Result([string]$Path,[int]$Exit=0) { Read-UnityTestResult -Path $Path -ExpectedFilter 'ColdWarStruggle.Tests' -ExitCode $Exit -MaxXmlBytes 33554432 -MaxTestCases 100000 }
function Invoke-Entry([string]$Root,[bool]$PreviewOnly) {
    $start=[Diagnostics.ProcessStartInfo]::new()
    $start.FileName=(Get-Process -Id $PID).Path
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true
    $start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
    foreach ($a in @('-NoProfile','-File',(Join-Path $PSScriptRoot '../Scripts/run-unity-editmode.ps1'),'-ProjectRoot',$Root,'-UnityEditor',$UnityEditor,'-RunId','sample')) { $start.ArgumentList.Add($a) }
    if ($PreviewOnly) { $start.ArgumentList.Add('-Preview') }
    $child=[Diagnostics.Process]::Start($start)
    try {
        $stdout=$child.StandardOutput.ReadToEndAsync();$stderr=$child.StandardError.ReadToEndAsync()
        if (-not $child.WaitForExit(15000)) { $child.Kill();$child.WaitForExit();throw 'ENTRY_TEST_TIMEOUT' }
        $text=$stdout.GetAwaiter().GetResult();$errorText=$stderr.GetAwaiter().GetResult()
        Assert ([string]::IsNullOrWhiteSpace($errorText)) $errorText
        return @{exit_code=$child.ExitCode;json=($text | ConvertFrom-Json)}
    } finally { $child.Dispose() }
}
$valid='<test-run total="2" passed="2" failed="0" skipped="0" inconclusive="0" result="Passed"><test-suite><test-case id="1" fullname="ColdWarStruggle.Tests.A.One" result="Passed"/><test-case id="2" fullname="ColdWarStruggle.Tests.A.Two" result="Passed"/></test-suite></test-run>'
Check 'preview-is-readonly-and-arguments-are-separate' {
    $root=Fixture 'project with spaces';$p=Plan $root
    Assert ($p.OutputDirectory -eq (Join-Path $root 'TestResults/UnityRuns/sample')) 'Wrong output path'
    Assert (-not (Test-Path -LiteralPath (Join-Path $root 'TestResults'))) 'Planning wrote files'
    Assert ($p.Arguments -contains $root) 'Project path must be one argument'
    Assert ($p.Arguments -contains 'ColdWarStruggle.Tests') 'Missing test filter'
    Assert (-not ($p.Arguments -contains '-quit')) 'Unity tests must not use -quit'
}
foreach ($badRunId in @('../escape','a/b','a b','CON','x;whoami','')) {
    Check ('reject-run-id-'+$badRunId) { $root=Fixture ('run-'+$checks.Count);Expect-Error 'INVALID_RUN_ID' { Plan $root $badRunId } }
}
Check 'reject-existing-output-without-changing-it' {
    $root=Fixture 'existing';$output=Join-Path $root 'TestResults/UnityRuns/sample';New-Item -ItemType Directory -Path $output -Force | Out-Null
    $sentinel=Join-Path $output 'old.xml';[IO.File]::WriteAllText($sentinel,'previous result')
    Expect-Error 'OUTPUT_ALREADY_EXISTS' { Plan $root }
    Assert ([IO.File]::ReadAllText($sentinel) -eq 'previous result') 'Existing output changed'
}
Check 'entry-preview-exits-zero-without-creating-output' {
    $root=Fixture 'entry preview with spaces';$r=Invoke-Entry $root $true
    Assert ($r.exit_code -eq 0 -and $r.json.status -ceq 'preview') 'Preview entry failed'
    Assert (-not (Test-Path -LiteralPath (Join-Path $root 'TestResults'))) 'Preview entry wrote output'
}
Check 'entry-rejects-existing-output-and-preserves-evidence' {
    $root=Fixture 'entry existing';$output=Join-Path $root 'TestResults/UnityRuns/sample';New-Item -ItemType Directory -Path $output -Force | Out-Null
    $sentinel=Join-Path $output 'summary.json';[IO.File]::WriteAllText($sentinel,'previous evidence')
    $r=Invoke-Entry $root $false
    Assert ($r.exit_code -eq 1 -and $r.json.error_code -ceq 'OUTPUT_ALREADY_EXISTS') 'Existing output accepted'
    Assert ([IO.File]::ReadAllText($sentinel) -ceq 'previous evidence') 'Entry overwrote evidence'
    Assert (@(Get-ChildItem -LiteralPath $output).Count -eq 1) 'Entry added files to old evidence'
}
Check 'reject-version-mismatch-before-start' {
    $root=Fixture 'version';[IO.File]::WriteAllText((Join-Path $root 'ProjectSettings/ProjectVersion.txt'),"m_EditorVersion: 1.2.3f1`nm_EditorVersionWithRevision: 1.2.3f1 (abcdef123456)`n")
    Expect-Error 'UNITY_VERSION_MISMATCH' { Plan $root }
}
Check 'reject-output-policy-escape' {
    $root=Fixture 'policy-escape';$path=Join-Path $root 'Tools/Configuration/unity-editmode.json';$c=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json;$c.output_relative_root='../outside';$c | ConvertTo-Json | Set-Content -LiteralPath $path
    Expect-Error 'INVALID_POLICY' { Plan $root }
}
Check 'reject-policy-unknown-field' {
    $root=Fixture 'policy-extra';$path=Join-Path $root 'Tools/Configuration/unity-editmode.json';$c=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable;$c['execute']='anything';$c | ConvertTo-Json | Set-Content -LiteralPath $path
    Expect-Error 'INVALID_POLICY' { Plan $root }
}
foreach ($policyField in @('dataset_id','test_platform')) {
    foreach ($arrayShape in @('empty','single')) {
        Check ('reject-policy-array-'+$policyField+'-'+$arrayShape) {
            $root=Fixture ('policy-array-'+$checks.Count);$path=Join-Path $root 'Tools/Configuration/unity-editmode.json'
            $c=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable
            $c[$policyField]=if($arrayShape -eq 'empty') { ,@() } else { ,@($c[$policyField]) }
            $c | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $path
            Expect-Error 'INVALID_POLICY' { Plan $root }
        }
    }
}
Check 'reject-linked-output-parent' {
    $root=Fixture 'linked';$other=Join-Path $FixtureRoot 'link-target';New-Item -ItemType Directory -Path $other | Out-Null
    New-Item -ItemType Junction -Path (Join-Path $root 'TestResults') -Target $other | Out-Null
    Expect-Error 'REPARSE_POINT_REJECTED' { Plan $root }
}
Check 'valid-xml-counts-real-test-cases' {
    $path=XmlFile 'valid' $valid;$result=Result $path
    Assert ($result.status -eq 'passed' -and $result.total -eq 2 -and $result.passed -eq 2) 'Wrong valid summary'
    Assert ($result.xml_sha256 -eq (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()) 'Wrong evidence hash'
}
Check 'nonzero-process-exit-cannot-pass' {
    $r=Result (XmlFile 'nonzero' $valid) 2;Assert ($r.status -eq 'failed' -and $r.error_code -eq 'UNITY_EXIT_NONZERO') 'Ignored process exit'
}
Check 'failed-case-produces-failed-summary' {
    $xml=$valid.Replace('passed="2" failed="0"','passed="1" failed="1"').Replace('result="Passed"><test-suite>','result="Failed"><test-suite>').Replace('Two" result="Passed"','Two" result="Failed"')
    $r=Result (XmlFile 'failed' $xml) 2;Assert ($r.status -eq 'failed' -and $r.failed -eq 1) 'Failure not counted'
}
Check 'skipped-case-is-not-complete-success' {
    $xml=$valid.Replace('passed="2"','passed="1"').Replace('skipped="0"','skipped="1"').Replace('Two" result="Passed"','Two" result="Skipped"')
    $r=Result (XmlFile 'skipped' $xml);Assert ($r.status -eq 'failed' -and $r.skipped -eq 1) 'Skip accepted'
}
Check 'inconsistent-root-counts-rejected' { Expect-Error 'XML_COUNT_MISMATCH' { Result (XmlFile 'counts' $valid.Replace('passed="2"','passed="1"')) } }
Check 'missing-report-rejected' { Expect-Error 'TEST_REPORT_MISSING' { Result (Join-Path $FixtureRoot 'missing.xml') } }
Check 'empty-test-run-rejected' { Expect-Error 'INVALID_TEST_REPORT' { Result (XmlFile 'zero' '<test-run total="0" passed="0" failed="0" skipped="0" inconclusive="0" result="Passed"/>') } }
Check 'malformed-xml-rejected' { Expect-Error 'INVALID_TEST_REPORT' { Result (XmlFile 'malformed' '<test-run') } }
Check 'dtd-rejected-without-external-resolution' { Expect-Error 'INVALID_TEST_REPORT' { Result (XmlFile 'dtd' ('<!DOCTYPE test-run [<!ENTITY x "text">]>'+$valid)) } }
Check 'foreign-namespace-test-rejected' { Expect-Error 'UNEXPECTED_TEST_CASE' { Result (XmlFile 'foreign' $valid.Replace('ColdWarStruggle.Tests.A.Two','Other.Tests.A.Two')) } }
Check 'duplicate-case-id-rejected' { Expect-Error 'INVALID_TEST_REPORT' { Result (XmlFile 'duplicate' $valid.Replace('id="2"','id="1"')) } }
Check 'unknown-case-result-rejected' { Expect-Error 'INVALID_TEST_REPORT' { Result (XmlFile 'unknown' $valid.Replace('Two" result="Passed"','Two" result="Maybe"')) } }
Check 'failed-root-cannot-pass-even-with-passed-cases' {
    $r=Result (XmlFile 'failed-root' $valid.Replace('result="Passed"><test-suite>','result="Failed"><test-suite>'))
    Assert ($r.status -ceq 'failed') 'Ignored root failure'
}
$failed=@($checks | Where-Object { -not $_.passed }).Count
$summary=@{ total=$checks.Count;passed=$checks.Count-$failed;failed=$failed;fixture_root=$FixtureRoot;tests=$checks }
$json=$summary | ConvertTo-Json -Depth 6
[IO.File]::WriteAllText((Join-Path $FixtureRoot 'summary.json'),$json)
Write-Output $json
if ($failed -ne 0) { exit 1 }
exit 0
