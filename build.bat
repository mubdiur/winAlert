@echo off
echo Building winAlert (Release)...

dotnet build src\winAlert\winAlert.csproj -c Release -v q

if %errorlevel% equ 0 (
    echo Build succeeded!
) else (
    echo Build failed!
)
pause
