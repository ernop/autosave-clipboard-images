using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace ClipboardMonitor
{
    public class ClipboardManager
    {
        private List<ClipboardItem> _clipboardHistory;
        private int _clipboardIndex;
        private HashSet<string> _seenHashes;
        private string _folderPath = "C:\\Screenshots";
        public event EventHandler<ClipboardChangedEventArgs> ClipboardChanged;

        private string _lastSavedImageFileName;

        public int ClipboardIndex
        {
            get => _clipboardIndex;
            set => _clipboardIndex = value;
        }

        public List<ClipboardItem> ClipboardHistory => _clipboardHistory;
        public ClipboardManager()
        {
            _clipboardHistory = new List<ClipboardItem>();
            _clipboardIndex = -1;
            _seenHashes = new HashSet<string>();

            Directory.CreateDirectory(_folderPath);

            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1 second
            timer.Tick += Timer_Tick;
            timer.Start();
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
                    var item = new ClipboardItem { Type = ClipboardItemType.Text, Content = text, AddedTime = DateTime.Now };
                    _clipboardHistory.Add(item);
                    _clipboardIndex = _clipboardHistory.Count - 1;

                    OnClipboardChanged(new ClipboardChangedEventArgs
                    {
                        ClipboardItem = item,
                        ClipboardIndex = _clipboardIndex,
                        AddedTime = DateTime.Now
                    });
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
                    var item = new ClipboardItem { Type = ClipboardItemType.Image, Content = image, AddedTime = DateTime.Now, OriginalFilename = _lastSavedImageFileName };
                    _clipboardHistory.Add(item);
                    _clipboardIndex = _clipboardHistory.Count - 1;

                    OnClipboardChanged(new ClipboardChangedEventArgs
                    {
                        ClipboardItem = item,
                        ClipboardIndex = _clipboardIndex,
                        Resolution = $"{image.Width}x{image.Height}",
                        AddedTime = DateTime.Now,
                        OriginalFilename = _lastSavedImageFileName
                    });
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
            _lastSavedImageFileName = Path.Combine(_folderPath, $"Screenshot_{DateTime.Now:yyyyMMddHHmmss}.png");
            image.Save(_lastSavedImageFileName);
            Console.WriteLine($"Saved image: {_lastSavedImageFileName}");
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

        protected virtual void OnClipboardChanged(ClipboardChangedEventArgs e)
        {
            ClipboardChanged?.Invoke(this, e);
        }
    }
}
