@echo off
echo Running winAlert...

if not exist "publish\winAlert.exe" (
    echo Error: Published app not found!
    echo Run 'release.bat' first to build the app.
    pause
    exit /b 1
)

start "" "publish\winAlert.exe"
