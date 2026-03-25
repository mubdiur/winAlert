@echo off
setlocal

echo ================================================
echo winAlert MSI Self-Signer
echo ================================================

set CERT_PASSWORD=winAlert2026!

echo.
echo Step 1: Creating self-signed certificate...

set CERT_FILE=winAlert-signing.pfx
if exist %CERT_FILE% del %CERT_FILE%

powershell -Command "$cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=winAlert Developer' -CertStoreLocation Cert:\CurrentUser\My -NotAfter (Get-Date).AddYears(5); $cert | Export-PfxCertificate -Password (ConvertTo-SecureString -String '%CERT_PASSWORD%' -Force -AsPlainText) -FilePath '%CD%\%CERT_FILE%'; Write-Host 'Certificate thumbprint:' $cert.Thumbprint"

if %errorlevel% neq 0 (
    echo.
    echo Error: Could not create or export certificate.
    echo Please create and export it manually:
    echo   1. Run: certlm.msc
    echo   2. Find your 'winAlert Developer' certificate
    echo   3. Right-click - All Tasks - Export...
    echo   4. Export as PFX with password '%CERT_PASSWORD%'
    echo   5. Save as: %CD%\%CERT_FILE%
    pause
    exit /b 1
)

echo Certificate created and exported successfully.

echo.
echo Step 2: Signing MSI...

set MSI_FILE=dist\winAlert.msi
if not exist %MSI_FILE% (
    echo Error: MSI not found! Run release.bat first.
    pause
    exit /b 1
)

rem Auto-detect SignTool location (find latest Windows SDK version)
for /f "tokens=*" %%i in ('dir /b /ad "C:\Program Files (x86)\Windows Kits\10\bin\10.0.*" 2^>nul ^| sort /r') do (
    set SIGNTOOL="C:\Program Files (x86)\Windows Kits\10\bin\%%i\x64\signtool.exe"
    goto :found_signtool
)

:found_signtool
if not exist %SIGNTOOL% (
    echo Error: SignTool not found. Please install Windows SDK.
    echo Run: winget install Microsoft.WindowsSDK
    pause
    exit /b 1
)

echo Using SignTool: %SIGNTOOL%

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
