# Clipboard Watcher Watchdog
# This script runs as an NSSM service and ensures the clipboard watcher is always running.
# It checks every 30 seconds and restarts the scheduled task if needed.

$taskName = "AutosaveClipboardWatcher"
$scriptPath = "C:\proj\autosave-clipboard-images\autosave-clipboard-images.ps1"
$logFile = "C:\proj\autosave-clipboard-images\watchdog.log"
$checkInterval = 30  # seconds

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] $Message"
    Write-Host $logMessage
    Add-Content -Path $logFile -Value $logMessage -ErrorAction SilentlyContinue
}

function Test-ClipboardWatcherProcessRunning {
    # Check if there's an actual pwsh process running the clipboard script
    $processes = Get-CimInstance Win32_Process | Where-Object { 
        $_.CommandLine -like "*autosave-clipboard-images.ps1*" 
    }
    return ($null -ne $processes -and @($processes).Count -gt 0)
}

function Test-ClipboardWatcherTaskRunning {
    # Check if the scheduled task is in Running state
    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($null -eq $task) {
        return $false
    }
    return $task.State -eq "Running"
}

function Start-ClipboardWatcher {
    Write-Log "Starting clipboard watcher task..."
    try {
        Start-ScheduledTask -TaskName $taskName -ErrorAction Stop
        
        # Wait up to 10 seconds for the process to appear
        $attempts = 0
        while ($attempts -lt 10) {
            Start-Sleep -Seconds 1
            $attempts++
            if (Test-ClipboardWatcherProcessRunning) {
                Write-Log "Clipboard watcher process started successfully (attempt $attempts)."
                return $true
            }
        }
        Write-Log "WARNING: Task started but process not detected after 10 seconds."
        return $false
    } catch {
        Write-Log "ERROR: Failed to start task: $_"
        return $false
    }
}

# Main loop
Write-Log "========================================="
Write-Log "Clipboard Watchdog Service Started"
Write-Log "Monitoring task: $taskName"
Write-Log "Check interval: $checkInterval seconds"
Write-Log "========================================="

# Initial check
if (Test-ClipboardWatcherProcessRunning) {
    Write-Log "Clipboard watcher process is already running."
} else {
    Write-Log "Clipboard watcher process not found. Starting..."
    Start-ClipboardWatcher
}

while ($true) {
    Start-Sleep -Seconds $checkInterval
    try {
        if (-not (Test-ClipboardWatcherProcessRunning)) {
            Write-Log "Clipboard watcher process NOT running. Attempting restart..."
            Start-ClipboardWatcher
        }
    } catch {
        Write-Log "ERROR in monitoring loop: $_"
    }
}
