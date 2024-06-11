using System;

namespace ClipboardMonitor
{
    public class ClipboardItem
    {
        public ClipboardItemType Type { get; set; }
        public object Content { get; set; }
        public DateTime AddedTime { get; set; }
        public string OriginalFilename { get; set; }
    }
}