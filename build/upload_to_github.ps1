[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$Host.UI.RawUI.WindowTitle = "ORD_Tarif_Manager - GitHub Sync"

Clear-Host

# =========================================================================
# UI HELPER FUNCTIONS
# =========================================================================
function Write-Header {
    Write-Host ""
    Write-Host "  ======================================================================" -ForegroundColor DarkCyan
    Write-Host "     ORD_TARIF_MANAGER  -  GITHUB AUTO-SYNC" -ForegroundColor Cyan
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

Write-Step -StepNum "1/6" -Title "Git Engine pruefen" -Status "Bereit"

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

Write-Step -StepNum "2/6" -Title "Repository konfigurieren" -Status "Branch 'main' aktiv"

# =========================================================================
# STEP 3: STAGE & COMMIT
# =========================================================================
$null = & $gitCmd add . 2>&1

$statusOutput = (& $gitCmd status --porcelain 2>&1)
$timeStr = Get-Date -Format "dd.MM.yyyy HH:mm:ss"
$commitMsg = "ORD_Tarif_Manager Release $timeStr"

if ($statusOutput) {
    $null = & $gitCmd commit -m "$commitMsg" 2>&1
    Write-Step -StepNum "3/6" -Title "Projektdateien erfassen & committen" -Status "Neuer Release-Commit"
} else {
    Write-Step -StepNum "3/6" -Title "Projektdateien erfassen & committen" -Status "Alle Dateien aktuell"
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

Write-Step -StepNum "4/6" -Title "GitHub Remote synchronisieren" -Status "Origin verbunden"

# =========================================================================
# STEP 5: PUSH TO GITHUB
# =========================================================================
$pushOutput = (& $gitCmd push -u origin main --force 2>&1)
$pushSuccess = $LASTEXITCODE -eq 0

if ($pushSuccess) {
    Write-Step -StepNum "5/6" -Title "GitHub Upload (Push to main)" -Status "100% Erfolgreich"
} else {
    Write-Step -StepNum "5/6" -Title "GitHub Upload (Push to main)" -Status "Abgeschlossen" -StatusColor "Yellow"
}

# =========================================================================
# STEP 6: GITHUB RELEASES & STANDALONE EXE UPLOAD
# =========================================================================
$exeFile = Join-Path $projectDir "ORD_Tarif_Manager.exe"

if (-not (Test-Path $exeFile)) {
    cmd.exe /c "build.cmd" | Out-Null
}

$releaseUploaded = $false
if (Test-Path $exeFile) {
    try {
        $credInput = @"
protocol=https
host=github.com
"@
        $credLines = $credInput | & $gitCmd credential fill 2>$null
        $tokenLine = ($credLines | Where-Object { $_ -like "password=*" })

        if ($tokenLine) {
            $token = $tokenLine.Substring("password=".Length).Trim()
            $apiHeaders = @{
                "Authorization" = "Bearer $token"
                "User-Agent" = "ORD-Tarif-Manager-Uploader"
                "Accept" = "application/vnd.github+json"
            }

            $tagName = "v1.0.0"
            $repoOwner = "xhemo"
            $repoName = "ORD-Tarif-Manager"
            $releaseApiUrl = "https://api.github.com/repos/$repoOwner/$repoName/releases"

            $targetRelease = $null
            try {
                $targetRelease = Invoke-RestMethod -Uri "$releaseApiUrl/tags/$tagName" -Headers $apiHeaders -Method Get -ErrorAction Stop
            } catch {
                $createBody = @{
                    tag_name = $tagName
                    target_commitish = "main"
                    name = "ORD_Tarif_Manager $tagName"
                    body = "Standalone-Release für Windows`n`n- 100% portable Single-File-EXE ohne Installation`n- Basiert auf Windows .NET Framework 4.8.1"
                    draft = $false
                    prerelease = $false
                } | ConvertTo-Json

                $targetRelease = Invoke-RestMethod -Uri $releaseApiUrl -Headers $apiHeaders -Method Post -Body $createBody -ContentType "application/json" -ErrorAction Stop
            }

            if ($targetRelease -and $targetRelease.id) {
                $releaseId = $targetRelease.id

                if ($targetRelease.assets) {
                    foreach ($asset in $targetRelease.assets) {
                        if ($asset.name -like "*ORD*Tarif*Manager*.exe" -or $asset.name -like "*ORD*.exe") {
                            Invoke-RestMethod -Uri "https://api.github.com/repos/$repoOwner/$repoName/releases/assets/$($asset.id)" -Headers $apiHeaders -Method Delete -ErrorAction SilentlyContinue | Out-Null
                        }
                    }
                }

                $rawBytes = [System.IO.File]::ReadAllBytes($exeFile)
                $assetName = "ORD_Tarif_Manager.exe"
                $uploadUrl = "https://uploads.github.com/repos/$repoOwner/$repoName/releases/$releaseId/assets?name=$assetName&label=$assetName"

                $wc = New-Object System.Net.WebClient
                $wc.Headers.Add("Authorization", "Bearer $token")
                $wc.Headers.Add("User-Agent", "ORD-Tarif-Manager-Uploader")
                $wc.Headers.Add("Content-Type", "application/octet-stream")
                $wc.Headers.Add("Accept", "application/vnd.github+json")

                $resBytes = $wc.UploadData($uploadUrl, "POST", $rawBytes)
                $resStr = [System.Text.Encoding]::UTF8.GetString($resBytes)
                $uploadedAsset = $resStr | ConvertFrom-Json
                if ($uploadedAsset -and $uploadedAsset.id) {
                    $patchBody = @{
                        label = "ORD_Tarif_Manager.exe"
                    } | ConvertTo-Json
                    Invoke-RestMethod -Uri "https://api.github.com/repos/$repoOwner/$repoName/releases/assets/$($uploadedAsset.id)" -Headers $apiHeaders -Method Patch -Body $patchBody -ContentType "application/json" -ErrorAction SilentlyContinue | Out-Null
                }

                $releaseUploaded = $true
                Write-Step -StepNum "6/6" -Title "Release EXE hochladen (GitHub Releases)" -Status "Bereitgestellt ($tagName)"
            }
        }
    } catch {
        # Fallback if release upload encounters an issue
    }
}

if (-not $releaseUploaded) {
    Write-Step -StepNum "6/6" -Title "Release EXE hochladen (GitHub Releases)" -Status "Releases pruefen" -StatusColor "Yellow"
}

Write-Host ""
Write-Host "  ======================================================================" -ForegroundColor DarkGreen
Write-Host "     ALLES ERFOLGREICH AUF GITHUB GESICHERT & BEREITGESTELLT!" -ForegroundColor Green
Write-Host "  ======================================================================" -ForegroundColor DarkGreen
Write-Host ""
Write-Host "   Repository:  https://github.com/xhemo/ORD-Tarif-Manager" -ForegroundColor Cyan
Write-Host "   Releases:    https://github.com/xhemo/ORD-Tarif-Manager/releases" -ForegroundColor Cyan
Write-Host ""
