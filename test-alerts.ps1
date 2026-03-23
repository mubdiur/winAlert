# Test script for winAlert
# Usage: .\test-alerts.ps1

param(
    [string]$TargetHost = "localhost",
    [int]$Port = 8888
)

function Send-Alert {
    param(
        [string]$Severity,
        [string]$Source,
        [string]$Title,
        [string]$Message
    )
    
    $json = @"
{
  "id": "$(New-Guid)",
  "severity": "$Severity",
  "source": "$Source",
  "title": "$Title",
  "message": "$Message",
  "timestamp": "$(Get-Date -Format o)",
  "requireAcknowledgment": true
}
"@
    
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $tcpClient.Connect($TargetHost, $Port)
        $stream = $tcpClient.GetStream()
        $writer = New-Object System.IO.StreamWriter($stream)
        $writer.WriteLine($json)
        $writer.Flush()
        $stream.Flush()
        
        # Small delay between alerts
        Start-Sleep -Milliseconds 500
        
        $tcpClient.Close()
        Write-Host "[OK] Sent $Severity alert: $Title" -ForegroundColor Green
    }
    catch {
        Write-Host "[FAIL] Could not connect to $TargetHost`:$Port - is winAlert running?" -ForegroundColor Red
        exit 1
    }
}

Write-Host "=== winAlert Test Script ===" -ForegroundColor Cyan
Write-Host "Sending alerts to $TargetHost`:$Port..." -ForegroundColor Yellow
Write-Host ""

# Critical - will show full-screen overlay + alarm
Send-Alert -Severity "critical" -Source "test-script" -Title "CRITICAL: Server Down" -Message "Primary database server is unreachable! Immediate action required. All transactions are failing."

Start-Sleep 1

# High - requires acknowledgment
Send-Alert -Severity "high" -Source "test-script" -Title "High: Disk Space Critical" -Message "Disk usage on D: drive has reached 98%. Will cause service disruption soon."

Start-Sleep 1

# Medium - auto-dismisses after 30s
Send-Alert -Severity "medium" -Source "test-script" -Title "Medium: Backup Failed" -Message "Nightly backup job did not complete successfully. Check backup logs for details."

Start-Sleep 1

# Low - subtle notification
Send-Alert -Severity "low" -Source "test-script" -Title "Low: High Memory Usage" -Message "Server memory usage is at 85%. Consider restarting non-essential services."

Start-Sleep 1

# Info - minimal notification
Send-Alert -Severity "info" -Source "test-script" -Title "Info: Service Updated" -Message "Application services have been updated to version 2.1.0."

Write-Host ""
Write-Host "All test alerts sent!" -ForegroundColor Cyan
Write-Host "Check the winAlert window to see them appear."
