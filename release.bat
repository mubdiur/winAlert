@echo off
setlocal

echo ================================================
echo winAlert Release Builder
echo ================================================

set VERSION=1.0.0
set PUBLISH_DIR=publish
set DIST_DIR=dist

echo.
echo Step 1: Publishing self-contained app...
dotnet publish src\winAlert\winAlert.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:PublishReadyToRun=true ^
    -o .\%PUBLISH_DIR%

if %errorlevel% neq 0 (
    echo Publish failed!
    pause
    exit /b 1
)

echo.
echo Step 2: Building MSI installer...
if not exist "%DIST_DIR%" mkdir %DIST_DIR%

wix build -o %DIST_DIR%\winAlert.msi installer\Package.wxs

if %errorlevel% neq 0 (
    echo MSI build failed!
    pause
    exit /b 1
)

echo.
echo ================================================
echo Release complete!
echo ================================================
echo.
echo Output files:
echo   %DIST_DIR%\winAlert.msi
echo   %PUBLISH_DIR%\winAlert.exe
echo.
echo To sign the MSI, run:
echo   sign.bat
echo.
pause
