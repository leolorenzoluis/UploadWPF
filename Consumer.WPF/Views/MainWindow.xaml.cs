using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Consumer.ViewModels;
using Microsoft.WindowsAPICodePack.Dialogs;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using File = Consumer.Models.File;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Consumer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        public MainWindow()
        {
            DataContext = new MainViewModel();
            InitializeComponent();
        }

        private async void DownloadFile(object sender, RoutedEventArgs e)
        {
            var frameworkContentElement = sender as FrameworkElement;
            var map = frameworkContentElement?.DataContext as KeyValuePair<string, File>? ??
                      new KeyValuePair<string, File>();
            var file = map.Value;

            var dialog = new SaveFileDialog()
            {
                Filter = "All(*.*)|*"
            };

            if (!dialog.ShowDialog().GetValueOrDefault()) return;
            using (var fileStream = System.IO.File.Create(dialog.FileName))
            {
                var progress = new Progress<double>();
                progress.ProgressChanged += (x, value) =>
                {
                    if (Math.Abs(value % 10) < 0.5)
                        Dispatcher.BeginInvoke(DispatcherPriority.Background,
                            new Action(() => file.DownloadProgress = value));
                };
                var download = ViewModel?.Download(file.FilePath, file.FileName, progress, new CancellationToken(),
                    fileStream);
                if (download != null)
                    await download;
            }
        }

        public MainViewModel ViewModel => DataContext as MainViewModel;

        private async void OnUploadDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            // Note that you can have more than one file.
            var files = (string[]) e.Data.GetData(DataFormats.FileDrop);
            await ViewModel.UploadFiles(files);
        }
        

       
        private void OnExamSelected(object sender, SelectionChangedEventArgs e)
        {
            FilesContainer.Visibility = Visibility.Visible;
        }

        private async void Upload(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                InitialDirectory = "C:\\Users",
                AllowNonFileSystemItems = true,
                Multiselect = true,
                IsFolderPicker = true
            };
            if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return;
            var uploadFiles = ViewModel?.UploadFiles(dialog.FileNames);
            if (uploadFiles != null) await uploadFiles;
        }
    }
}