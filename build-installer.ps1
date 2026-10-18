param([string]$Compiler, [string]$Version = '0.1.0')
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Use a numeric major.minor.patch version.' }
if (Get-Process '3.14' -ErrorAction SilentlyContinue) { throw 'Close 3.14 before building its installer.' }
if (!$Compiler) {
    $candidates = @('.tools\Inno\ISCC.exe', "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")
    $Compiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (!$Compiler -or !(Test-Path -LiteralPath $Compiler)) { throw 'Install Inno Setup 6 or pass -Compiler with the path to ISCC.exe.' }
& ./build.ps1
& $Compiler '/Qp' "/DAppVersion=$Version" 'installer\3.14.iss'
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$hash = (Get-FileHash 'dist\3.14-Setup.exe' -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $PWD 'dist\SHA256SUMS.txt'), "$hash  3.14-Setup.exe`n", [Text.Encoding]::ASCII)
Write-Host 'Built dist\3.14-Setup.exe and dist\SHA256SUMS.txt'
