# Remove any existing service
C:\tools\nssm.exe stop ClipboardWatchdog 2>$null
C:\tools\nssm.exe remove ClipboardWatchdog confirm 2>$null
Start-Sleep -Seconds 1

# Install the service
C:\tools\nssm.exe install ClipboardWatchdog "C:\Program Files\PowerShell\7\pwsh.exe" "-NoProfile -ExecutionPolicy Bypass -File C:\proj\autosave-clipboard-images\clipboard-watchdog.ps1"

# Configure restart behavior - restart on failure after 30 seconds
C:\tools\nssm.exe set ClipboardWatchdog AppExit Default Restart
C:\tools\nssm.exe set ClipboardWatchdog AppRestartDelay 30000

# Configure logging
C:\tools\nssm.exe set ClipboardWatchdog AppStdout C:\proj\autosave-clipboard-images\watchdog-stdout.log
C:\tools\nssm.exe set ClipboardWatchdog AppStderr C:\proj\autosave-clipboard-images\watchdog-stderr.log
C:\tools\nssm.exe set ClipboardWatchdog AppRotateFiles 1
C:\tools\nssm.exe set ClipboardWatchdog AppRotateBytes 1048576

# Set service to start automatically
C:\tools\nssm.exe set ClipboardWatchdog Start SERVICE_AUTO_START

# Set description
C:\tools\nssm.exe set ClipboardWatchdog Description "Monitors and ensures the clipboard watcher is always running"

# Start the service
C:\tools\nssm.exe start ClipboardWatchdog

Write-Host "Service installed and started. Press any key to close..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
