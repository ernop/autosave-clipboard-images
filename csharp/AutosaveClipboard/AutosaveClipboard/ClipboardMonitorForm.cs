using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

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
        private System.Windows.Forms.Timer _timer;
        private List<ClipboardItem> _clipboardHistory;
        private int _clipboardIndex;
        private HashSet<string> _seenHashes;
        private string _folderPath = "C:\\Screenshots";

        private PictureBox _thumbnailPictureBox;
        private Label _clipboardInfoLabel;
        private Label _textLabel;

        public ClipboardMonitorForm()
        {
            _clipboardHistory = new List<ClipboardItem>();
            _clipboardIndex = -1;
            _seenHashes = new HashSet<string>();

            Directory.CreateDirectory(_folderPath);

            RegisterHotKey(this.Handle, HOTKEY_ID_PREVIOUS, MOD_CONTROL | MOD_SHIFT | MOD_ALT, (int)Keys.Left);
            RegisterHotKey(this.Handle, HOTKEY_ID_NEXT, MOD_CONTROL | MOD_SHIFT | MOD_ALT, (int)Keys.Right);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 1000; // 1 second
            _timer.Tick += Timer_Tick;
            _timer.Start();

            _thumbnailPictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Bottom,
                Height = 200, // Adjust the height as needed
                Visible = false
            };

            _textLabel = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Visible = false
            };

            _clipboardInfoLabel = new Label
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Controls.Add(_thumbnailPictureBox);
            Controls.Add(_textLabel);
            Controls.Add(_clipboardInfoLabel);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            SaveClipboardContent();
        }

        private void SaveClipboardContent()
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                string hash = ComputeHash(text);
                if (!_seenHashes.Contains(hash))
                {
                    SaveTextToFile(text);
                    _seenHashes.Add(hash);
                    var item = new ClipboardItem { Type = ClipboardItemType.Text, Content = text };
                    _clipboardHistory.Add(item);
                    _clipboardIndex = _clipboardHistory.Count - 1;

                    DisplayClipboardContent(item);
                }
            }
            else if (Clipboard.ContainsImage())
            {
                Image image = Clipboard.GetImage();
                string hash = ComputeHash(image);
                if (!_seenHashes.Contains(hash))
                {
                    SaveImageToFile(image);
                    _seenHashes.Add(hash);
                    var item = new ClipboardItem { Type = ClipboardItemType.Image, Content = image };
                    _clipboardHistory.Add(item);
                    _clipboardIndex = _clipboardHistory.Count - 1;

                    DisplayClipboardContent(item);
                }
            }
        }

        private void SaveTextToFile(string text)
        {
            string fileName = Path.Combine(_folderPath, $"Text_{DateTime.Now:yyyyMMddHHmmss}.txt");
            File.WriteAllText(fileName, text);
            Console.WriteLine($"Saved text: {fileName}");
        }

        private void SaveImageToFile(Image image)
        {
            string fileName = Path.Combine(_folderPath, $"Screenshot_{DateTime.Now:yyyyMMddHHmmss}.png");
            image.Save(fileName);
            Console.WriteLine($"Saved image: {fileName}");
        }

        private string ComputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        private string ComputeHash(Image image)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] bytes = ms.ToArray();
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(bytes);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                }
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_ID_PREVIOUS)
                {
                    LoadPreviousClipboardItem();
                }
                else if (id == HOTKEY_ID_NEXT)
                {
                    LoadNextClipboardItem();
                }
            }
            base.WndProc(ref m);
        }
        private void LoadPreviousClipboardItem()
        {
            if (_clipboardIndex > 0)
            {
                _clipboardIndex--;
                LoadClipboardContent(_clipboardIndex);
                Console.WriteLine("Loaded previous clipboard item");
            }
        }
        private void LoadNextClipboardItem()
        {
            if (_clipboardIndex < _clipboardHistory.Count - 1)
            {
                _clipboardIndex++;
                LoadClipboardContent(_clipboardIndex);
                Console.WriteLine("Loaded next clipboard item");
            }
        }

        private void DisplayClipboardContent(ClipboardItem item)
        {
            _clipboardInfoLabel.Text = $"You have loaded clipboard item {_clipboardIndex + 1}/{_clipboardHistory.Count}";

            if (item.Type == ClipboardItemType.Text)
            {
                string text = (string)item.Content;
                int newlineCount = text.Split('\n').Length - 1;
                string preview = text.Length > 30 ? text.Substring(0, 30) + "..." : text;
                ShowNotification($"TEXT:\t\t{preview}\r\n\t{newlineCount} LINES");
                Console.WriteLine($"TEXT:\t\t{preview}\r\n\t{newlineCount} LINES");
                _thumbnailPictureBox.Visible = false; // Hide the thumbnail if it's text
                _textLabel.Text = text;
                _textLabel.Visible = true;
            }
            else if (item.Type == ClipboardItemType.Image)
            {
                Image image = (Image)item.Content;
                ShowNotification($"IMAGE:\t\t{image.Width}x{image.Height}, {image.PixelFormat}");
                Console.WriteLine($"IMAGE:\t\t{image.Width}x{image.Height}, {image.PixelFormat}");
                ShowImageThumbnail(image);
            }
        }


        private void LoadClipboardContent(int index)
        {
            ClipboardItem item = _clipboardHistory[index];
            if (item.Type == ClipboardItemType.Text)
            {
                Clipboard.SetText((string)item.Content);
            }
            else if (item.Type == ClipboardItemType.Image)
            {
                Clipboard.SetImage((Image)item.Content);
            }
            DisplayClipboardContent(item);
        }


        private void ShowImageThumbnail(Image image)
        {
            _thumbnailPictureBox.Image = image;
            _thumbnailPictureBox.Visible = true;
            _textLabel.Visible = false;
        }


        private void ShowNotification(string message)
        {
            NotifyIcon notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
                BalloonTipTitle = "Clipboard Monitor",
                BalloonTipText = message,
                Visible = true
            };
            notifyIcon.ShowBalloonTip(3000);
            var timer = new System.Windows.Forms.Timer { Interval = 3000 };

            timer.Tick += (s, e) => { notifyIcon.Dispose(); timer.Dispose(); };
            timer.Start();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }

    public enum ClipboardItemType
    {
        Text,
        Image
    }

    public class ClipboardItem
    {
        public ClipboardItemType Type { get; set; }
        public object Content { get; set; }
    }
}

