Add-Type -AssemblyName System.Windows.Forms

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class HotKeyRegister
{
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public const int MOD_CONTROL = 0x2;
    public const int MOD_SHIFT = 0x4;
    public const int MOD_ALT = 0x1;
    public const int WM_HOTKEY = 0x312;
}

public class CustomForm : Form
{
    public Action OnPrevious;
    public Action OnNext;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == HotKeyRegister.WM_HOTKEY)
        {
            int id = m.WParam.ToInt32();
            if (id == 1)
            {
                if (OnPrevious != null) OnPrevious.Invoke();
            }
            else if (id == 2)
            {
                if (OnNext != null) OnNext.Invoke();
            }
        }
        base.WndProc(ref m);
    }
}
"@ -ReferencedAssemblies "System.Windows.Forms"

# Set the save folder path
$folderPath = "C:\Screenshots"

# Create the folder if it doesn't exist
if (-not (Test-Path -Path $folderPath)) {
    New-Item -ItemType Directory -Force -Path $folderPath
}

# Initialize global variables for clipboard history and hashes
$global:clipboardHistory = @()
$global:clipboardIndex = -1
$global:maxHistory = 10
$global:seenImageHashes = @()
$global:seenTextHashes = @()

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

# Function to save text to file
function SaveTextToFile($text) {
    # Generate unique filename based on text prefix and timestamp
    $fileName = [System.IO.Path]::Combine($folderPath,
        ($text.Substring(0, [Math]::Min(20, $text.Length)) -replace "[^\w\d_-]", "-") + "_" +
        (Get-Date -Format "yyyyMMddHHmmss") + ".txt")
    $text | Out-File -FilePath $fileName
    Write-Host "Saved text: $fileName"
}

# Function to save clipboard content with hash checks
function SaveClipboardContent {
    if ([System.Windows.Forms.Clipboard]::ContainsText()) {
        $text = [System.Windows.Forms.Clipboard]::GetText()
        $currentTextHash = $text.GetHashCode().ToString()
        if ($currentTextHash -notin $global:seenTextHashes) {
            SaveTextToFile $text
            $global:seenTextHashes += $currentTextHash
            AddToClipboardHistory "text" $text
            Write-Host "New text copied and saved."
        }
    } elseif ([System.Windows.Forms.Clipboard]::ContainsImage()) {
        $image = [System.Windows.Forms.Clipboard]::GetImage()
        $currentImageHash = Get-ImageHash $image
        if ($currentImageHash -notin $global:seenImageHashes) {
            # Generate unique filename based on timestamp
            $fileName = [System.IO.Path]::Combine($folderPath, "Screenshot_" + (Get-Date -Format "yyyyMMddHHmmss") + ".png")
            $bitmap = New-Object System.Drawing.Bitmap $image
            $bitmap.Save($fileName, [System.Drawing.Imaging.ImageFormat]::Png)
            $bitmap.Dispose()
            Write-Host "Saved image: $fileName"
            $global:seenImageHashes += $currentImageHash
            AddToClipboardHistory "image" $image
            Write-Host "New image copied and saved."
        }
    } else {
        Write-Host "No new text or image found in clipboard."
    }
}

# Function to add to clipboard history
function AddToClipboardHistory($type, $content) {
    $global:clipboardHistory = $global:clipboardHistory + @([pscustomobject]@{Type=$type; Content=$content})
    if ($global:clipboardHistory.Count -gt $global:maxHistory) {
        $global:clipboardHistory = $global:clipboardHistory[-$global:maxHistory..-1]
    }
    $global:clipboardIndex = $global:clipboardHistory.Count - 1
}

# Function to load clipboard content
function LoadClipboardContent {
    param (
        [int]$index
    )
    if ($index -ge 0 -and $index -lt $global:clipboardHistory.Count) {
        $item = $global:clipboardHistory[$index]
        if ($item.Type -eq "text") {
            [System.Windows.Forms.Clipboard]::SetText($item.Content)
            ShowNotification "Loaded text"
        } elseif ($item.Type -eq "image") {
            [System.Windows.Forms.Clipboard]::SetImage($item.Content)
            ShowNotification "Loaded image"
        }
    }
}

# Function to show notification
function ShowNotification($message) {
    $notifyIcon = New-Object System.Windows.Forms.NotifyIcon
    $notifyIcon.Icon = [System.Drawing.SystemIcons]::Information
    $notifyIcon.BalloonTipTitle = "Clipboard Monitor"
    $notifyIcon.BalloonTipText = $message
    $notifyIcon.Visible = $true
    $notifyIcon.ShowBalloonTip(3000)
    Start-Sleep -Seconds 3
    $notifyIcon.Dispose()
}

# Initialize the custom form and start it in a separate thread
$form = New-Object CustomForm

$form.OnPrevious = {
    if ($global:clipboardHistory.Count -gt 0) {
        Write-Host "Ctrl+Shift+Alt+Left Arrow pressed: Loading previous clipboard item"
        $global:clipboardIndex = ($global:clipboardIndex - 1 + $global:clipboardHistory.Count) % $global:clipboardHistory.Count
        LoadClipboardContent -index $global:clipboardIndex
    } else {
        Write-Host "No clipboard items to load"
    }
}

$form.OnNext = {
    if ($global:clipboardHistory.Count -gt 0) {
        Write-Host "Ctrl+Shift+Alt+Right Arrow pressed: Loading next clipboard item"
        $global:clipboardIndex = ($global:clipboardIndex + 1) % $global:clipboardHistory.Count
        LoadClipboardContent -index $global:clipboardIndex
    } else {
        Write-Host "No clipboard items to load"
    }
}

# Register the global hotkeys
$hotkeyId1 = 1
$hotkey1 = [HotKeyRegister]::RegisterHotKey($form.Handle, $hotkeyId1, [HotKeyRegister]::MOD_CONTROL -bor [HotKeyRegister]::MOD_SHIFT -bor [HotKeyRegister]::MOD_ALT, [System.Windows.Forms.Keys]::Left)
if (-not $hotkey1) {
    Write-Host "Failed to register hotkey Ctrl+Shift+Alt+Left Arrow."
    exit
}

$hotkeyId2 = 2
$hotkey2 = [HotKeyRegister]::RegisterHotKey($form.Handle, $hotkeyId2, [HotKeyRegister]::MOD_CONTROL -bor [HotKeyRegister]::MOD_SHIFT -bor [HotKeyRegister]::MOD_ALT, [System.Windows.Forms.Keys]::Right)
if (-not $hotkey2) {
    Write-Host "Failed to register hotkey Ctrl+Shift+Alt+Right Arrow."
    exit
}

$form.Add_FormClosed({
    # Unregister the hotkeys and clean up
    [HotKeyRegister]::UnregisterHotKey($form.Handle, $hotkeyId1)
    [HotKeyRegister]::UnregisterHotKey($form.Handle, $hotkeyId2)
    $form.Dispose()
})

$thread = [System.Threading.Thread]::New([System.Threading.ThreadStart]{
    [System.Windows.Forms.Application]::Run($form)
})
$thread.IsBackground = $true
$thread.Start()

Write-Host "Monitoring clipboard for images and text. Press CTRL+C to stop."

try {
    # Continuously monitor the clipboard for changes
    while ($true) {
        Start-Sleep -Seconds 1
        Write-Host "Checking clipboard content..."
        SaveClipboardContent
    }
} catch [System.Management.Automation.Host.HostException] {
    Write-Host "Script terminated by user."
    [HotKeyRegister]::UnregisterHotKey($form.Handle, $hotkeyId1)
    [HotKeyRegister
