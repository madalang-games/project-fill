@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "BATCH_NAME=%~nx0"
set "LOG_DIR=%SCRIPT_DIR%logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "RUN_TS=%%I"
set "LOG_FILE=%LOG_DIR%\%~n0-%RUN_TS%.log"
set "EXIT_CODE=0"

call :log "[%BATCH_NAME%] log=%LOG_FILE%"
call :run_step "sync-contracts" "%SCRIPT_DIR%pkt_generator\pkt_generator.js"
set "EXIT_CODE=%ERRORLEVEL%"

call :log "[%BATCH_NAME%] exit_code=%EXIT_CODE%"
if not "%GEN_BATCH_NO_PAUSE%"=="1" (
    echo.
    echo Press any key to close...
    pause >nul
)

endlocal & exit /b %EXIT_CODE%

:run_step
call :log "[%BATCH_NAME%] step=%~1"
node "%~2"
exit /b %ERRORLEVEL%

:log
echo %~1
>>"%LOG_FILE%" echo %~1
exit /b 0
