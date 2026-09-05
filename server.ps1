# Simple static file server for the AR demo (model-viewer).
# Run: powershell -NoProfile -ExecutionPolicy Bypass -File server.ps1
param(
    [int]$HttpPort = 8080
)

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$mime = @{
    '.html' = 'text/html; charset=utf-8'
    '.htm'  = 'text/html; charset=utf-8'
    '.js'   = 'text/javascript; charset=utf-8'
    '.mjs'  = 'text/javascript; charset=utf-8'
    '.css'  = 'text/css; charset=utf-8'
    '.json' = 'application/json'
    '.png'  = 'image/png'
    '.jpg'  = 'image/jpeg'
    '.jpeg' = 'image/jpeg'
    '.svg'  = 'image/svg+xml'
    '.ico'  = 'image/x-icon'
    '.glb'  = 'model/gltf-binary'
    '.gltf' = 'model/gltf+json'
    '.bin'  = 'application/octet-stream'
    '.usdz' = 'model/vnd.usdz+zip'
    '.hdr'  = 'image/vnd.radiance'
    '.webp' = 'image/webp'
    '.txt'  = 'text/plain; charset=utf-8'
}

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://localhost:$HttpPort/")
try {
    $listener.Start()
} catch {
    Write-Error "Cannot bind port $HttpPort : $_"
    exit 1
}

Write-Host "Serving folder: $root"
Write-Host "Open in browser: http://localhost:$HttpPort/"
Write-Host "Press Ctrl+C to stop"

$fullRoot = [System.IO.Path]::GetFullPath($root)

while ($listener.IsListening) {
    $ctx = $listener.GetContext()
    try {
        $path = [Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath)
        if ($path -eq '/') { $path = '/index.html' }
        $rel = $path.TrimStart('/') -replace '/', '\'
        $file = Join-Path $root $rel
        $fullFile = [System.IO.Path]::GetFullPath($file)

        # path traversal guard
        if (-not $fullFile.StartsWith($fullRoot)) {
            $ctx.Response.StatusCode = 403
            $ctx.Response.Close()
            continue
        }

        if (Test-Path -LiteralPath $fullFile -PathType Container) {
            $fullFile = Join-Path $fullFile 'index.html'
        }

        if (-not (Test-Path -LiteralPath $fullFile)) {
            $ctx.Response.StatusCode = 404
            $ctx.Response.ContentType = 'text/plain; charset=utf-8'
            $msg = [Text.Encoding]::UTF8.GetBytes("404 Not Found: $path")
            $ctx.Response.ContentLength64 = $msg.Length
            $ctx.Response.OutputStream.Write($msg, 0, $msg.Length)
            $ctx.Response.Close()
            Write-Host "$(Get-Date -Format HH:mm:ss) 404 $path"
            continue
        }

        $ext = [System.IO.Path]::GetExtension($fullFile).ToLowerInvariant()
        if ($mime.ContainsKey($ext)) { $ctx.Response.ContentType = $mime[$ext] }
        else { $ctx.Response.ContentType = 'application/octet-stream' }

        # dev server: always revalidate so edits show up after a normal reload
        $ctx.Response.Headers['Cache-Control'] = 'no-cache'

        $bytes = [System.IO.File]::ReadAllBytes($fullFile)
        $ctx.Response.ContentLength64 = $bytes.Length
        $ctx.Response.AddHeader('Accept-Ranges', 'bytes')
        $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        $ctx.Response.Close()
        Write-Host "$(Get-Date -Format HH:mm:ss) 200 $path"
    } catch {
        try { $ctx.Response.Close() } catch {}
        Write-Host "ERR: $_"
    }
}
