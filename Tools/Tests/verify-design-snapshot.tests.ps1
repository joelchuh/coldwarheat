[CmdletBinding()]
param(
    [string]$FixtureRoot = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'coldwarheat-snapshot-tests', [Guid]::NewGuid().ToString('N')),
    [string[]]$TestCases
)
$ErrorActionPreference = 'Stop'
$knownCases = @('snapshot-current-bytes', 'hash-changed-after-report', 'manifest-path-escape', 'report-path-escape', 'hash-reference-escape', 'duplicate-manifest', 'case-alias-manifest', 'unlisted-candidate-file', 'missing-candidate-file', 'rooted-manifest-path', 'junction-reference-rejected', 'published-current', 'published-bytes-differ', 'published-file-missing', 'published-same-root', 'candidate-volume-root', 'report-duplicate-json-key', 'report-not-object', 'report-empty-hashes', 'report-invalid-hash', 'report-count-inconsistent', 'report-failed-result', 'report-path-alias', 'report-malformed-json', 'report-comment-json', 'manifest-not-array', 'empty-manifest', 'report-boolean-count', 'report-results-length', 'report-duplicate-result-id', 'test-runner-existing-root', 'test-runner-unsafe-name', 'test-runner-link-parent', 'verifier-caller-success-status', 'test-runner-caller-success-status', 'extended-candidate-root', 'extended-manifest-input', 'test-runner-extended-root')
if (-not $TestCases) { $TestCases = $knownCases }
$allowed = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($name in $knownCases) { $allowed.Add($name) | Out-Null }
$requested = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($name in $TestCases) {
    if (-not $allowed.Contains($name) -or -not $requested.Add($name)) { @{ error_code = 'UNSUPPORTED_TEST_CASE'; failed = 1 } | ConvertTo-Json -Compress; exit 1 }
}
$FixtureRoot = [IO.Path]::GetFullPath($FixtureRoot)
if ($FixtureRoot -notmatch '^[A-Za-z]:\\') { @{ error_code = 'LOCAL_PATH_REQUIRED'; failed = 1 } | ConvertTo-Json -Compress; exit 1 }
$ancestors = [Collections.Generic.List[string]]::new()
$cursor = $FixtureRoot
while ($cursor) {
    $ancestors.Add($cursor)
    $cursor = [IO.Path]::GetDirectoryName($cursor)
}
for ($i = $ancestors.Count - 1; $i -ge 0; $i--) {
    $item = Get-Item -LiteralPath $ancestors[$i] -Force -ErrorAction SilentlyContinue
    if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { @{ error_code = 'FIXTURE_ROOT_LINK'; failed = 1 } | ConvertTo-Json -Compress; exit 1 }
}
if (Test-Path -LiteralPath $FixtureRoot) { @{ error_code = 'FIXTURE_ROOT_EXISTS'; failed = 1 } | ConvertTo-Json -Compress; exit 1 }
$verifier = [IO.Path]::GetFullPath([IO.Path]::Combine($PSScriptRoot, '..', 'Scripts', 'verify-design-snapshot.ps1'))
function New-Fixture([string]$Name) {
    $base = Join-Path $FixtureRoot $Name
    $project = Join-Path $base 'project'; $candidate = Join-Path $base 'candidate'
    foreach ($root in @($project, $candidate)) {
        [IO.Directory]::CreateDirectory((Join-Path $root 'Data')) | Out-Null
        [IO.Directory]::CreateDirectory((Join-Path $root 'Docs')) | Out-Null
        [IO.File]::WriteAllText((Join-Path $root 'Data/sample.json'), '{"description":"test fixture","value":1}' + "`n")
    }
    $digest = (Get-FileHash -LiteralPath (Join-Path $candidate 'Data/sample.json') -Algorithm SHA256).Hash.ToLowerInvariant()
    $report = @{ description = 'fixture byte report'; new_cases_passed = 1; previous_cases_passed = 0; total_cases_passed = 1; validated_sha256 = @{ 'Data/sample.json' = $digest }; results = @(@{ id = 'fixture-1'; passed = $true }) }
    foreach ($root in @($project, $candidate)) { [IO.File]::WriteAllText((Join-Path $root 'Docs/check.json'), ($report | ConvertTo-Json -Depth 8) + "`n") }
    $manifest = Join-Path $base 'files.json'
    [IO.File]::WriteAllText($manifest, '["Data/sample.json","Docs/check.json"]' + "`n")
    return @{ base = $base; project = $project; candidate = $candidate; manifest = $manifest; report = $report; report_relative = 'Docs/check.json'; check_published = $false }
}
function Snapshot-Inputs([string]$Root) {
    $rows = @(Get-ChildItem -LiteralPath $Root -File -Recurse | Sort-Object FullName | ForEach-Object { $_.FullName + '=' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
    return ($rows -join "`n")
}
$checks = @()
foreach ($name in $TestCases) {
    $f = New-Fixture $name
    $expectedError = $null
    switch ($name) {
        'snapshot-current-bytes' { }
        'hash-changed-after-report' { [IO.File]::WriteAllText((Join-Path $f.candidate 'Data/sample.json'), '{"value":2}'); $expectedError = 'HASH_MISMATCH' }
        'manifest-path-escape' { [IO.File]::WriteAllText($f.manifest, '["../outside.json","Docs/check.json"]'); $expectedError = 'UNSAFE_RELATIVE_PATH' }
        'report-path-escape' { [IO.File]::WriteAllText((Join-Path $f.base 'outside-report.json'), ($f.report | ConvertTo-Json -Depth 8)); $f.report_relative = '../outside-report.json'; $expectedError = 'UNSAFE_RELATIVE_PATH' }
        'hash-reference-escape' {
            [IO.File]::WriteAllText((Join-Path $f.base 'outside.json'), 'outside fixture')
            $f.report.validated_sha256 = @{ '../outside.json' = (Get-FileHash -LiteralPath (Join-Path $f.base 'outside.json') -Algorithm SHA256).Hash }
            [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8))
            $expectedError = 'UNSAFE_RELATIVE_PATH'
        }
        'duplicate-manifest' { [IO.File]::WriteAllText($f.manifest, '["Data/sample.json","Data/sample.json","Docs/check.json"]'); $expectedError = 'DUPLICATE_MANIFEST' }
        'case-alias-manifest' { [IO.File]::WriteAllText($f.manifest, '["Data/sample.json","data/SAMPLE.json","Docs/check.json"]'); $expectedError = 'DUPLICATE_MANIFEST' }
        'unlisted-candidate-file' { [IO.File]::WriteAllText((Join-Path $f.candidate 'Data/extra.json'), '{"extra":true}'); $expectedError = 'UNLISTED_CANDIDATE_FILE' }
        'missing-candidate-file' { [IO.File]::WriteAllText($f.manifest, '["Data/missing.json","Docs/check.json"]'); $expectedError = 'MISSING_CANDIDATE_FILE' }
        'rooted-manifest-path' { [IO.File]::WriteAllText($f.manifest, (@((Join-Path $f.base 'outside.json'), 'Docs/check.json') | ConvertTo-Json)); $expectedError = 'UNSAFE_RELATIVE_PATH' }
        'junction-reference-rejected' {
            $outside = Join-Path $f.base 'linked-source'
            [IO.Directory]::CreateDirectory($outside) | Out-Null
            [IO.File]::WriteAllText((Join-Path $outside 'linked.json'), '{"linked":true}')
            New-Item -ItemType Junction -Path (Join-Path $f.candidate 'Linked') -Target $outside | Out-Null
            $f.report.validated_sha256 = @{ 'Linked/linked.json' = (Get-FileHash -LiteralPath (Join-Path $outside 'linked.json') -Algorithm SHA256).Hash }
            [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8))
            [IO.File]::WriteAllText($f.manifest, '["Data/sample.json","Docs/check.json","Linked/linked.json"]')
            $expectedError = 'REPARSE_POINT_REJECTED'
        }
        'published-current' { $f.check_published = $true }
        'published-bytes-differ' { $f.check_published = $true; [IO.File]::WriteAllText((Join-Path $f.project 'Data/sample.json'), '{"value":99}'); $expectedError = 'PUBLISHED_BYTES_DIFFER' }
        'published-file-missing' { $f.check_published = $true; $f.project = Join-Path $f.base 'empty-project'; [IO.Directory]::CreateDirectory($f.project) | Out-Null; $expectedError = 'MISSING_PUBLISHED_FILE' }
        'published-same-root' { $f.check_published = $true; $f.project = $f.candidate; $expectedError = 'DISTINCT_ROOTS_REQUIRED' }
        'candidate-volume-root' { $f.candidate = [IO.Path]::GetPathRoot($f.candidate); $expectedError = 'INVALID_ROOT' }
        'report-duplicate-json-key' { $text = Get-Content -LiteralPath (Join-Path $f.candidate 'Docs/check.json') -Raw; $text = $text.Replace('"description":', '"total_cases_passed":999,"description":'); [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), $text); $expectedError = 'DUPLICATE_JSON_KEY' }
        'report-not-object' { [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), '[]'); $expectedError = 'INVALID_REPORT' }
        'report-empty-hashes' { $f.report.validated_sha256 = @{}; [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8)); $expectedError = 'INVALID_REPORT' }
        'report-invalid-hash' { $f.report.validated_sha256['Data/sample.json'] = 'not-a-digest'; [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8)); $expectedError = 'INVALID_REPORT' }
        'report-count-inconsistent' { $f.report.total_cases_passed = 2; [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8)); $expectedError = 'INVALID_REPORT' }
        'report-failed-result' { $f.report.results[0].passed = $false; [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8)); $expectedError = 'INVALID_REPORT' }
        'report-path-alias' {
            $hashes = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
            $hashes.Add('Data/sample.json', $f.report.validated_sha256['Data/sample.json']); $hashes.Add('data/SAMPLE.json', $f.report.validated_sha256['Data/sample.json'])
            $f.report.validated_sha256 = $hashes
            [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8)); $expectedError = 'DUPLICATE_REFERENCE'
        }
        'report-malformed-json' { [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), '{oops'); $expectedError = 'INVALID_JSON' }
        'report-comment-json' { $text = Get-Content -LiteralPath (Join-Path $f.candidate 'Docs/check.json') -Raw; [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), '/* comment */' + $text); $expectedError = 'INVALID_JSON' }
        'manifest-not-array' { [IO.File]::WriteAllText($f.manifest, '"Data/sample.json"'); $expectedError = 'INVALID_MANIFEST' }
        'empty-manifest' { [IO.File]::WriteAllText($f.manifest, '[]'); $expectedError = 'INVALID_MANIFEST' }
        'report-boolean-count' { $f.report.new_cases_passed = $true; [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8)); $expectedError = 'INVALID_REPORT' }
        'report-results-length' { $f.report.results = @(); [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8)); $expectedError = 'INVALID_REPORT' }
        'report-duplicate-result-id' { $f.report.results = @(@{ id = 'fixture-1'; passed = $true }, @{ id = 'fixture-1'; passed = $true }); $f.report.new_cases_passed = 2; $f.report.total_cases_passed = 2; [IO.File]::WriteAllText((Join-Path $f.candidate 'Docs/check.json'), ($f.report | ConvertTo-Json -Depth 8)); $expectedError = 'INVALID_REPORT' }
        'test-runner-existing-root' {
            $f.run_test_runner = $true; $f.runner_root = Join-Path $f.base 'existing-runner-root'; $f.runner_case = 'snapshot-current-bytes'
            $protected = Join-Path $f.runner_root 'snapshot-current-bytes/candidate/Data'
            [IO.Directory]::CreateDirectory($protected) | Out-Null; [IO.File]::WriteAllText((Join-Path $protected 'sample.json'), 'protected fixture')
            $expectedError = 'FIXTURE_ROOT_EXISTS'
        }
        'test-runner-unsafe-name' { $f.run_test_runner = $true; $f.runner_root = Join-Path $f.base 'new-runner-root'; $f.runner_case = '../escaped'; $expectedError = 'UNSUPPORTED_TEST_CASE' }
        'test-runner-link-parent' {
            $f.run_test_runner = $true; $source = Join-Path $f.base 'runner-source'; [IO.Directory]::CreateDirectory($source) | Out-Null
            $link = Join-Path $f.base 'runner-link'; New-Item -ItemType Junction -Path $link -Target $source | Out-Null
            $f.runner_root = Join-Path $link 'new-fixture'; $f.runner_case = 'snapshot-current-bytes'; $expectedError = 'FIXTURE_ROOT_LINK'
        }
        'verifier-caller-success-status' { $f.run_verifier_inprocess = $true }
        'extended-candidate-root' { $f.candidate = '\\?\' + $f.candidate; $expectedError = 'LOCAL_PATH_REQUIRED' }
        'extended-manifest-input' { $f.manifest = '\\?\' + $f.manifest; $expectedError = 'LOCAL_PATH_REQUIRED' }
        'test-runner-extended-root' { $f.run_test_runner = $true; $f.runner_root = '\\?\' + (Join-Path $f.base 'new-device-fixtures'); $f.runner_case = 'snapshot-current-bytes'; $expectedError = 'LOCAL_PATH_REQUIRED' }
        'test-runner-caller-success-status' { $f.run_runner_inprocess = $true; $f.runner_root = Join-Path $f.base 'fresh-caller-fixtures' }
        default { throw "Unknown test case: $name" }
    }
    $before = Snapshot-Inputs $f.base
    if (Test-Path -LiteralPath $verifier -PathType Leaf) {
        $arguments = if ($f.run_test_runner) { @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $PSCommandPath, '-FixtureRoot', $f.runner_root, '-TestCases', $f.runner_case) } else { @('-NoLogo', '-NoProfile', '-NonInteractive', '-File', $verifier, '-ProjectRoot', $f.project, '-CandidateRoot', $f.candidate, '-ManifestPath', $f.manifest, '-ReportRelativePath', $f.report_relative) }
        if ($f.check_published) { $arguments += '-CheckPublished' }
        if ($f.run_verifier_inprocess) {
            & (Join-Path $PSHOME 'pwsh.exe') -NoProfile -NonInteractive -Command 'exit 1'
            $output = & $verifier -ProjectRoot $f.project -CandidateRoot $f.candidate -ManifestPath $f.manifest -ReportRelativePath $f.report_relative
            $code = $LASTEXITCODE
        } elseif ($f.run_runner_inprocess) {
            $existingBefore = (Snapshot-Inputs $f.candidate) + (Snapshot-Inputs $f.project) + (Get-FileHash -LiteralPath $f.manifest -Algorithm SHA256).Hash
            $output = & $PSCommandPath -FixtureRoot $f.runner_root -TestCases 'snapshot-current-bytes','hash-changed-after-report'
            $code = $LASTEXITCODE
        } else { $output = & (Join-Path $PSHOME 'pwsh.exe') @arguments 2>$null; $code = $LASTEXITCODE }
        try { $result = ($output -join "`n") | ConvertFrom-Json -AsHashtable; if ($null -eq $result) { throw 'No structured output' } } catch { $result = @{ verified = $false; error_code = 'UNSTRUCTURED_FAILURE' } }
    } else { $code = 1; $result = @{ verified = $false; error_code = 'MISSING_IMPLEMENTATION' } }
    $passed = if ($f.run_runner_inprocess) { $code -eq 0 -and $result['passed'] -eq 2 -and $result['failed'] -eq 0 } elseif ($null -eq $expectedError) { $code -eq 0 -and $result['verified'] -eq $true -and $result['files_verified'] -eq 2 -and $result['report_hashes_verified'] -eq 1 -and (-not $f.check_published -or $result['published_checked'] -eq $true) } else { $code -ne 0 -and $result['error_code'] -eq $expectedError }
    $unchanged = if ($f.run_runner_inprocess) { ((Snapshot-Inputs $f.candidate) + (Snapshot-Inputs $f.project) + (Get-FileHash -LiteralPath $f.manifest -Algorithm SHA256).Hash) -ceq $existingBefore } else { (Snapshot-Inputs $f.base) -ceq $before }
    $checks += @{ id = $name; passed = ($passed -and $unchanged); inputs_unchanged = $unchanged; expected_error = $expectedError; observed_error = $result['error_code'] }
}
$failed = @($checks | Where-Object { -not $_.passed }).Count
@{ description = 'Real filesystem snapshot verification tests; not game runtime'; fixture_root = $FixtureRoot; checks = $checks; passed = $checks.Count - $failed; failed = $failed } | ConvertTo-Json -Depth 5 -Compress
if ($failed -gt 0) { exit 1 }
exit 0
