using System;
using System.Drawing;
using System.Windows.Forms;

namespace ClipboardMonitor
{
    public class ThumbnailsBarUpdater
    {
        private Panel _thumbnailsPanel;
        private ClipboardManager _clipboardManager;
        private NavigationManager _navigationManager;
        private Panel _pastPanel;
        private Panel _selectedPanel;
        private Panel _futurePanel;
        private int SingleThumbnailWidth = 300;  // Fixed width for selected thumbnail

        public ThumbnailsBarUpdater(Panel thumbnailsPanel, ClipboardManager clipboardManager, NavigationManager navigationManager)
        {
            _thumbnailsPanel = thumbnailsPanel;
            _clipboardManager = clipboardManager;
            _navigationManager = navigationManager;

            _thumbnailsPanel.Resize += ThumbnailsPanel_Resize;

            _pastPanel = new Panel { Dock = DockStyle.Left, BackColor = Color.LightGray };
            _selectedPanel = new Panel { Dock = DockStyle.Left, Width = SingleThumbnailWidth, BackColor = Color.LightGray };
            _futurePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.LightGray };

            _thumbnailsPanel.Controls.Add(_futurePanel);
            _thumbnailsPanel.Controls.Add(_selectedPanel);
            _thumbnailsPanel.Controls.Add(_pastPanel);

            ThumbnailsPanel_Resize(null, EventArgs.Empty);
            
        }

        private void ThumbnailsPanel_Resize(object sender, EventArgs e)
        {
            AdjustPanelWidths();
        }

        private void AdjustPanelWidths()
        {
            int totalWidth = _thumbnailsPanel.Width;
            int sidePanelWidth = (totalWidth - SingleThumbnailWidth) / 2;
            _pastPanel.Width = sidePanelWidth;
            _futurePanel.Width = sidePanelWidth;  // Adjust dynamically based on total width
        }

        public void UpdateThumbnailsBar()
        {
            int currentIndex = _clipboardManager.ClipboardIndex;

            _pastPanel.Controls.Clear();
            _selectedPanel.Controls.Clear();
            _futurePanel.Controls.Clear();

            if (currentIndex == -1) return;  // Handle no selection

            var itemsToShow = 14;

            for (var ii = currentIndex - itemsToShow; ii <= currentIndex + itemsToShow; ii++)
            {
                if (ii < 0 || ii >= _clipboardManager.ClipboardHistory.Count) continue;

                var item = _clipboardManager.ClipboardHistory[ii];
                var thumbnail = CreateThumbnail(item, ii == currentIndex, ii);
                thumbnail.Dock = (ii < currentIndex) ? DockStyle.Right : DockStyle.Left;

                if (ii < currentIndex)
                {
                    _pastPanel.Controls.Add(thumbnail);
                }
                else if (ii == currentIndex)
                {
                    _selectedPanel.Controls.Add(thumbnail);
                    thumbnail.Dock = DockStyle.Fill; // Ensure it fills the middle panel
                }
                else
                {
                    _futurePanel.Controls.Add(thumbnail);
                }
            }
            AdjustPanelWidths();
        }

        

        private Panel CreateThumbnail(ClipboardItem item, bool isSelected, int clipboardItemIndex)
        {
            Console.WriteLine("CreateThumbnail");
            EventHandler clickHandler = (sender, e) => {
                // Get the parent panel if the sender is a child control
                Console.WriteLine("Clickhandle.");
                var control = sender as Control;
                while (!(control is Panel))
                {
                    Console.WriteLine("WAIT>");
                    control = control.Parent;
                }
                int itemIndex = (int)control.Tag;
                _navigationManager.JumpToClipboardItem(itemIndex);
            };

            var thumbnail = new Panel
            {
                Width = SingleThumbnailWidth,
                Height = _thumbnailsPanel.Height,
                BorderStyle = isSelected ? BorderStyle.Fixed3D : BorderStyle.FixedSingle,
                BackColor = isSelected ? Color.LightBlue : Color.White,
                Padding = new Padding(10),
                Tag = clipboardItemIndex  // Store the index in the Tag property
            };

            thumbnail.Click += clickHandler;

            // Check the type of the clipboard item and add appropriate control
            if (item.Type == ClipboardItemType.Text)
            {
                var label = new Label
                {
                    Text = item.Content.ToString().Length > 20 ? item.Content.ToString().Substring(0, 20) + "..." : item.Content.ToString(),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoSize = false,
                    BackColor = Color.White
                };
                label.Click += clickHandler;
                thumbnail.Controls.Add(label);
            }
            else if (item.Type == ClipboardItemType.Image)
            {
                var pictureBox = new PictureBox
                {
                    Image = (Image)item.Content,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Dock = DockStyle.Fill,
                    BackColor = Color.Black
                };
                pictureBox.Click += clickHandler;
                thumbnail.Controls.Add(pictureBox);
            }

            return thumbnail;
        }
    }
}