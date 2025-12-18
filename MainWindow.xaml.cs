#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DownloadsOrganizer
{
    public partial class MainWindow : Window
    {
        string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Dictionary<string, List<FileInfo>> fileCategories = new Dictionary<string, List<FileInfo>>();

        bool isDarkMode = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += async (s, e) => await ScanFilesAsync();
        }

        private async Task ScanFilesAsync()
        {
            LoadingGrid.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                // Listeleri sıfırla
                fileCategories["Compressed"] = new List<FileInfo>();
                fileCategories["Images"] = new List<FileInfo>();
                fileCategories["Docs"] = new List<FileInfo>();
                fileCategories["Media"] = new List<FileInfo>();
                fileCategories["Executables"] = new List<FileInfo>();
                fileCategories["Others"] = new List<FileInfo>();

                // --- GENİŞLETİLMİŞ DOSYA TÜRÜ VERİTABANI ---
                // HashSet kullanıyoruz çünkü içinde arama yapmak if-else'den 100 kat hızlıdır.

                var extCompressed = new HashSet<string> {
                    ".zip", ".rar", ".7z", ".tar", ".gz", ".iso", ".tgz", ".bz2", ".xz", ".lz", ".z",
                    ".cab", ".wim", ".deb", ".rpm", ".pkg", ".dmg", ".vhd", ".vhdx", ".toast", ".sitx"
                };

                var extImages = new HashSet<string> {
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico",
                    ".tif", ".tiff", ".heic", ".heif", ".raw", ".cr2", ".nef", ".arw", ".dng",
                    ".psd", ".ai", ".eps", ".xcf", ".jxl", ".avif", ".ind", ".indd"
                };

                var extDocs = new HashSet<string> {
                    ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".odt", ".ods", ".odp",
                    ".csv", ".md", ".log", ".ini", ".cfg", ".json", ".xml", ".yaml", ".epub", ".mobi", ".azw3",
                    ".c", ".cpp", ".cs", ".py", ".java", ".js", ".html", ".css", ".php", ".sql" // Kod dosyalarını da buraya aldım
                };

                var extMedia = new HashSet<string> { 
                    // Video
                    ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".3gp", ".ts", ".mts", ".vob", ".ogv",
                    // Ses
                    ".mp3", ".wav", ".flac", ".ogg", ".wma", ".m4a", ".aac", ".alac", ".aiff", ".mid", ".midi", ".opus"
                };

                var extExecutables = new HashSet<string> {
                    ".exe", ".msi", ".bat", ".cmd", ".sh", ".vbs", ".ps1", ".jar", ".apk", ".appx", ".msix",
                    ".com", ".gadget", ".bin", ".scr", ".pif"
                };

                if (Directory.Exists(downloadsPath))
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(downloadsPath);
                    try
                    {
                        foreach (var file in dirInfo.GetFiles())
                        {
                            string ext = file.Extension.ToLower();

                            if (extCompressed.Contains(ext))
                                fileCategories["Compressed"].Add(file);
                            else if (extImages.Contains(ext))
                                fileCategories["Images"].Add(file);
                            else if (extDocs.Contains(ext))
                                fileCategories["Docs"].Add(file);
                            else if (extMedia.Contains(ext))
                                fileCategories["Media"].Add(file);
                            else if (extExecutables.Contains(ext))
                                fileCategories["Executables"].Add(file);
                            else
                                fileCategories["Others"].Add(file); // Torrent dosyaları vs. buraya düşer
                        }
                    }
                    catch { /* Erişim hatası vs. olursa geç */ }
                }
            });

            ApplySortingAndDisplay();
            LoadingGrid.Visibility = Visibility.Collapsed;
        }

        private void ApplySortingAndDisplay()
        {
            int sortIndex = cbSort.SelectedIndex;

            foreach (var key in fileCategories.Keys.ToList())
            {
                var list = fileCategories[key];
                switch (sortIndex)
                {
                    case 0: list = list.OrderByDescending(f => f.LastWriteTime).ToList(); break;
                    case 1: list = list.OrderBy(f => f.LastWriteTime).ToList(); break;
                    case 2: list = list.OrderBy(f => f.Name).ToList(); break;
                    case 3: list = list.OrderByDescending(f => f.Length).ToList(); break;
                }
                fileCategories[key] = list;
            }

            UpdateUI(listCompressed, lblTitleCompressed, "📦 SIKIŞTIRILMIŞ", fileCategories["Compressed"]);
            UpdateUI(listImages, lblTitleImages, "🖼️ GÖRSELLER", fileCategories["Images"]);
            UpdateUI(listDocs, lblTitleDocs, "📄 BELGELER", fileCategories["Docs"]);
            UpdateUI(listMedia, lblTitleMedia, "🎵 MEDYA", fileCategories["Media"]);
            UpdateUI(listExecutables, lblTitleExecutables, "🚀 UYGULAMALAR", fileCategories["Executables"]);
            UpdateUI(listOthers, lblTitleOthers, "📁 DİĞER", fileCategories["Others"]);

            lblTotalCount.Text = $"Toplam Dosya: {fileCategories.Values.Sum(x => x.Count)}";
        }

        private void UpdateUI(ListBox lb, TextBlock titleBlock, string titleText, List<FileInfo> files)
        {
            lb.ItemsSource = null;
            lb.ItemsSource = files;
            titleBlock.Text = $"{titleText} ({files.Count})";
        }

        // --- OLAYLAR ---

        private void CbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fileCategories.Count > 0) ApplySortingAndDisplay();
        }

        private void OpenCategory_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string key = btn.Tag.ToString();
            string title = key switch
            {
                "Compressed" => "Sıkıştırılmış Dosyalar",
                "Images" => "Görseller",
                "Docs" => "Belgeler",
                "Media" => "Medya",
                "Executables" => "Uygulamalar",
                _ => "Diğer Dosyalar"
            };

            CategoryWindow detailWin = new CategoryWindow(title, fileCategories[key]);
            detailWin.Show();
        }

        // --- PENCERE KONTROLLERİ ---
        private void TitleBar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // --- TEMA ---
        private void BtnTheme_Click(object sender, RoutedEventArgs e)
        {
            isDarkMode = !isDarkMode;

            if (isDarkMode)
            {
                // DARK (Lossless Style)
                App.Current.Resources["MainBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#181818"));
                App.Current.Resources["CardBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#242424"));
                App.Current.Resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                App.Current.Resources["SubTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAAAAA"));
                App.Current.Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"));
            }
            else
            {
                // LIGHT
                App.Current.Resources["MainBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F3F3"));
                App.Current.Resources["CardBackground"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                App.Current.Resources["TextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
                App.Current.Resources["SubTextBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                App.Current.Resources["BorderBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            }
        }

        // --- LİSTE ETKİLEŞİMLERİ ---
        private void MainList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => OpenFileFromList(sender);
        private void Context_Open_Click(object sender, RoutedEventArgs e) => OpenFileFromMenu(sender);
        private void Context_ShowInFolder_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFileFromMenu(sender);
            if (file != null) System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{file.FullName}\"");
        }

        private void OpenFileFromList(object sender)
        {
            if ((sender as ListBox)?.SelectedItem is FileInfo file)
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.FullName) { UseShellExecute = true }); } catch { }
        }

        private void OpenFileFromMenu(object sender)
        {
            var file = GetSelectedFileFromMenu(sender);
            if (file != null)
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file.FullName) { UseShellExecute = true }); } catch { }
        }

        private FileInfo GetSelectedFileFromMenu(object sender)
        {
            try
            {
                var menuItem = sender as MenuItem;
                var contextMenu = menuItem.Parent as ContextMenu;
                var listBox = contextMenu.PlacementTarget as ListBox;
                return listBox.SelectedItem as FileInfo;
            }
            catch { return null; }
        }
    }
}