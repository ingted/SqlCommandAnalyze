# the version in progress, used by pre-release builds
$version = (Get-Content .build\.version -Raw).Trim()

$exit = 0
trap [Exception] {
    $exit++
    Write-Warning $_.Exception.Message
    continue
}

$pst = [TimeZoneInfo]::FindSystemTimeZoneById('Pacific Standard Time')
$date = New-Object DateTimeOffset([TimeZoneInfo]::ConvertTimeFromUtc([DateTime]::UtcNow, $pst), $pst.BaseUtcOffset)
$version = $version + '-a' + $date.ToString('yyMMddHHmm')

$path = Get-Location

.\packages\FAKE\tools\FAKE.exe .build\build.fsx -ev version $version
if ($lastexitcode -ne 0){ exit $lastexitcode }

Set-Location .\SqlCommandAnalyze
.\..\.paket\paket.exe pack output ..\bin version $version 
echo "created $path\bin\Tachyus.SqlCommandAnalyze.$version.nupkg"

Set-Location ..

exit $exit