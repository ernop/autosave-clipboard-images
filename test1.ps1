Add-Type -AssemblyName System.Windows.Forms

$form = New-Object System.Windows.Forms.Form
$form.WindowState = 'Minimized'
$form.ShowInTaskbar = $false
$form.Add_KeyDown({
    param($sender, $e)
    if ($e.KeyCode -eq [System.Windows.Forms.Keys]::F6 -and $e.Control -and $e.Shift) {
        Write-Host "CTRL+SHIFT+F6 pressed"
    }
})

$form.ShowDialog()
