#Requires -Version 5.1
<#
    .SYNOPSIS
    100% Pure Native C# .NET WPF Direct-Root Build Pipeline
    ORD Tarif Manager
#>

$baseDir = Split-Path $PSScriptRoot -Parent
$srcDir  = Join-Path $baseDir 'src'
$rootExe = Join-Path $baseDir 'ORD Tarif Manager.exe'
$oldExe  = Join-Path $baseDir 'ORD Tarif Manager.old'

Set-Location $baseDir

$frameworkDir = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path $frameworkDir)) {
    $frameworkDir = 'C:\Windows\Microsoft.NET\Framework\v4.0.30319'
}
$wpfDir       = Join-Path $frameworkDir 'WPF'
$cscPath      = Join-Path $frameworkDir 'csc.exe'

$iconIcoPath  = Join-Path $srcDir 'Resources\app.ico'

Write-Host '==========================================================' -ForegroundColor Cyan
Write-Host '   ORD TARIF MANAGER - DIRECT ROOT BUILD                  ' -ForegroundColor Yellow
Write-Host '==========================================================' -ForegroundColor Cyan
Write-Host ''

# 1. Beende alle laufenden Instanzen
$running = Get-Process -Name 'ORD Tarif Manager' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host '-> Beende laufende Instanz(en) von ORD Tarif Manager...' -ForegroundColor Yellow
    $running | ForEach-Object { 
        try { $_.Kill() } catch {}
        try { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue } catch {}
    }
    $timeout = 25
    while ((Get-Process -Name 'ORD Tarif Manager' -ErrorAction SilentlyContinue) -and ($timeout -gt 0)) {
        Start-Sleep -Milliseconds 100
        $timeout--
    }
}

# 2. Live-Replace Vorbereitung
if (Test-Path $rootExe) {
    $retries = 10
    while ($retries -gt 0) {
        try {
            Remove-Item $rootExe -Force -ErrorAction Stop
            break
        } catch {
            Start-Sleep -Milliseconds 150
            $retries--
        }
    }
    if (Test-Path $rootExe) {
        Remove-Item $oldExe -Force -ErrorAction SilentlyContinue
        Move-Item $rootExe $oldExe -Force -ErrorAction SilentlyContinue
    }
}

# 3. Kompiliere direkt nach ORD Tarif Manager.exe
Write-Host '-> Kompiliere 100% natives C# Release mit App-Icon...' -ForegroundColor White

$refList = @(
    (Join-Path $wpfDir 'PresentationFramework.dll'),
    (Join-Path $wpfDir 'PresentationCore.dll'),
    (Join-Path $wpfDir 'WindowsBase.dll'),
    (Join-Path $frameworkDir 'System.Xaml.dll'),
    (Join-Path $frameworkDir 'System.Xml.dll'),
    (Join-Path $frameworkDir 'System.Xml.Linq.dll'),
    (Join-Path $frameworkDir 'System.Data.dll'),
    (Join-Path $frameworkDir 'System.dll'),
    (Join-Path $frameworkDir 'System.Core.dll')
)
$refArgs = ($refList | ForEach-Object { "/reference:`"$_`"" }) -join ' '

$iconArg = ""
if (Test-Path $iconIcoPath) {
    $iconArg = "/win32icon:`"$iconIcoPath`""
}

$resList = @(
    "/resource:src\UI\Styles\Theme.xaml,Theme.xaml",
    "/resource:src\UI\Views\MainWindow.xaml,MainWindow.xaml",
    "/resource:src\UI\Dialogs\BulkUpdateDialog.xaml,BulkUpdateDialog.xaml",
    "/resource:src\UI\Dialogs\FilterDialog.xaml,FilterDialog.xaml",
    "/resource:src\UI\Dialogs\CreateTariffDialog.xaml,CreateTariffDialog.xaml",
    "/resource:src\UI\Dialogs\MatrixImportDialog.xaml,MatrixImportDialog.xaml",
    "/resource:src\Resources\app.ico,app.ico",
    "/resource:src\Resources\logo.png,logo.png"
)
$resArgs = $resList -join ' '

$csFiles = Get-ChildItem -Path $srcDir -Filter "*.cs" -Recurse | ForEach-Object { "`"$($_.FullName)`"" }
$csFilesArg = $csFiles -join ' '

$compileCmd = "& `"$cscPath`" /target:winexe /optimize+ /nologo /codepage:65001 $iconArg $resArgs $refArgs /out:`"$rootExe`" $csFilesArg"
Invoke-Expression $compileCmd

Remove-Item $oldExe -Force -ErrorAction SilentlyContinue

if (Test-Path $rootExe) {
    $size = (Get-Item $rootExe).Length
    Write-Host ''
    Write-Host ('[OK] ERFOLGREICH KOMPILIERT: ' + $rootExe + ' (' + $size + ' Bytes)') -ForegroundColor Green
    
    # 4. Automatischer Neustart der App
    Write-Host '-> Starte ORD Tarif Manager.exe neu...' -ForegroundColor Cyan
    Start-Process -FilePath $rootExe
    Start-Sleep -Milliseconds 400
} else {
    Write-Host 'FEHLER beim Kompilieren!' -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host '==========================================================' -ForegroundColor Green
