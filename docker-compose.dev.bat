@echo off
setlocal

cd /d "%~dp0"

set "STATUS=0"
if not exist ".env.dev" (
  echo [docker-compose.dev] ERROR: .env.dev not found.
  set "STATUS=1"
  goto :end
)

set /p "BUILD=Build images before starting? (y/n): "
set "BUILD_FLAG="
if /i "%BUILD%"=="y" set "BUILD_FLAG=--build"

docker compose --env-file .env.dev -f docker-compose.yml -f docker-compose.dev.yml up -d %BUILD_FLAG% %*
set "STATUS=%ERRORLEVEL%"

:end
if "%STATUS%"=="0" (
  echo [docker-compose.dev] Finished with exit code 0.
) else (
  echo [docker-compose.dev] Finished with exit code %STATUS%.
)
pause
exit /b %STATUS%
