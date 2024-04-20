Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$folderPath = "C:\Screenshots"
if (-not (Test-Path -Path $folderPath)) {
    New-Item -ItemType Directory -Force -Path $folderPath
}

$lastHash = ""

function Get-ImageHash($image) {
    $bitmap = New-Object System.Drawing.Bitmap $image
    $ms = New-Object System.IO.MemoryStream
    $bitmap.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $hashAlgorithm = [System.Security.Cryptography.HashAlgorithm]::Create("SHA256")
    $hash = $hashAlgorithm.ComputeHash($ms.ToArray())
    $bitmap.Dispose()
    $ms.Dispose()
    return [BitConverter]::ToString($hash) -replace '-', ''
}

function SaveClipboardImage {
    if ([System.Windows.Forms.Clipboard]::ContainsImage()) {
        $image = [System.Windows.Forms.Clipboard]::GetImage()
        $currentHash = Get-ImageHash $image
        if ($currentHash -ne $global:lastHash) {
            $fileName = [System.IO.Path]::Combine($folderPath, "Screenshot_" + (Get-Date -Format "yyyyMMddHHmmss") + ".png")
            $bitmap = New-Object System.Drawing.Bitmap $image
            $bitmap.Save($fileName, [System.Drawing.Imaging.ImageFormat]::Png)
            $bitmap.Dispose()
            Write-Host "Saved $fileName"
            $global:lastHash = $currentHash
        }
    }
}

Write-Host "Monitoring clipboard for images. Press CTRL+C to stop."

while ($true) {
    Start-Sleep -Seconds 1
    SaveClipboardImage
}