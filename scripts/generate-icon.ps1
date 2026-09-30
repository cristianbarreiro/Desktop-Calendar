Add-Type -AssemblyName System.Drawing

function Create-CalendarBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap($size, $size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $size / 64.0

    # Calendar base card
    $rectX = [int](8 * $scale)
    $rectY = [int](10 * $scale)
    $rectW = [int](48 * $scale)
    $rectH = [int](46 * $scale)

    # Background brush & border
    $bgBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(250, 250, 252))
    $penWidth = [float][Math]::Max(1.0, 1.5 * $scale)
    $borderPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(60, 60, 67)), $penWidth
    $headerBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(37, 99, 235)) # Royal Blue

    # Fill card
    $g.FillRectangle($bgBrush, $rectX, $rectY, $rectW, $rectH)
    # Header bar
    $headerH = [int](14 * $scale)
    $g.FillRectangle($headerBrush, $rectX, $rectY, $rectW, $headerH)
    # Outline
    $g.DrawRectangle($borderPen, $rectX, $rectY, $rectW, $rectH)

    # Spiral / Binder rings
    $ringBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(100, 116, 139))
    $ring1X = [int](18 * $scale)
    $ring2X = [int](30 * $scale)
    $ring3X = [int](42 * $scale)
    $ringY = [int](6 * $scale)
    $ringW = [Math]::Max(2, [int](4 * $scale))
    $ringH = [Math]::Max(4, [int](8 * $scale))
    $g.FillRectangle($ringBrush, $ring1X, $ringY, $ringW, $ringH)
    $g.FillRectangle($ringBrush, $ring2X, $ringY, $ringW, $ringH)
    $g.FillRectangle($ringBrush, $ring3X, $ringY, $ringW, $ringH)

    # Calendar day grid dots
    $dotBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(71, 85, 105))
    $dotSize = [Math]::Max(1, [int](5 * $scale))

    for ($row = 0; $row -lt 3; $row++) {
        for ($col = 0; $col -lt 4; $col++) {
            if ($row -eq 1 -and $col -eq 2) {
                $accentDot = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(239, 68, 68))
                $g.FillRectangle($accentDot, [int](($rectX + 8 * $scale) + $col * (10 * $scale)), [int](($rectY + 18 * $scale) + $row * (9 * $scale)), $dotSize, $dotSize)
                $accentDot.Dispose()
            } else {
                $g.FillRectangle($dotBrush, [int](($rectX + 8 * $scale) + $col * (10 * $scale)), [int](($rectY + 18 * $scale) + $row * (9 * $scale)), $dotSize, $dotSize)
            }
        }
    }

    $g.Dispose()
    return $bmp
}

# Generate multi-size icon
$sizes = @(16, 32, 48, 64, 128, 256)
$bitmaps = $sizes | ForEach-Object { Create-CalendarBitmap $_ }

$targetPath = "src/CalendarWidget.App/app.ico"
$targetDir = [System.IO.Path]::GetDirectoryName($targetPath)
if (-not (Test-Path $targetDir)) {
    [System.IO.Directory]::CreateDirectory($targetDir) | Out-Null
}

$stream = New-Object System.IO.FileStream $targetPath, ([System.IO.FileMode]::Create)
$writer = New-Object System.IO.BinaryWriter $stream

# ICONDIR header
$writer.Write([UInt16]0) # Reserved
$writer.Write([UInt16]1) # Type 1 = ICO
$writer.Write([UInt16]$sizes.Count) # Image count

$offset = 6 + ($sizes.Count * 16)
$pngStreams = @()

foreach ($bmp in $bitmaps) {
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytes = $ms.ToArray()
    $pngStreams += ,$pngBytes

    $w = if ($bmp.Width -ge 256) { [byte]0 } else { [byte]$bmp.Width }
    $h = if ($bmp.Height -ge 256) { [byte]0 } else { [byte]$bmp.Height }

    $writer.Write($w)
    $writer.Write($h)
    $writer.Write([byte]0) # Color palette count
    $writer.Write([byte]0) # Reserved
    $writer.Write([UInt16]1) # Color planes
    $writer.Write([UInt16]32) # Bits per pixel
    $writer.Write([UInt32]$pngBytes.Length)
    $writer.Write([UInt32]$offset)

    $offset += $pngBytes.Length
}

foreach ($bytes in $pngStreams) {
    $writer.Write($bytes)
}

$writer.Flush()
$writer.Close()
$stream.Close()

Write-Output "Successfully generated $targetPath"
