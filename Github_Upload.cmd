@echo off
chcp 65001 >nul
title ORD Tarif Manager - GitHub Upload
cls
cd /d "%~dp0"
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "& { Unblock-File -Path '%~dp0build\upload_to_github.ps1' -ErrorAction SilentlyContinue; & '%~dp0build\upload_to_github.ps1' }"
echo.
pause
