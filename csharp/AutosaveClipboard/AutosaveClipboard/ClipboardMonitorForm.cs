using ClipboardMonitor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Timers;

namespace ClipboardMonitor
{
    public class ClipboardMonitorForm : Form
    {
        private const int HOTKEY_ID_PREVIOUS = 1;
        private const int HOTKEY_ID_NEXT = 2;
        private const int MOD_CONTROL = 0x2;
        private const int MOD_SHIFT = 0x4;
        private const int MOD_ALT = 0x1;
        private const int WM_HOTKEY = 0x312;
        private const int StatusBarHeight = 25;
        private const double ElapsedTimeUpdateInterval = 1000;

        private PictureBox _thumbnailPictureBox;
        private Label _copiedTextBox;
        private Label _itemIndexLabel;
        private Label _resolutionLabel;
        private Label _addedTimeLabel;
        private Label _lengthLabel;
        private Label _elapsedTimeLabel;
        private Button _previousButton;
        private Button _nextButton;

        private DateTime _lastAddedTime;
        private System.Timers.Timer _elapsedTimeUpdateTimer;
        private ClipboardManager _clipboardManager;
        public StatusBarManager _statusBarManager;
        private NavigationManager _navigationManager;

        public Label ItemIndexLabel => _itemIndexLabel;
        public Label ResolutionLabel => _resolutionLabel;
        public Label AddedTimeLabel => _addedTimeLabel;
        public Label LengthLabel => _lengthLabel;
        public Label ElapsedTimeLabel => _elapsedTimeLabel;
        public List<ClipboardItem> ClipboardHistory => _clipboardManager.ClipboardHistory;
        public PictureBox ThumbnailPictureBox => _thumbnailPictureBox;
        public int ClipboardIndex => _clipboardManager.ClipboardIndex;
        public Label TextLabel => _copiedTextBox;

        public ClipboardMonitorForm()
        {
            Text = "CLP";
            Size = new Size(800, 900);

            _clipboardManager = new ClipboardManager();
            _statusBarManager = new StatusBarManager(this);
            _navigationManager = new NavigationManager(this, _clipboardManager);

            RegisterHotKey(this.Handle, HOTKEY_ID_PREVIOUS, MOD_CONTROL | MOD_SHIFT | MOD_ALT, (int)Keys.Left);
            RegisterHotKey(this.Handle, HOTKEY_ID_NEXT, MOD_CONTROL | MOD_SHIFT | MOD_ALT, (int)Keys.Right);

            InitializeComponents();

            _clipboardManager.ClipboardChanged += OnClipboardChanged;
            _navigationManager.ClipboardChanged += OnClipboardChanged;
            _statusBarManager.StartTimer();
            Resize += ClipboardMonitorForm_Resize;
            ClipboardMonitorForm_Resize(this, EventArgs.Empty);

            _elapsedTimeUpdateTimer = new System.Timers.Timer(ElapsedTimeUpdateInterval);
            _elapsedTimeUpdateTimer.Elapsed += (s, e) => UpdateElapsedTime();
            _elapsedTimeUpdateTimer.Start();
        }

        private Label NewLabel()
        {
            return new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = StatusBarHeight,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = true
            };
        }

        private void InitializeComponents()
        {
            _thumbnailPictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(0, 0),
                Size = new Size(Width, Height - StatusBarHeight),
                Visible = false,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.Fixed3D
            };

            _copiedTextBox = new Label
            {
                Location = new Point(0, 0),
                Size = new Size(Width, Height - StatusBarHeight),
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Arial", 18),
                Visible = false,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.Fixed3D
            };

            Controls.Add(_thumbnailPictureBox);
            Controls.Add(_copiedTextBox);

            _itemIndexLabel = NewLabel();
            _resolutionLabel = NewLabel();
            _addedTimeLabel = NewLabel();
            _lengthLabel = NewLabel();
            _elapsedTimeLabel = NewLabel();

            Controls.Add(_itemIndexLabel);
            Controls.Add(_resolutionLabel);
            Controls.Add(_addedTimeLabel);
            Controls.Add(_lengthLabel);
            Controls.Add(_elapsedTimeLabel);

            _previousButton = new Button
            {
                Text = "← Back",
                Font = new Font("Arial", 12),
                Size = new Size(100, StatusBarHeight)
            };
            _previousButton.Click += (s, e) => _navigationManager.LoadPreviousClipboardItem();

            _nextButton = new Button
            {
                Text = "→ Forward",
                Font = new Font("Arial", 12),
                Size = new Size(100, StatusBarHeight)
            };
            _nextButton.Click += (s, e) => _navigationManager.LoadNextClipboardItem();

            Controls.Add(_previousButton);
            Controls.Add(_nextButton);
        }

        private void ClipboardMonitorForm_Resize(object sender, EventArgs e)
        {
            int totalUnits = 6; // 5 labels + 1 unit for both buttons
            int labelWidth = Width / totalUnits;
            int statusBarTop = ClientSize.Height - StatusBarHeight;
            var ii = 0;

            foreach (var label in new[] { _itemIndexLabel, _resolutionLabel, _addedTimeLabel, _lengthLabel, _elapsedTimeLabel })
            {
                label.Width = labelWidth;
                label.Height = StatusBarHeight;
                label.Top = statusBarTop;
                label.Left = labelWidth * ii;
                ii++;
            }

            _previousButton.Width = labelWidth / 2;
            _nextButton.Width = labelWidth / 2;
            _previousButton.Height = StatusBarHeight;
            _nextButton.Height = StatusBarHeight;
            _previousButton.Top = statusBarTop;
            _nextButton.Top = statusBarTop;
            _previousButton.Left = labelWidth * ii;
            _nextButton.Left = _previousButton.Left + _previousButton.Width;

            var factor = 2.5;
            _thumbnailPictureBox.Size = new Size(ClientSize.Width, Height - (int)(factor * StatusBarHeight));
            _copiedTextBox.Size = new Size(ClientSize.Width, Height - (int)(factor * StatusBarHeight));
        }

        private void ShowNotification(string message)
        {
            using (var notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                BalloonTipTitle = "Clipboard Monitor",
                BalloonTipText = message,
                Visible = true
            })
            {
                notifyIcon.ShowBalloonTip(3000);
            }
        }

        private void UpdateElapsedTime()
        {
            if (_lastAddedTime == DateTime.MinValue)
                return;

            TimeSpan elapsed = DateTime.Now - _lastAddedTime;
            string elapsedTimeText = FormatElapsedTime(elapsed);

            if (InvokeRequired)
            {
                Invoke(new Action(() => _elapsedTimeLabel.Text = elapsedTimeText));
            }
            else
            {
                _elapsedTimeLabel.Text = elapsedTimeText;
            }
        }

        private string FormatElapsedTime(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 60)
                return $"{elapsed.Seconds} seconds ago";
            if (elapsed.TotalMinutes < 60)
                return $"{(int)elapsed.TotalMinutes} min, {elapsed.Seconds} s ago";
            return $"{(int)elapsed.TotalHours} hr, {(int)elapsed.TotalMinutes % 60} min ago";
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_PREVIOUS)
                {
                    _navigationManager.LoadPreviousClipboardItem();
                }
                else if (id == HOTKEY_ID_NEXT)
                {
                    _navigationManager.LoadNextClipboardItem();
                }
            }
            base.WndProc(ref m);
        }

        private void OnClipboardChanged(object sender, ClipboardChangedEventArgs e)
        {
            if (e.ClipboardItem.Type == ClipboardItemType.Text)
            {
                var text = e.ClipboardItem.Content.ToString();
                int lineCount = text.Split('\n').Length;
                string preview = text.Length > 30 ? text.Substring(0, 30) + "..." : text;
                ShowNotification($"TEXT:\t\t{preview}\r\n\t{lineCount} LINES");
                _copiedTextBox.Text = text;
                _copiedTextBox.Visible = true;
                _thumbnailPictureBox.Visible = false;
                var plural = (lineCount > 1 || lineCount == 0) ? "s" : "";
                Text = $"CLP - {preview} ({lineCount} line{plural}, {text.Length} chars)";
            }
            else if (e.ClipboardItem.Type == ClipboardItemType.Image)
            {
                var image = (Image)e.ClipboardItem.Content;
                ShowNotification($"IMAGE:\t\t{image.Width}x{image.Height}, {image.PixelFormat}");
                _thumbnailPictureBox.Image = image;
                _thumbnailPictureBox.Visible = true;
                _copiedTextBox.Visible = false;
                Text = $"CLP - Image - {e.Resolution}";
            }
            _statusBarManager.UpdateStatusBar(e.ClipboardIndex, e.Resolution, e.AddedTime);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private void InitializeComponent()
        {
            // Removed unused method.
        }
    }
}
