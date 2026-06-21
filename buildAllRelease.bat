@echo off
cd /d "%~dp0"

call git submodule update --init --recursive
call dotnet build -c Release

pause
