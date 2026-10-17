param([int]$Port = 8443, [switch]$RegenerateCertificate)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
if (!(Test-Path "$projectRoot/WebGL/index.html")) { throw 'Build WebGL first. See README.md.' }
if ($RegenerateCertificate -or !(Test-Path "$projectRoot/.certs/server.pem")) {
    $pythonRuntime = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
    if (!(Test-Path $pythonRuntime)) { $pythonRuntime = 'python' }
    $lanAddresses = @(Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.IPAddress -notlike '169.254.*' } | ForEach-Object { $_.IPAddress })
    & $pythonRuntime "$projectRoot/tools/create-cert.py" @lanAddresses
    if ($LASTEXITCODE -ne 0) { throw 'Certificate generation failed. Python + cryptography required.' }
}
$env:PORT = "$Port"
Write-Host "Open https://localhost:$Port/"
Write-Host 'For Android: install .certs/rootCA.cer as a CA certificate, then open your PC LAN address.'
& node "$projectRoot/tools/https-server.cjs"
