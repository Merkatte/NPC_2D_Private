@echo off
setlocal
pushd "%~dp0" >nul
dotnet run --project "%~dp0Tools\NpcHarness\NpcHarness.csproj" -- %*
set "harness_exit=%errorlevel%"
popd >nul
exit /b %harness_exit%
