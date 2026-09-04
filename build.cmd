@echo off
title ORD Tarif Manager - Builder
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "& { Unblock-File -Path '%~dp0build\build.ps1' -ErrorAction SilentlyContinue; & '%~dp0build\build.ps1' }"
pause
