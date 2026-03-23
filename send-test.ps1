$tcp = New-Object System.Net.Sockets.TcpClient
$tcp.Connect("localhost", 8888)
$stream = $tcp.GetStream()
$json = '{"id":"test-alert-001","severity":"critical","source":"test","title":"Test Alert","message":"This is a test alert","timestamp":"2026-03-23T11:15:00Z","requireAcknowledgment":true}'
$bytes = [System.Text.Encoding]::UTF8.GetBytes($json + [char]10)
$stream.Write($bytes, 0, $bytes.Length)
$stream.Close()
$tcp.Close()
Write-Host "Sent test alert"
