using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NewSourceAdapter.Models
{
    class MatchDownloadLine
    {
        private bool _approved;
        private bool _checked;

        public int Id { get; set; }
        public bool Approved
        {
            get
            {
                return _approved;
            }
            set
            {
                _approved = value;
                ApproveChanged?.Invoke(_approved);
            }
        }
        public string Link { get; set; }
        public string FileSize { get; set; }
        public string HtmlText { get; set; }
        public string DownloadName { get; set; }
        public string DownloadNameLowerCase { 
            get {
                return DownloadName.ToLower();
            }
        }
        public bool Checked {
            get
            {
                return _checked;
            }
            set
            {
                _checked = value;
                CheckChanged?.Invoke(_checked);
            } 
        }
        public string Prefix { get; set; }
        public string FormattedNewLink
        {
            get
            {
                string name = Path.GetFileNameWithoutExtension(DownloadName);
                name = name.Replace("_", "-");
                name = name.Replace(" ", "-");
                bool changed;
                do
                {
                    changed = false;
                    if (name.Contains("--"))
                    {
                        name = name.Replace("--", "-");
                        changed = true;
                    }
                } while (changed);
                int i = 0;
                while (name[i] == '-')
                {
                    name = name.Remove(i, 1);
                }
                i = name.Length - 1;
                while (name[i] == '-')
                {
                    name = name.Remove(i, 1);
                    i = name.Length - 1;
                }
                string ext = Path.GetExtension(DownloadName);
                return Prefix + (name + ext).ToLower();
            }
        }

        public delegate void ApproveChangedDelegate(bool newState);
        public event ApproveChangedDelegate ApproveChanged;
        public delegate void CheckChangedDelegate(bool newState);
        public event CheckChangedDelegate CheckChanged;

        public MatchDownloadLine(int id, bool approved, string link, string fileSize, string htmlText, string downloadName, bool @checked, string prefix = "")
        {
            Id = id;
            _approved = approved;
            Link = link;
            FileSize = fileSize;
            HtmlText = htmlText;
            DownloadName = downloadName;
            _checked = @checked;
            Prefix = prefix;
        }

        public Border GetVisualisation()
        {
            Grid outer = new Grid();
            ColumnDefinition id_col = new ColumnDefinition();
            id_col.Width = new GridLength(0.3, GridUnitType.Star);
            ColumnDefinition approve_col = new ColumnDefinition();
            approve_col.Width = new GridLength(0.3, GridUnitType.Star);
            ColumnDefinition link_col = new ColumnDefinition();
            link_col.Width = new GridLength(1.5, GridUnitType.Star);
            ColumnDefinition size_col = new ColumnDefinition();
            size_col.Width = new GridLength(0.8, GridUnitType.Star);
            ColumnDefinition text_col = new ColumnDefinition();
            text_col.Width = new GridLength(2, GridUnitType.Star);
            ColumnDefinition dnlc_col = new ColumnDefinition();
            dnlc_col.Width = new GridLength(2, GridUnitType.Star);
            ColumnDefinition dnlc_copy_col = new ColumnDefinition();
            dnlc_copy_col.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition c_col = new ColumnDefinition();
            c_col.Width = new GridLength(0.5, GridUnitType.Star);


            outer.ColumnDefinitions.Add(id_col);
            outer.ColumnDefinitions.Add(approve_col);
            outer.ColumnDefinitions.Add(link_col);
            outer.ColumnDefinitions.Add(size_col);
            outer.ColumnDefinitions.Add(text_col);
            outer.ColumnDefinitions.Add(dnlc_col);
            outer.ColumnDefinitions.Add(dnlc_copy_col);
            outer.ColumnDefinitions.Add(c_col);
            
            TextBlock id_block = new TextBlock();
            id_block.SetValue(Grid.ColumnProperty, 0);
            id_block.Text = Id.ToString();
            id_block.TextAlignment = TextAlignment.Center;

            Image approve_block = new Image();
            approve_block.Height = 20;
            if (Approved)
            {
                approve_block.Source = (ImageSource)new ImageSourceConverter()
                    .ConvertFromString("Images/checked.png");
                approve_block.ToolTip = "Approved";
            }
            else
            {
                approve_block.Source = (ImageSource)new ImageSourceConverter()
                    .ConvertFromString("Images/cross.png");
                approve_block.ToolTip = "Not approved";
            }
            ApproveChanged += (bool b) =>
            {
                if (Approved)
                {
                    approve_block.Source = (ImageSource)new ImageSourceConverter()
                        .ConvertFromString("Images/checked.png");
                    approve_block.ToolTip = "Approved";
                }
                else
                {
                    approve_block.Source = (ImageSource)new ImageSourceConverter()
                        .ConvertFromString("Images/cross.png");
                    approve_block.ToolTip = "Not approved";
                }
            };

            Border border0i = new Border();
            border0i.SetValue(Grid.ColumnProperty, 1);
            border0i.BorderBrush = Brushes.Black;
            border0i.BorderThickness = new Thickness(0.2);
            border0i.Child = approve_block;

            TextBlock link_block = new TextBlock();
            link_block.Margin = new Thickness(2, 2, 2, 2);
            link_block.Text = Link;
            link_block.Foreground = Brushes.BlueViolet;
            link_block.Cursor = Cursors.Hand;
            link_block.ToolTip = Link;
            link_block.TextAlignment = TextAlignment.Right;
            link_block.HorizontalAlignment = HorizontalAlignment.Right;
            Border border0 = new Border();
            border0.SetValue(Grid.ColumnProperty, 2);
            border0.BorderBrush = Brushes.Black;
            border0.BorderThickness = new Thickness(0.2);
            border0.Child = link_block;
            link_block.MouseLeftButtonUp += (object sender, MouseButtonEventArgs mbea) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Link,
                    UseShellExecute = true
                });
            };
            

            TextBlock size_block = new TextBlock();
            size_block.Margin = new Thickness(2, 2, 2, 2);
            size_block.Text = FileSize;
            size_block.ToolTip = FileSize;
            Border border0s = new Border();
            border0s.SetValue(Grid.ColumnProperty, 3);
            border0s.BorderBrush = Brushes.Black;
            border0s.BorderThickness = new Thickness(0.2);
            border0s.Child = size_block;

            TextBlock text_block = new TextBlock();
            text_block.Margin = new Thickness(2, 2, 2, 2);
            text_block.Text = HtmlText;
            text_block.ToolTip = HtmlText;
            text_block.SetValue(Grid.ColumnProperty, 0);
            Button txt_btn = new Button();
            txt_btn.Content = "copy";
            txt_btn.Click += (object sender, RoutedEventArgs rea) =>
            {
                Clipboard.SetText(HtmlText);
            };
            txt_btn.SetValue(Grid.ColumnProperty, 1);
            Border border1 = new Border();
            border1.SetValue(Grid.ColumnProperty, 4);
            border1.BorderBrush = Brushes.Black;
            border1.BorderThickness = new Thickness(0.2);
            Grid tb_grid = new Grid();
            ColumnDefinition txt_col = new ColumnDefinition();
            txt_col.Width = new GridLength(1, GridUnitType.Star);
            ColumnDefinition txt_copy_col = new ColumnDefinition();
            txt_copy_col.Width = new GridLength(30, GridUnitType.Pixel);
            tb_grid.ColumnDefinitions.Add(txt_col);
            tb_grid.ColumnDefinitions.Add(txt_copy_col);
            tb_grid.Children.Add(text_block);
            tb_grid.Children.Add(txt_btn);
            border1.Child = tb_grid;


            TextBlock dnlc_block = new TextBlock();
            dnlc_block.Margin = new Thickness(2, 2, 2, 2);
            dnlc_block.Text = DownloadName;
            dnlc_block.ToolTip = DownloadName;
            Border border2 = new Border();
            border2.SetValue(Grid.ColumnProperty, 5);
            border2.BorderBrush = Brushes.Black;
            border2.BorderThickness = new Thickness(0.2);
            border2.Child = dnlc_block;

            Button dnlc_copy_btn = new Button();
            dnlc_copy_btn.Margin = new Thickness(4,0,4,0);
            dnlc_copy_btn.SetValue(Grid.ColumnProperty, 6);
            dnlc_copy_btn.Content = "Copy new path";
            //dnlc_copy_btn.ToolTip = FormattedNewLink;
            dnlc_copy_btn.Click += (object sender, RoutedEventArgs rea) =>
            {
                Clipboard.SetText(FormattedNewLink);
            };

            CheckBox c_cb = new CheckBox();
            dnlc_copy_btn.Margin = new Thickness(6, 0, 6, 0);
            c_cb.SetValue(Grid.ColumnProperty, 7);
            c_cb.Content = "Select";
            c_cb.IsChecked = Checked;
            //c_cb.IsEnabled = false;
            c_cb.Click += (object sender, RoutedEventArgs rea) =>
            {
                Checked = (bool)c_cb.IsChecked;
            };
            CheckChanged += (bool b) =>
            {
                c_cb.IsChecked = b;
                if (b)
                    outer.Background = Brushes.LightGreen;
                else
                    outer.Background = Brushes.Transparent;
            };

            outer.Children.Add(id_block);
            outer.Children.Add(border0i);
            outer.Children.Add(border0);
            outer.Children.Add(border0s);
            outer.Children.Add(border1);
            outer.Children.Add(border2);
            outer.Children.Add(dnlc_copy_btn);
            outer.Children.Add(c_cb);

            outer.PreviewMouseLeftButtonUp += (object sender, MouseButtonEventArgs mbea) =>
            {
                if (mbea.Source != dnlc_copy_btn && mbea.Source != txt_btn
                    && mbea.Source != c_cb && mbea.Source != link_block)
                {
                    Checked = !Checked;
                }  
            };

            Border border = new Border();
            border.BorderBrush = Brushes.Black;
            border.BorderThickness = new Thickness(0.5);
            border.Child = outer;

            border.MouseEnter += (object sender, MouseEventArgs e) =>
            {
                border.Background = Brushes.LightGray;
            };
            border.MouseLeave += (object sender, MouseEventArgs e) =>
            {
                border.Background = Brushes.White;
            };

            return border;
        }
    }
}
