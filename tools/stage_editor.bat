@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "BATCH_NAME=%~nx0"
set "EDITOR_DIR=%SCRIPT_DIR%stage_editor"
set "PROJECT_ROOT=%SCRIPT_DIR%.."
set "GEN_DIR=%SCRIPT_DIR%stage_generator"
set "GEN_DLL=%GEN_DIR%\bin\publish\StageGenerator.Cli.dll"
set "GEN_PROJ=%GEN_DIR%\StageGenerator.Cli.csproj"
set "LOG_DIR=%SCRIPT_DIR%logs"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

for /f %%I in ('powershell -NoProfile -Command "Get-Date -Format yyyyMMdd-HHmmss"') do set "RUN_TS=%%I"
set "LOG_FILE=%LOG_DIR%\%~n0-%RUN_TS%.log"
set "EXIT_CODE=0"

call :log "[%BATCH_NAME%] log=%LOG_FILE%"

:: Publish stage_generator if DLL is missing or source is newer
powershell -NoProfile -Command ^
  "if (!(Test-Path '%GEN_DLL%') -or ((Get-Item '%GEN_DIR%\Program.cs').LastWriteTime -gt (Get-Item '%GEN_DLL%').LastWriteTime) -or ((Get-Item '%GEN_PROJ%').LastWriteTime -gt (Get-Item '%GEN_DLL%').LastWriteTime)) { exit 1 } else { exit 0 }"
if %ERRORLEVEL% neq 0 (
    call :log "[stage_generator] Publishing Release build..."
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
      "& cmd.exe /d /c 'dotnet publish ""%GEN_PROJ%"" -c Release -o ""%GEN_DIR%\bin\publish"" --nologo -v quiet 2>&1' | ForEach-Object { Write-Host $_; Add-Content -Value $_ -Path '%LOG_FILE%' -Encoding UTF8 }; exit $LASTEXITCODE"
    if %ERRORLEVEL% neq 0 (
        set "EXIT_CODE=%ERRORLEVEL%"
        call :log "[stage_generator] ERROR: publish failed"
        goto :finish
    )
    call :log "[stage_generator] Publish complete."
)

if not exist "%EDITOR_DIR%\node_modules" (
    call :log "[stage_editor] node_modules not found, running npm install..."
    powershell -NoProfile -ExecutionPolicy Bypass -Command ^
      "Push-Location '%EDITOR_DIR%'; try { & cmd.exe /d /c 'npm.cmd install 2>&1' | ForEach-Object { Write-Host $_; Add-Content -Value $_ -Path '%LOG_FILE%' -Encoding UTF8 }; exit $LASTEXITCODE } finally { Pop-Location }"
    if %ERRORLEVEL% neq 0 (
        set "EXIT_CODE=%ERRORLEVEL%"
        call :log "[stage_editor] ERROR: npm install failed"
        goto :finish
    )
)

call :log "[stage_editor] Starting dev server at http://[::1]:3000"
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$env:PROJECT_ROOT='%PROJECT_ROOT%'; Push-Location '%EDITOR_DIR%'; try { & cmd.exe /d /c 'npm.cmd run dev 2>&1' | ForEach-Object { Write-Host $_; Add-Content -Value $_ -Path '%LOG_FILE%' -Encoding UTF8 }; exit $LASTEXITCODE } finally { Pop-Location }"
set "EXIT_CODE=%ERRORLEVEL%"

:finish
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
