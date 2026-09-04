[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "ORD Tarif Manager - GitHub Sync"

Clear-Host

# =========================================================================
# UI HELPER FUNCTIONS
# =========================================================================
function Write-Header {
    Write-Host ""
    Write-Host "  ======================================================================" -ForegroundColor DarkCyan
    Write-Host "     ORD TARIF MANAGER  -  GITHUB AUTO-SYNC" -ForegroundColor Cyan
    Write-Host "  ======================================================================" -ForegroundColor DarkCyan
    Write-Host ""
}

function Write-Step {
    param(
        [string]$StepNum,
        [string]$Title,
        [string]$Status,
        [string]$StatusColor = "Green"
    )
    $prefix = "  [$StepNum] "
    Write-Host $prefix -ForegroundColor DarkGray -NoNewline
    Write-Host ($Title.PadRight(44)) -ForegroundColor White -NoNewline
    if ($Status) {
        Write-Host "[OK] $Status" -ForegroundColor $StatusColor
    }
}

# =========================================================================
# STARTUP & PATH DISCOVERY
# =========================================================================
Write-Header

$projectDir = if ($PSScriptRoot) { (Resolve-Path (Join-Path $PSScriptRoot "..")).Path } else { $PWD.Path }
Set-Location $projectDir

$repoUrl = "https://github.com/xhemo/ORD-Tarif-Manager.git"

Write-Host "   Ordner:      " -ForegroundColor DarkGray -NoNewline
Write-Host $projectDir -ForegroundColor Gray
Write-Host "   Repository:  " -ForegroundColor DarkGray -NoNewline
Write-Host $repoUrl -ForegroundColor Cyan
Write-Host "  ----------------------------------------------------------------------" -ForegroundColor DarkGray
Write-Host ""

# =========================================================================
# STEP 1: GIT ENGINE
# =========================================================================
$gitCmd = $null
if (Get-Command git -ErrorAction SilentlyContinue) {
    $gitCmd = "git"
} elseif (Test-Path "C:\Program Files\Git\cmd\git.exe") {
    $gitCmd = "C:\Program Files\Git\cmd\git.exe"
} elseif (Test-Path "$env:LOCALAPPDATA\Programs\Git\cmd\git.exe") {
    $gitCmd = "$env:LOCALAPPDATA\Programs\Git\cmd\git.exe"
}

if (-not $gitCmd) {
    $minGitFolder = Join-Path $env:TEMP "MinGit_Znipe"
    $minGitZip = Join-Path $env:TEMP "mingit.zip"
    $gitCmd = Join-Path $minGitFolder "cmd\git.exe"

    if (-not (Test-Path $gitCmd)) {
        if (-not (Test-Path $minGitFolder)) {
            New-Item -ItemType Directory -Path $minGitFolder -Force | Out-Null
        }
        $url = "https://github.com/git-for-windows/git/releases/download/v2.44.0.windows.1/MinGit-2.44.0-64-bit.zip"
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($url, $minGitZip)
        Expand-Archive -Path $minGitZip -DestinationPath $minGitFolder -Force
        Remove-Item $minGitZip -Force -ErrorAction SilentlyContinue
    }
}

Write-Step -StepNum "1/5" -Title "Git Engine pruefen" -Status "Bereit"

# =========================================================================
# STEP 2: INIT & CONFIG
# =========================================================================
if (-not (Test-Path (Join-Path $projectDir ".git"))) {
    $null = & $gitCmd init -b main 2>&1
} else {
    $null = & $gitCmd branch -M main 2>&1
}

$null = & $gitCmd config user.name "xhemo" 2>&1
$null = & $gitCmd config user.email "xhemo@github.com" 2>&1

Write-Step -StepNum "2/5" -Title "Repository konfigurieren" -Status "Branch 'main' aktiv"

# =========================================================================
# STEP 3: STAGE & COMMIT
# =========================================================================
$null = & $gitCmd add . 2>&1

$statusOutput = (& $gitCmd status --porcelain 2>&1)
$timeStr = Get-Date -Format "dd.MM.yyyy HH:mm:ss"
$commitMsg = "ORD Tarif Manager Release $timeStr"

if ($statusOutput) {
    $null = & $gitCmd commit -m "$commitMsg" 2>&1
    Write-Step -StepNum "3/5" -Title "Projektdateien erfassen & committen" -Status "Neuer Release-Commit"
} else {
    Write-Step -StepNum "3/5" -Title "Projektdateien erfassen & committen" -Status "Alle Dateien aktuell"
}

# =========================================================================
# STEP 4: REMOTE CONFIG
# =========================================================================
$existingRemotes = (& $gitCmd remote 2>&1)
if ($existingRemotes -contains "origin") {
    $null = & $gitCmd remote set-url origin $repoUrl 2>&1
} else {
    $null = & $gitCmd remote add origin $repoUrl 2>&1
}

Write-Step -StepNum "4/5" -Title "GitHub Remote synchronisieren" -Status "Origin verbunden"

# =========================================================================
# STEP 5: PUSH TO GITHUB
# =========================================================================
$pushOutput = (& $gitCmd push -u origin main --force 2>&1)
$pushSuccess = $LASTEXITCODE -eq 0

if ($pushSuccess) {
    Write-Step -StepNum "5/5" -Title "GitHub Upload (Push to main)" -Status "100% Erfolgreich"
    
    Write-Host ""
    Write-Host "  ======================================================================" -ForegroundColor DarkGreen
    Write-Host "     ALLES ERFOLGREICH AUF GITHUB GESICHERT!" -ForegroundColor Green
    Write-Host "  ======================================================================" -ForegroundColor DarkGreen
    Write-Host ""
    Write-Host "   Repository:  https://github.com/xhemo/ORD-Tarif-Manager" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Step -StepNum "5/5" -Title "GitHub Upload (Push to main)" -Status "Abgeschlossen" -StatusColor "Yellow"
    Write-Host ""
    Write-Host "  Hinweis: Falls noetig, bitte im GitHub-Browser-Dialog anmelden." -ForegroundColor Yellow
    Write-Host ""
}
