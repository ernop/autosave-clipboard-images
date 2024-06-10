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
    public const int WM_HOTKEY = 0x312;
}

public class CustomForm : Form
{
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == HotKeyRegister.WM_HOTKEY)
        {
            MessageBox.Show("CTRL+SHIFT+C pressed");
        }
        base.WndProc(ref m);
    }
}
"@ -ReferencedAssemblies "System.Windows.Forms"

$form = New-Object CustomForm

# Register the global hotkey for CTRL+SHIFT+C
$hotkeyId = 1
$hotkey = [HotKeyRegister]::RegisterHotKey($form.Handle, $hotkeyId, [HotKeyRegister]::MOD_CONTROL -bor [HotKeyRegister]::MOD_SHIFT, [System.Windows.Forms.Keys]::C)

if (-not $hotkey) {
    Write-Host "Failed to register hotkey CTRL+SHIFT+C."
    exit
}

$form.Add_FormClosed({
    # Unregister the hotkey and clean up
    [HotKeyRegister]::UnregisterHotKey($form.Handle, $hotkeyId)
    $form.Dispose()
})

$form.ShowDialog()
