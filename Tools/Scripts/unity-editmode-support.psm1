Set-StrictMode -Version Latest
function Assert-LocalPhysicalPath([string]$Path) {
    if (-not $IsWindows -or -not [IO.Path]::IsPathFullyQualified($Path) -or $Path -notmatch '^[A-Za-z]:[\\/]') { throw 'LOCAL_ABSOLUTE_PATH_REQUIRED' }
    $full=[IO.Path]::GetFullPath($Path)
    $cursor=$full
    while ($cursor) {
        $item=Get-Item -LiteralPath $cursor -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'REPARSE_POINT_REJECTED' }
        $cursor=[IO.Path]::GetDirectoryName($cursor)
    }
    if ([IO.DriveInfo]::new([IO.Path]::GetPathRoot($full)).DriveType -ne [IO.DriveType]::Fixed) { throw 'FIXED_LOCAL_DRIVE_REQUIRED' }
    return $full
}
function Read-VerificationPolicy([string]$Path) {
    $text=[IO.File]::ReadAllText($Path)
    if ($text.Length -gt 8192) { throw 'INVALID_POLICY' }
    $document=$null
    try {
        $document=[System.Text.Json.JsonDocument]::Parse($text)
        if ($document.RootElement.ValueKind -ne [System.Text.Json.JsonValueKind]::Object) { throw 'INVALID_POLICY' }
        $keys=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $expected=@('schema_version','dataset_id','description','test_platform','test_filter','output_relative_root','max_xml_bytes','max_test_cases')
        foreach ($p in $document.RootElement.EnumerateObject()) { if ($p.Name -cnotin $expected -or -not $keys.Add($p.Name)) { throw 'INVALID_POLICY' } }
        if ($keys.Count -ne $expected.Count) { throw 'INVALID_POLICY' }
        $v=ConvertFrom-Json -InputObject $text -AsHashtable -Depth 8
        if ($v.schema_version -isnot [long] -and $v.schema_version -isnot [int]) { throw 'INVALID_POLICY' }
        foreach ($key in @('dataset_id','test_platform','description','test_filter','output_relative_root')) { if ($v[$key] -isnot [string] -or [string]::IsNullOrWhiteSpace($v[$key])) { throw 'INVALID_POLICY' } }
        if ($v.schema_version -ne 1 -or $v.dataset_id -cne 'workflow.unity_editmode' -or $v.test_platform -cne 'EditMode') { throw 'INVALID_POLICY' }
        if ($v.test_filter -cnotmatch '^[A-Za-z_]\w*(\.[A-Za-z_]\w*)+$' -or $v.output_relative_root -cnotmatch '^TestResults/[A-Za-z0-9_-]+$') { throw 'INVALID_POLICY' }
        foreach ($key in @('max_xml_bytes','max_test_cases')) { if (($v[$key] -isnot [long] -and $v[$key] -isnot [int]) -or $v[$key] -le 0) { throw 'INVALID_POLICY' } }
        if ($v.max_xml_bytes -gt 67108864 -or $v.max_test_cases -gt 1000000) { throw 'INVALID_POLICY' }
        return $v
    } catch { throw 'INVALID_POLICY' }
    finally { if ($null -ne $document) { $document.Dispose() } }
}
function New-UnityVerificationPlan {
    param([string]$ProjectRoot,[string]$UnityEditor,[string]$RunId,[int]$TimeoutSeconds)
    if ($RunId -cnotmatch '^[a-z0-9][a-z0-9-]{0,63}$' -or $RunId -match '^(con|prn|aux|nul|com[1-9]|lpt[1-9])$') { throw 'INVALID_RUN_ID' }
    if ($TimeoutSeconds -lt 30 -or $TimeoutSeconds -gt 1800) { throw 'INVALID_TIMEOUT' }
    $root=Assert-LocalPhysicalPath $ProjectRoot
    if ($root.TrimEnd('\') -eq [IO.Path]::GetPathRoot($root).TrimEnd('\') -or -not [IO.Directory]::Exists($root)) { throw 'INVALID_PROJECT_ROOT' }
    $root=$root.TrimEnd('\')
    foreach ($relative in @('Assets','Packages/manifest.json','ProjectSettings/ProjectVersion.txt','Tools/Configuration/unity-editmode.json')) {
        $path=Assert-LocalPhysicalPath (Join-Path $root $relative)
        if (-not (Test-Path -LiteralPath $path)) { throw 'MISSING_PROJECT_INPUT' }
    }
    $editor=Assert-LocalPhysicalPath $UnityEditor
    if (-not [IO.File]::Exists($editor) -or [IO.Path]::GetFileName($editor) -ine 'Unity.exe') { throw 'INVALID_UNITY_EDITOR' }
    $versionText=[IO.File]::ReadAllText((Join-Path $root 'ProjectSettings/ProjectVersion.txt'))
    $version=[regex]::Match($versionText,'(?m)^m_EditorVersion: ([^\s]+)\r?$')
    $revision=[regex]::Match($versionText,'(?m)^m_EditorVersionWithRevision: ([^\s]+) \(([a-f0-9]+)\)\r?$')
    if (-not $version.Success -or -not $revision.Success -or $version.Groups[1].Value -cne $revision.Groups[1].Value) { throw 'INVALID_PROJECT_VERSION' }
    $expected=$version.Groups[1].Value+'_'+$revision.Groups[2].Value
    if ([Diagnostics.FileVersionInfo]::GetVersionInfo($editor).ProductVersion -cne $expected) { throw 'UNITY_VERSION_MISMATCH' }
    $policyPath=Join-Path $root 'Tools/Configuration/unity-editmode.json'
    $policy=Read-VerificationPolicy $policyPath
    $output=Assert-LocalPhysicalPath (Join-Path (Join-Path $root $policy.output_relative_root) $RunId)
    if (-not $output.StartsWith($root+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'OUTPUT_PATH_ESCAPE' }
    if (Test-Path -LiteralPath $output) { throw 'OUTPUT_ALREADY_EXISTS' }
    $xml=Join-Path $output 'results.xml';$log=Join-Path $output 'editor.log'
    return [pscustomobject]@{
        ProjectRoot=$root;UnityEditor=$editor;UnityVersion=$expected;RunId=$RunId;TimeoutSeconds=$TimeoutSeconds
        OutputDirectory=$output;XmlPath=$xml;LogPath=$log;Policy=$policy
        PolicySha256=(Get-FileHash -LiteralPath $policyPath -Algorithm SHA256).Hash.ToLowerInvariant()
        Arguments=@('-batchmode','-nographics','-projectPath',$root,'-runTests','-testPlatform',$policy.test_platform,'-testFilter',$policy.test_filter,'-testResults',$xml,'-logFile',$log)
    }
}
function Read-UnityTestResult {
    param([string]$Path,[string]$ExpectedFilter,[int]$ExitCode,[int]$MaxXmlBytes,[int]$MaxTestCases)
    $full=Assert-LocalPhysicalPath $Path
    if (-not [IO.File]::Exists($full)) { throw 'TEST_REPORT_MISSING' }
    if ($MaxXmlBytes -le 0 -or $MaxTestCases -le 0 -or [string]::IsNullOrWhiteSpace($ExpectedFilter) -or (Get-Item -LiteralPath $full).Length -gt $MaxXmlBytes) { throw 'INVALID_TEST_REPORT' }
    $reader=$null
    try {
        $settings=[Xml.XmlReaderSettings]::new();$settings.DtdProcessing=[Xml.DtdProcessing]::Prohibit;$settings.XmlResolver=$null;$settings.MaxCharactersInDocument=$MaxXmlBytes
        $reader=[Xml.XmlReader]::Create($full,$settings)
        $document=[Xml.XmlDocument]::new();$document.XmlResolver=$null;$document.Load($reader)
    } catch { throw 'INVALID_TEST_REPORT' }
    finally { if ($null -ne $reader) { $reader.Dispose() } }
    $root=$document.DocumentElement
    if ($null -eq $root -or $root.Name -cne 'test-run') { throw 'INVALID_TEST_REPORT' }
    $counts=@{}
    foreach ($key in @('total','passed','failed','skipped','inconclusive')) {
        $value=0
        if (-not [int]::TryParse($root.GetAttribute($key),[ref]$value) -or $value -lt 0 -or $value -gt $MaxTestCases) { throw 'INVALID_TEST_REPORT' }
        $counts[$key]=$value
    }
    $cases=@($document.SelectNodes('//test-case'))
    if ($counts.total -eq 0 -or $cases.Count -gt $MaxTestCases) { throw 'INVALID_TEST_REPORT' }
    $actual=@{passed=0;failed=0;skipped=0;inconclusive=0}
    $ids=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($case in $cases) {
        $id=$case.GetAttribute('id');$name=$case.GetAttribute('fullname');$result=$case.GetAttribute('result')
        if ([string]::IsNullOrWhiteSpace($id) -or -not $ids.Add($id) -or $result -cnotin @('Passed','Failed','Skipped','Inconclusive')) { throw 'INVALID_TEST_REPORT' }
        if (-not $name.StartsWith($ExpectedFilter+'.',[StringComparison]::Ordinal)) { throw 'UNEXPECTED_TEST_CASE' }
        $actual[$result.ToLowerInvariant()]++
    }
    if ($cases.Count -ne $counts.total) { throw 'XML_COUNT_MISMATCH' }
    foreach ($key in $actual.Keys) { if ($actual[$key] -ne $counts[$key]) { throw 'XML_COUNT_MISMATCH' } }
    $passed=$ExitCode -eq 0 -and $counts.passed -eq $counts.total -and $root.GetAttribute('result') -ceq 'Passed'
    return [ordered]@{
        status=$(if ($passed) {'passed'} else {'failed'})
        error_code=$(if ($passed) {$null} elseif ($ExitCode -ne 0) {'UNITY_EXIT_NONZERO'} else {'TESTS_NOT_PASSED'})
        total=$counts.total;passed=$counts.passed;failed=$counts.failed;skipped=$counts.skipped;inconclusive=$counts.inconclusive
        unity_exit_code=$ExitCode;xml_sha256=(Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}
Export-ModuleMember -Function New-UnityVerificationPlan,Read-UnityTestResult
