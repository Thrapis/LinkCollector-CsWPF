using CsQuery;
using NewSourceAdapter.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NewSourceAdapter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string DownloadFolder = "Downloads";
        private const string NO_SIZE_DATA = "NO SIZE DATA";

        private string DownloadFolderFull = Directory.GetCurrentDirectory() + "\\" + DownloadFolder;

        List<MatchDownloadLine> _linkList = new List<MatchDownloadLine>();
        HashSet<string> _approviesList = new HashSet<string>();

        string _lastPrefix;
        string _lastUrl;
        string _lastDownloadedUrl;
        string _lastPattern;
        CQ _lastCQ;

        bool _loadFileSizes;
        bool _loadingData;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                _approviesList = LocalStoreManager
                    .LoadApprovies().GetApproviesHashSet();
            }
            catch { }
            _loadingData = true;
            try
            {
                ApplicationState state = LocalStoreManager.LoadState();
                _lastPrefix = state.Prefix;
                _lastUrl = state.Url;
                _lastPattern = state.Pattern;
                PrefixField.Text = _lastPrefix;
                UrlField.Text = _lastUrl;
                PatternField.Text = _lastPattern;
            }
            catch { }
            _loadingData = false;

            DispatcherTimer LiveTime = new DispatcherTimer();
            LiveTime.Interval = TimeSpan.FromSeconds(1);
            LiveTime.Tick += (object sender, EventArgs e) =>
            { 
                LiveTimeLabel.Content = DateTime.Now.ToString("HH:mm:ss");
            };
            LiveTime.Start();
            LiveTimeLabel.Content = DateTime.Now.ToString("HH:mm:ss");
        }

        private void Log(string message, Brush brush)
        {
            TextBlock log = new TextBlock();
            log.Text = "--- " + message;
            log.Foreground = brush;
            log.TextWrapping = TextWrapping.Wrap;
            LogTable.Children.Add(log);
            LogTableScroll.ScrollToEnd();
        }

        private void Log(LogMessage logMessage) => Log(logMessage.Message, logMessage.Color);

        public string GetWebSourceSizeBytes(string uri)
        {
            var webRequest = HttpWebRequest.Create(uri);
            webRequest.Method = "HEAD";
            using (var webResponse = webRequest.GetResponse())
            {
                var fileSize = webResponse.Headers.Get("Content-Length");
                return fileSize;
            }
        }

        private void CheckUrlIndicator(string newUrl, string size = "")
        {
            if (newUrl == _lastDownloadedUrl)
            {
                LoadedIndicator.Background = Brushes.Green;
            }
            else
            {
                LoadedIndicator.Background = Brushes.Red;
            }
            if (size.Length > 0)
                LoadedIndicator.Text = size;
        }

        private bool IndicatorValue()
        {
            if (LoadedIndicator.Background == Brushes.Green)
            {
                return true;
            }
            return false;
        }

        public void RecheckApproviation()
        {
            for (int i = 0; i < _linkList.Count; i++)
            {
                var el = _linkList[i];
                if (_approviesList.Contains(el.Link))
                    el.Approved = true;
                else
                    el.Approved = false;
            }
        }

        public bool IsFilePath(string path) => new Uri(path).IsFile;

        // Event Handlers

        private void EventHandler_SomeChecked(bool newState)
        {
            double summarySize = 0;
            bool no_find = false;
            foreach (var el in _linkList)
            {
                if (el.FileSize != NO_SIZE_DATA && el.Checked)
                {
                    string str_num = el.FileSize.Substring(0,
                        el.FileSize.IndexOf(' '));
                    summarySize += Convert.ToDouble(str_num);
                }
                else if (el.Checked)
                    no_find = true;
            }
            string text = $"{Math.Round(summarySize, 2)}{(no_find ? "?" : "")} MiB";
            SummarySize.Text = text;
            SummarySize.ToolTip = text;
        }

        private void PrefixField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_loadingData)
            {
                string prefix = PrefixField.Text;
                _lastPrefix = prefix;
                for (int i = 0; i < _linkList.Count; i++)
                {
                    _linkList[i].Prefix = prefix;
                }
                ApplicationState state = new ApplicationState(_lastPrefix, _lastUrl, _lastPattern);
                LocalStoreManager.SaveState(state);
            }
        }

        private void UrlField_TextChanged(object sender, TextChangedEventArgs e)
        {
            string newUrl = UrlField.Text;
            CheckUrlIndicator(newUrl);
        }

        private void LoadPageButton_Click(object sender, RoutedEventArgs e)
        {
            string newUrl = UrlField.Text;
            string size = "";
            try
            {
                byte[] htmlBytes;
                if (IsFilePath(newUrl))
                {
                    htmlBytes = File.ReadAllBytes(newUrl);
                } 
                else
                {
                    using (WebClient client = new WebClient())
                    {
                        htmlBytes = client.DownloadData(newUrl);
                    }
                }
                _lastCQ = CQ.Create(new MemoryStream(htmlBytes));
                _lastUrl = newUrl;
                _lastDownloadedUrl = newUrl;

                int sourceSize = htmlBytes.Length;
                size = $"{sourceSize} B";
                Log($"Page loaded [{DateTime.Now}]", Brushes.White);
            }
            catch (Exception ex)
            {
                Log(ex.Message, Brushes.Yellow);
            }
            
            CheckUrlIndicator(newUrl, size);
            ApplicationState state = new ApplicationState(_lastPrefix, _lastUrl, _lastPattern);
            LocalStoreManager.SaveState(state);
        }

        private void UseLinkPatternButton_Click(object sender, RoutedEventArgs e)
        {
            if (IndicatorValue())
            {
                string pattern = PatternField.Text;
                _lastPattern = pattern;
                ApplicationState state = new ApplicationState(_lastPrefix, _lastUrl, _lastPattern);
                LocalStoreManager.SaveState(state);
                try
                {
                    _approviesList = LocalStoreManager
                        .LoadApprovies().GetApproviesHashSet();
                }
                catch { }
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;
                worker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    List<MatchDownloadLine> list = new List<MatchDownloadLine>();
                    string host = "";
                    if (IsFilePath(_lastUrl))
                    {
                        string comment = _lastCQ.Document.FirstChild.ToString();
                        Regex r = new Regex(@"(?<Protocol>\w+):\/\/(?<Domain>[\w@][\w.:@]+)\/?[\w\.?=%&=\-@/$,]*");
                        Match m = r.Match(comment);
                        host = m.Groups[0].Value;
                    }
                    else
                    {
                        host = new Uri(_lastUrl).GetLeftPart(UriPartial.Authority);
                    }

                    Uri baseUri = new Uri(host);
                    CQ aResult = _lastCQ.Find("a");
                    for (int i = 0; i < aResult.Count(); i++) 
                    {
                        IDomObject obj = aResult.Get(i);
                        string href = WebUtility.UrlDecode(obj.GetAttribute("href"));
                        if (href != null && href.Contains(pattern))
                        {
                            var uri = new Uri(baseUri, href);
                            string filename = System.IO.Path.GetFileName(uri.LocalPath);
                            string text = obj.Cq().Text();
                            if (obj.Cq().Find("img").Count() > 0)
                            {
                                text = $"[image] {text}";
                            }

                            string endFileSize = NO_SIZE_DATA;
                            if (_loadFileSizes)
                            {
                                try
                                {
                                    var fileSize = GetWebSourceSizeBytes(uri.AbsoluteUri);
                                    var fileSizeInMegaByte = Math.Round(Convert.ToDouble(fileSize) / 1024.0 / 1024.0, 2);
                                    endFileSize = fileSizeInMegaByte + " MiB";
                                }
                                catch (Exception ex)
                                {
                                    (sender as BackgroundWorker).ReportProgress(i * 100 / aResult.Count(),
                                        new LogMessage($"Exception [{uri}]:\t{ex.Message}", Brushes.Pink));
                                    
                                }
                            }
                            bool approved = _approviesList.Contains(uri.ToString());
                            var el = new MatchDownloadLine(list.Count, approved, uri.ToString(),
                                endFileSize, text, filename, false, _lastPrefix);
                            list.Add(el);
                        }
                        (sender as BackgroundWorker).ReportProgress(i * 100 / aResult.Count());
                    }
                    e.Result = list;
                };
                worker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    UseLinkPatterProgressbar.Value = e.ProgressPercentage;
                    LogMessage logMessage = (LogMessage)e.UserState;
                    if (logMessage != null)
                        Log(logMessage);
                };
                worker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    _linkList = (List<MatchDownloadLine>)e.Result;
                    DownloadTable.Children.Clear();
                    for (int i = 0; i < _linkList.Count; i++)
                    {
                        var el = _linkList.ElementAt(i);
                        el.CheckChanged += EventHandler_SomeChecked;
                        DownloadTable.Children.Add(el.GetVisualisation());
                    }
                    EventHandler_SomeChecked(true);
                    UseLinkPatterProgressbar.Value = 0;
                    Log($"Link pattern applied [{DateTime.Now}]", Brushes.White);
                };
                worker.RunWorkerAsync();
            }
        }

        private void UncheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _linkList.Count; i++)
            {
                _linkList[i].Checked = false;
            }
        }

        private void CheckAllButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _linkList.Count; i++)
            {
                _linkList[i].Checked = true;
            }
        }

        private void DownloadFilesButton_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(DownloadFolderFull) && MessageBox.Show("Do you want to clear download folder?", "Question",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                foreach (FileInfo file in new DirectoryInfo(DownloadFolderFull).GetFiles())
                {
                    file.Delete();
                }
            }

            if (!Directory.Exists(DownloadFolderFull))
            {
                Directory.CreateDirectory(DownloadFolderFull);
            }

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                var hashSet = new HashSet<string>();
                foreach (var el in _linkList)
                {
                    if (el.Checked)
                        hashSet.Add(el.Link);
                }
                using (var client = new WebClient() { BaseAddress = _lastUrl })
                {
                    for (int i = 0; i < hashSet.Count; i++)
                    {
                        string href = hashSet.ElementAt(i);
                        Uri uri = new Uri(href);
                        string filename = System.IO.Path.GetFileName(uri.LocalPath);
                        try
                        {
                            if (File.Exists(filename))
                            {
                                string subFolder = uri.Segments[uri.Segments.Length-1];
                                string subFolderPath = $"{DownloadFolderFull}\\{subFolder}";
                                if (!Directory.Exists(subFolderPath))
                                {
                                    Directory.CreateDirectory(subFolderPath);
                                }
                                client.DownloadFile(href, $"{subFolderPath}\\{filename}");
                            }
                            else
                            {
                                client.DownloadFile(href, $"{DownloadFolderFull}\\{filename}");
                            }
                            (sender as BackgroundWorker).ReportProgress(i * 100 / hashSet.Count);
                        }
                        catch (Exception ex)
                        {
                            (sender as BackgroundWorker).ReportProgress(i * 100 / hashSet.Count,
                                new LogMessage($"Exception [{href}]:\t{ex.Message}", Brushes.Red));
                        }
                    }
                }
            (sender as BackgroundWorker).ReportProgress(0, new LogMessage("Download complete", Brushes.White));
            };

            worker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
            {
                DownloadProgressBar.Value = e.ProgressPercentage;
                LogMessage logMessage = (LogMessage)e.UserState;
                if (logMessage != null)
                    Log(logMessage);
            };

            worker.RunWorkerAsync();
        }

        private void LoadSizes_Checked(object sender, RoutedEventArgs e)
        {
            _loadFileSizes = (bool)LoadSizes.IsChecked;
        }

        private void LoadSizes_Unchecked(object sender, RoutedEventArgs e)
        {
            _loadFileSizes = (bool)LoadSizes.IsChecked;
        }

        private void OpenAllSelectedLinksButton_Click(object sender, RoutedEventArgs e)
        {
            var hashSet = new HashSet<string>();
            foreach (var el in _linkList)
            {
                if (el.Checked)
                    hashSet.Add(el.Link);
            }
            for (int i = 0; i < hashSet.Count; i++)
            {
                string href = hashSet.ElementAt(i);
                Process.Start(new ProcessStartInfo
                {
                    FileName = href,
                    UseShellExecute = true
                });
            }
        }

        private void ALButton_Click(object sender, RoutedEventArgs e)
        {
            int selected = _linkList.Count(e => e.Checked);
            int will_be_approved = _linkList.Count(e => e.Checked && !e.Approved);
            if (MessageBox.Show($"Do you really want to APPROVE all selected links [{selected}]? " +
                $"Then will be approved additionally {will_be_approved} links.", "Question",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                for (int i = 0; i < _linkList.Count; i++)
                {
                    var el = _linkList[i];
                    if (el.Checked)
                    {
                        _approviesList.Add(el.Link);
                    }
                }
                var asc = new ApproviesSaveCard(_approviesList);
                LocalStoreManager.SaveApprovies(asc);
                RecheckApproviation();
            }
        }

        private void DLButton_Click(object sender, RoutedEventArgs e)
        {
            int selected = _linkList.Count(e => e.Checked);
            int will_be_disapproved = _linkList.Count(e => e.Checked && e.Approved);
            if (MessageBox.Show($"Do you really want to DISAPPROVE all selected links [{selected}]? " +
                $"Then will be disapproved additionally {will_be_disapproved} links.", "Question",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                for (int i = 0; i < _linkList.Count; i++)
                {
                    var el = _linkList[i];
                    if (el.Checked)
                    {
                        _approviesList.Remove(el.Link);
                    }
                }
                var asc = new ApproviesSaveCard(_approviesList);
                LocalStoreManager.SaveApprovies(asc);
                RecheckApproviation();
            }
        }
    }
}
