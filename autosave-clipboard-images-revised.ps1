# Import necessary assemblies
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# Set the save folder path
$folderPath = "C:\Screenshots"

# Create the folder if it doesn't exist
if (-not (Test-Path -Path $folderPath)) {
    New-Item -ItemType Directory -Force -Path $folderPath
}

# Initialize variables to track last saved content
$lastImageHash = $null
$lastTextHash = $null
$lastText = ""

# Function to calculate image hash
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

# Function to save text to a file
function SaveTextToFile($text) {
    # Generate unique filename based on text prefix and timestamp
    $fileName = [System.IO.Path]::Combine($folderPath, 
        ($text.Substring(0, [Math]::Min(20, $text.Length)) -replace "[^\w\d]", "") + "_" + 
        (Get-Date -Format "yyyyMMddHHmmss") + ".txt")
    $text | Out-File -FilePath $fileName
    Write-Host "Saved text: $fileName"
}

# Function to save clipboard content
function SaveClipboardContent {
    if ([System.Windows.Forms.Clipboard]::ContainsText()) {
        $text = [System.Windows.Forms.Clipboard]::GetText()
        $currentTextHash = $text.GetHashCode().ToString()
        if ($currentTextHash -ne $global:lastTextHash) {
            SaveTextToFile $text
            $global:lastText = $text
            $global:lastTextHash = $currentTextHash
            $global:lastImageHash = $null
        }
    } elseif ([System.Windows.Forms.Clipboard]::ContainsImage()) {
        $image = [System.Windows.Forms.Clipboard]::GetImage()
        $currentImageHash = Get-ImageHash $image
        if ($currentImageHash -ne $global:lastImageHash) {
            # Generate unique filename based on timestamp
            $fileName = [System.IO.Path]::Combine($folderPath, "Screenshot_" + (Get-Date -Format "yyyyMMddHHmmss") + ".png")
            $bitmap = New-Object System.Drawing.Bitmap $image
            $bitmap.Save($fileName, [System.Drawing.Imaging.ImageFormat]::Png)
            $bitmap.Dispose()
            Write-Host "Saved image: $fileName"
            [System.Windows.Forms.Clipboard]::SetText($global:lastText)
            $global:lastImageHash = $currentImageHash
        }
    }
}

Write-Host "Monitoring clipboard for images and text. Press CTRL+C to stop."

# Continuously monitor the clipboard for changes
while ($true) {
    Start-Sleep -Seconds 1
    SaveClipboardContent
}