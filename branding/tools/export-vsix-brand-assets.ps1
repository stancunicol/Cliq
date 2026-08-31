param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

Add-Type -AssemblyName System.Drawing

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputDirectory)
[System.IO.Directory]::CreateDirectory($resolvedOutput) | Out-Null

function New-CliqIcon {
    param(
        [int]$Size,
        [string]$Path
    )

    $bitmap = [System.Drawing.Bitmap]::new($Size, $Size)
    $bitmap.SetResolution(96, 96)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $inset = [single]($Size * 0.035)
    $radius = [single]($Size * 0.265)
    $bounds = [System.Drawing.RectangleF]::new($inset, $inset, $Size - 2 * $inset, $Size - 2 * $inset)
    $surfacePath = [System.Drawing.Drawing2D.GraphicsPath]::new()
    $diameter = 2 * $radius
    $surfacePath.AddArc($bounds.Left, $bounds.Top, $diameter, $diameter, 180, 90)
    $surfacePath.AddArc($bounds.Right - $diameter, $bounds.Top, $diameter, $diameter, 270, 90)
    $surfacePath.AddArc($bounds.Right - $diameter, $bounds.Bottom - $diameter, $diameter, $diameter, 0, 90)
    $surfacePath.AddArc($bounds.Left, $bounds.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $surfacePath.CloseFigure()

    $surface = [System.Drawing.Drawing2D.LinearGradientBrush]::new(
        $bounds,
        [System.Drawing.Color]::FromArgb(255, 27, 16, 46),
        [System.Drawing.Color]::FromArgb(255, 9, 7, 13),
        45)
    $graphics.FillPath($surface, $surfacePath)
    $border = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 46, 29, 73), [single]($Size * 0.025))
    $graphics.DrawPath($border, $surfacePath)

    $arcBox = [System.Drawing.RectangleF]::new($Size * 0.235, $Size * 0.235, $Size * 0.53, $Size * 0.53)
    $signal = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(255, 139, 92, 246), [single]($Size * 0.11))
    $signal.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $signal.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawArc($signal, $arcBox, 45, 270)

    $nodeSize = [single]($Size * 0.13)
    $nodeX = [single]($Size * 0.69 - $nodeSize / 2)
    $topY = [single]($Size * 0.31 - $nodeSize / 2)
    $bottomY = [single]($Size * 0.69 - $nodeSize / 2)
    $graphics.FillEllipse([System.Drawing.Brushes]::White, $nodeX, $topY, $nodeSize, $nodeSize)
    $violet = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255, 139, 92, 246))
    $graphics.FillEllipse($violet, $nodeX, $bottomY, $nodeSize, $nodeSize)

    $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)

    $violet.Dispose()
    $signal.Dispose()
    $border.Dispose()
    $surface.Dispose()
    $surfacePath.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

New-CliqIcon -Size 128 -Path (Join-Path $resolvedOutput 'CliqIcon.png')
New-CliqIcon -Size 200 -Path (Join-Path $resolvedOutput 'CliqPreview.png')
