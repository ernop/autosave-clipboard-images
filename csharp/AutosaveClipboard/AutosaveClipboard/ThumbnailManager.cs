using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClipboardMonitor
{
    public class ThumbnailManager
    {
        private ClipboardMonitorForm _form;

        public ThumbnailManager(ClipboardMonitorForm form)
        {
            _form = form;
        }

        public void ShowImageThumbnail(Image image, DateTime addedTime, string originalFilename)
        {
            _form.ThumbnailPictureBox.Image = image;
            _form.ThumbnailPictureBox.Visible = true;
            _form.TextLabel.Visible = false;
            var resolution = $"{image.Width}x{image.Height}";
            _form._statusBarManager.UpdateStatusBar(_form.ClipboardIndex, resolution, addedTime, originalFilename);
        }
    }
}
