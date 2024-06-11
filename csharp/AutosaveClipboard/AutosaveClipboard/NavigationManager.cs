using System;
using System.Windows.Forms;
using System.Drawing;

namespace ClipboardMonitor
{
    public class NavigationManager
    {
        private ClipboardManager _clipboardManager;
        public event EventHandler<ClipboardChangedEventArgs> ClipboardChanged;

        protected virtual void OnClipboardChanged(ClipboardChangedEventArgs e)
        {
            ClipboardChanged?.Invoke(this, e);
        }

        public NavigationManager(ClipboardMonitorForm form, ClipboardManager clipboardManager)
        {
            _clipboardManager = clipboardManager;
        }

        public void LoadPreviousClipboardItem()
        {
            if (_clipboardManager.ClipboardIndex > 0)
            {
                _clipboardManager.ClipboardIndex--;
                LoadClipboardContent(_clipboardManager.ClipboardIndex);
                Console.WriteLine("Loaded previous clipboard item");
            }
        }

        public void LoadNextClipboardItem()
        {
            if (_clipboardManager.ClipboardIndex < _clipboardManager.ClipboardHistory.Count - 1)
            {
                _clipboardManager.ClipboardIndex++;
                LoadClipboardContent(_clipboardManager.ClipboardIndex);
                Console.WriteLine("Loaded next clipboard item");
            }
        }

        private void LoadClipboardContent(int index)
        {
            var item = _clipboardManager.ClipboardHistory[index];
            if (item.Type == ClipboardItemType.Text)
            {
                Clipboard.SetText((string)item.Content);
            }
            else if (item.Type == ClipboardItemType.Image)
            {
                Clipboard.SetImage((Image)item.Content);
            }

            OnClipboardChanged(new ClipboardChangedEventArgs
            {
                ClipboardItem = item,
                ClipboardIndex = index,
                AddedTime = item.AddedTime,
                Resolution = item.Type == ClipboardItemType.Image ? $"{((Image)item.Content).Width}x{((Image)item.Content).Height}" : null,
                OriginalFilename = item.OriginalFilename
            });
        }
    }
}
