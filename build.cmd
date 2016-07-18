@echo off
powershell -NoProfile -ExecutionPolicy Bypass -Command "& '%~dp0\build.ps1' %*; if ($psake.build_success -eq $false) { exit 1 } else { exit 0 }"