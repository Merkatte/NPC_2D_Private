@echo off
setlocal
dotnet run --project "%~dp0Tools\NpcHarness\NpcHarness.csproj" -- %*
exit /b %errorlevel%
