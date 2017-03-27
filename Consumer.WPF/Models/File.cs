using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Consumer.Models
{
    public class File : INotifyPropertyChanged
    {
        private double _downloadProgress;
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime UploadDate { get; set; }
        public string UserId { get; set; }
        public string FileId { get; set; }

        public double DownloadProgress
        {
            get { return _downloadProgress; }
            set { _downloadProgress = value; OnPropertyChanged(); }
        }

        public void Download()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}