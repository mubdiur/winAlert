@echo off
echo Installing dependencies...

echo.
echo Installing WiX v5 CLI...
dotnet tool install --global wix --version 5.0.0

echo.
echo Restoring NuGet packages...
dotnet restore src\winAlert\winAlert.csproj

echo.
echo Done! Dependencies installed.
pause
