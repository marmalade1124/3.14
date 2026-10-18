$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$references = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Web.Extensions.dll','System.Xaml.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$windowsBase = Get-ChildItem (Join-Path $env:WINDIR 'Microsoft.NET\assembly\GAC_MSIL\WindowsBase') -Recurse -Filter WindowsBase.dll | Select-Object -First 1
$references += '/reference:' + $windowsBase.FullName
New-Item -ItemType Directory -Force dist | Out-Null
& (Join-Path $framework 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ /win32manifest:app.manifest /out:dist\3.14.exe /resource:Main.xaml,Main.xaml $references src\*.cs
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Copy-Item README.md dist\README.md
Write-Host 'Built dist\3.14.exe'
