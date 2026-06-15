@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "BATCH_NAME=%~nx0"
set "BATCH_PATH=%~f0"
set "LOG_DIR=%SCRIPT_DIR%logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss-fff"') do set "RUN_TS=%%I"
set "LOG_FILE=%LOG_DIR%\%~n0-%RUN_TS%.log"
set "EXIT_CODE=0"

call :log "[%BATCH_NAME%] log=%LOG_FILE%"
call :log "[%BATCH_NAME%] args=%*"

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$node=(Get-Command node -ErrorAction Stop).Source;" ^
  "$script='%SCRIPT_DIR%subset_tool\subset_fonts.js';" ^
  "& $node $script %* | ForEach-Object { Write-Host $_; Add-Content -Value $_ -Path '%LOG_FILE%' -Encoding UTF8 };" ^
  "exit $LASTEXITCODE"
set "EXIT_CODE=%ERRORLEVEL%"

call :log "[%BATCH_NAME%] exit_code=%EXIT_CODE%"
if not "%GEN_BATCH_NO_PAUSE%"=="1" (
    echo.
    echo Press any key to close...
    pause >nul
)

endlocal & exit /b %EXIT_CODE%

:log
echo %~1
>>"%LOG_FILE%" echo %~1
exit /b 0
