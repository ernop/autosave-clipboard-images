using System;
using System.Windows.Forms;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace ClipboardMonitor
{
    public class StatusBarManager
    {
        private ClipboardMonitorForm _form;
        private Timer _updateTimer;
        private DateTime _lastAddedTime;

        public StatusBarManager(ClipboardMonitorForm form)
        {
            _form = form;
            _updateTimer = new Timer();
            _updateTimer.Interval = 1000;
            _updateTimer.Tick += UpdateTimer_Tick;
        }

        public void StartTimer()
        {
            _updateTimer.Start();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_lastAddedTime != DateTime.MinValue)
            {
                var elapsedTime = DateTime.Now - _lastAddedTime;
                _form.ElapsedTimeLabel.Text = $"Copied: {elapsedTime.TotalSeconds:F0}s  ago";
            }
        }

        public void UpdateStatusBar(int index, string resolution = null, DateTime? addedTime = null, string originalFilename = null)
        {
            if (_form.ClipboardHistory.Count == 0 || index < 0 || index >= _form.ClipboardHistory.Count)
            {
                return;
            }
            _lastAddedTime = addedTime ?? DateTime.MinValue;
            var elapsedTime = DateTime.Now - _lastAddedTime;
            var elapsedTimeText = $"Copied: {elapsedTime.TotalSeconds:F0}s ago";
            _form.ElapsedTimeLabel.Text = elapsedTimeText;
            var item = _form.ClipboardHistory[index];
            if (item.Type == ClipboardItemType.Text)
            {
                string text = (string)item.Content;
                int lineCount = text.Split('\n').Length;
                var plural = (lineCount > 1 || lineCount == 0) ? "s" : "";
                _form.LengthLabel.Text = $"{lineCount} line{plural} ({text.Length} chars)";
            }
            else if (item.Type == ClipboardItemType.Image)
            {
                var imgResolution = resolution ?? $"{((System.Drawing.Image)item.Content).Width}x{((Image)item.Content).Height}";
                var imgAddedTime = addedTime ?? item.AddedTime;
                var imgFilename = originalFilename ?? item.OriginalFilename;
                _form.LengthLabel.Text = "";
                _form.ResolutionLabel.Text = $"Resolution: {imgResolution}";
            }
            _form.ItemIndexLabel.Text = $"Item {_form.ClipboardIndex+ 1}/{_form.ClipboardHistory.Count}";
            _form.AddedTimeLabel.Text = $"Added: {addedTime:dddd, HH:mm:ss}";
        }
    }
}
