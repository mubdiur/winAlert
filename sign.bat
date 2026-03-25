@echo off
setlocal

echo ================================================
echo winAlert MSI Self-Signer
echo ================================================

set CERT_PASSWORD=winAlert2026!

echo.
echo Step 1: Creating self-signed certificate...

for /f "tokens=*" %%i in ('powershell -Command "New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=winAlert Developer' -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5).ToString('yyyy-MM-dd')" 2^>nul') do set CERT_OUTPUT=%%i

echo Certificate created (check Windows certificate store)

echo.
echo Step 2: Exporting certificate to PFX...

set CERT_FILE=winAlert-signing.pfx
if exist %CERT_FILE% del %CERT_FILE%

powershell -Command "Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert ^| Select-Object -First 1 ^| Export-PfxCertificate -Password (ConvertTo-SecureString -String '%CERT_PASSWORD%' -Force -AsPlainText) -FilePath '%CD%\%CERT_FILE%'"

if %errorlevel% neq 0 (
    echo.
    echo Error: Could not export certificate.
    echo Please create and export it manually:
    echo   1. Run: certlm.msc
    echo   2. Find your 'winAlert Developer' certificate
    echo   3. Right-click - All Tasks - Export...
    echo   4. Export as PFX with password '%CERT_PASSWORD%'
    echo   5. Save as: %CD%\%CERT_FILE%
    pause
    exit /b 1
)

echo.
echo Step 3: Signing MSI...

set MSI_FILE=dist\winAlert.msi
if not exist %MSI_FILE% (
    echo Error: MSI not found! Run release.bat first.
    pause
    exit /b 1
)

set SIGNTOOL="C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe"

if not exist %SIGNTOOL% (
    echo Error: SignTool not found at expected location.
    echo Please install Windows SDK or update SignTool path.
    pause
    exit /b 1
)

%SIGNTOOL% sign /f %CERT_FILE% /p %CERT_PASSWORD% /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 %MSI_FILE%

if %errorlevel% neq 0 (
    echo.
    echo Signing failed!
    pause
    exit /b 1
)

echo.
echo ================================================
echo Signed successfully!
echo ================================================
echo.
echo MSI: %MSI_FILE%
echo.

rem Cleanup temp cert
rem del %CERT_FILE%

echo Note: Self-signed certificates show 'Unknown Publisher' in SmartScreen.
echo For production, use a certificate from a CA like DigiCert or GlobalSign.
echo.
pause
