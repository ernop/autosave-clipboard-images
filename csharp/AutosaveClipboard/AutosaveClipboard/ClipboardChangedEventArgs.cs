using System;

namespace ClipboardMonitor
{
    public class ClipboardChangedEventArgs : EventArgs
    {
        public ClipboardItem ClipboardItem { get; set; }
        public int ClipboardIndex { get; set; }
        public string Resolution { get; set; }
        public DateTime AddedTime { get; set; }
        public string OriginalFilename { get; set; }
    }
}
