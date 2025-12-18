#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DownloadsOrganizer
{
    public partial class CategoryWindow : Window
    {
        // Artık sadece isim değil, tüm dosya bilgisini tutuyoruz
        private List<FileInfo> _allFiles;
        private List<FileInfo> _currentFilteredFiles; // Arama ve sıralama sonucu

        // Constructor değişti: List<string> yerine List<FileInfo> alıyor
        public CategoryWindow(string categoryName, List<FileInfo> files)
        {
            InitializeComponent();
            this.Title = categoryName;

            // Listeyi kopyalıyoruz ki ana pencereyle karışmasın
            _allFiles = new List<FileInfo>(files);
            _currentFilteredFiles = new List<FileInfo>(files);

            // İlk açılışta varsayılan sıralama (Tarih Yeni)
            ApplyFilterAndSort();
        }

        // --- SIRALAMA VE FİLTRELEME MANTIĞI ---
        private void ApplyFilterAndSort()
        {
            string query = txtSearch.Text.ToLower();
            int sortIndex = cbSort.SelectedIndex;

            // 1. Önce Arama Yap
            var tempFiles = _allFiles.Where(f => f.Name.ToLower().Contains(query));

            // 2. Sonra Sırala
            switch (sortIndex)
            {
                case 0: // Tarih (En Yeni)
                    tempFiles = tempFiles.OrderByDescending(f => f.LastWriteTime);
                    break;
                case 1: // Tarih (En Eski)
                    tempFiles = tempFiles.OrderBy(f => f.LastWriteTime);
                    break;
                case 2: // İsim (A-Z)
                    tempFiles = tempFiles.OrderBy(f => f.Name);
                    break;
                case 3: // Boyut
                    tempFiles = tempFiles.OrderByDescending(f => f.Length);
                    break;
            }

            _currentFilteredFiles = tempFiles.ToList();
            lbFiles.ItemsSource = _currentFilteredFiles;
        }

        // Arama değişince
        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilterAndSort();
        }

        // Sıralama değişince
        private void CbSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allFiles != null) ApplyFilterAndSort();
        }

        // --- ETKİLEŞİMLER ---
        private void LbFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MenuOpen_Click(sender, null);
        }

        private FileInfo GetSelectedFile()
        {
            return lbFiles.SelectedItem as FileInfo;
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFile();
            if (file != null)
                try { Process.Start(new ProcessStartInfo(file.FullName) { UseShellExecute = true }); } catch { }
        }

        private void MenuShowInFolder_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFile();
            if (file != null) Process.Start("explorer.exe", $"/select, \"{file.FullName}\"");
        }

        private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFile();
            if (file != null) Clipboard.SetText(file.FullName);
        }

        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            var file = GetSelectedFile();
            if (file != null && MessageBox.Show("Bu dosya kalıcı olarak silinecek?", "Dikkat", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    file.Delete();
                    _allFiles.Remove(file); // Listeden de düş
                    ApplyFilterAndSort(); // Listeyi yenile
                }
                catch { MessageBox.Show("Silinemedi. Dosya açık olabilir."); }
            }
        }
    }
}