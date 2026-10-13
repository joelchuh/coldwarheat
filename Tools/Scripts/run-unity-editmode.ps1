[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ProjectRoot,
    [Parameter(Mandatory)][string]$UnityEditor,
    [Parameter(Mandatory)][string]$RunId,
    [int]$TimeoutSeconds=180,
    [switch]$Preview
)
#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
Import-Module (Join-Path $PSScriptRoot 'unity-editmode-support.psm1') -Force
$plan=$null;$process=$null;$reserved=$false;$timedOut=$false
$timer=[Diagnostics.Stopwatch]::StartNew()
try {
    $plan=New-UnityVerificationPlan -ProjectRoot $ProjectRoot -UnityEditor $UnityEditor -RunId $RunId -TimeoutSeconds $TimeoutSeconds
    if ($Preview) {
        [ordered]@{status='preview';project=$plan.ProjectRoot;unity=$plan.UnityEditor;unity_version=$plan.UnityVersion;output=$plan.OutputDirectory;arguments=$plan.Arguments;timeout_seconds=$plan.TimeoutSeconds;side_effects='Actual run imports project and writes Unity caches/meta plus a new TestResults directory; no Git actions.'} | ConvertTo-Json -Depth 4
        exit 0
    }
    if (@(Get-Process -Name Unity -ErrorAction SilentlyContinue).Count -gt 0) { throw 'UNITY_EDITOR_ALREADY_RUNNING' }
    $parent=[IO.Path]::GetDirectoryName($plan.OutputDirectory)
    [IO.Directory]::CreateDirectory($parent) | Out-Null
    # New-Item without Force rejects an existing directory; never resume in old evidence.
    New-Item -ItemType Directory -Path $plan.OutputDirectory -ErrorAction Stop | Out-Null
    $reserved=$true
    $start=[Diagnostics.ProcessStartInfo]::new();$start.FileName=$plan.UnityEditor;$start.WorkingDirectory=$plan.ProjectRoot
    $start.UseShellExecute=$false;$start.CreateNoWindow=$true;$start.WindowStyle=[Diagnostics.ProcessWindowStyle]::Hidden
    foreach ($argument in $plan.Arguments) { $start.ArgumentList.Add($argument) }
    $process=[Diagnostics.Process]::new();$process.StartInfo=$start
    if (-not $process.Start()) { throw 'UNITY_START_FAILED' }
    if (-not $process.WaitForExit($plan.TimeoutSeconds*1000)) {
        $timedOut=$true
        $process.Kill() # Only the child created above, not a name-based process selection or process tree.
        if (-not $process.WaitForExit(5000)) { throw 'UNITY_TIMEOUT_PROCESS_STILL_RUNNING' }
        throw 'UNITY_TIMEOUT'
    }
    $summary=Read-UnityTestResult -Path $plan.XmlPath -ExpectedFilter $plan.Policy.test_filter -ExitCode $process.ExitCode -MaxXmlBytes $plan.Policy.max_xml_bytes -MaxTestCases $plan.Policy.max_test_cases
} catch {
    $summary=[ordered]@{status='failed';error_code=$_.Exception.Message;timed_out=$timedOut}
} finally {
    if ($null -ne $process) { $process.Dispose() }
    $timer.Stop()
}
$summary['schema_version']=1;$summary['run_id']=$RunId;$summary['duration_ms']=$timer.ElapsedMilliseconds
if ($null -ne $plan) {
    $summary['project']=$plan.ProjectRoot;$summary['unity_version']=$plan.UnityVersion;$summary['policy_sha256']=$plan.PolicySha256
    $summary['output_directory']=$plan.OutputDirectory;$summary['xml']=$plan.XmlPath;$summary['log']=$plan.LogPath
}
$json=$summary | ConvertTo-Json -Depth 6
if ($reserved) {
    $summaryPath=Join-Path $plan.OutputDirectory 'summary.json'
    $stream=[IO.File]::Open($summaryPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try { $bytes=[Text.UTF8Encoding]::new($false).GetBytes($json);$stream.Write($bytes,0,$bytes.Length) } finally { $stream.Dispose() }
}
Write-Output $json
if ($summary.status -eq 'passed') { exit 0 }
exit 1
