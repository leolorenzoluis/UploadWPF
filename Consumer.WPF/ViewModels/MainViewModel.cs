using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Consumer.Annotations;
using Consumer.Models;

namespace Consumer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AuthenticationService _authenticationService;
        private Visibility _uploadContainerVisibility;

        public MainViewModel()
        {
            _authenticationService = new AuthenticationService();
            Setup();
        }

        private async void Setup()
        {
            UploadContainerVisibility = Visibility.Collapsed;
            await _authenticationService.Login();
            Exams = await _authenticationService.GetExams();
            OnPropertyChanged(nameof(Exams));
        }

        public ObservableCollection<UploadViewModel> Uploads { get; } = new ObservableCollection<UploadViewModel>();

        public List<Exam> Exams { get; set; }

        public Exam SelectedExam { get; set; }

        public async Task UploadFiles(IEnumerable<string> files)
        {
            try
            {
                var s3Config = new AmazonS3Config
                {
                    SignatureVersion = "4",
                    RegionEndpoint = RegionEndpoint.USEast1
                };
                var s3Client = new AmazonS3Client(_authenticationService.Credentials, s3Config);

                var exam = SelectedExam;

                const string bucketName = "lix.dev.files";
                using (var transferUtility = new TransferUtility(s3Client))
                {
                    foreach (var filePath in files)
                    {
                        var isDirectory = (System.IO.File.GetAttributes(filePath) & FileAttributes.Directory) != 0;
                        if (isDirectory)
                        {
                            var request =
                                new TransferUtilityUploadDirectoryRequest
                                {
                                    BucketName = bucketName,
                                    Directory = filePath,
                                    SearchOption = SearchOption.AllDirectories,
                                    SearchPattern = "*.*",
                                    KeyPrefix = $"{_authenticationService.Me.IdentityId}/{exam?.ExamId}"
                                };
                            request.UploadDirectoryProgressEvent += RequestOnUploadDirectoryProgressEvent;
                            await transferUtility.UploadDirectoryAsync(request);
                        }
                        else
                        {
                            Uploads.Add(new UploadViewModel(s3Client, filePath, bucketName, exam?.ExamId, _authenticationService.Me.IdentityId));
                            UploadContainerVisibility = Visibility.Visible;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
        }

        public Task Download(string filePath, string fileName, Progress<double> progress, CancellationToken cancellationToken, FileStream fileStream)
        {
            return _authenticationService.Download(filePath, fileName, progress, new CancellationToken(),
                       fileStream);
        }

        public Visibility UploadContainerVisibility
        {
            get { return _uploadContainerVisibility; }
            set { _uploadContainerVisibility = value; OnPropertyChanged(); }
        }


        private async Task UsingTransferUtility(IEnumerable<string> files, AmazonS3Client s3Client, string bucketName)
        {
            using (var transferUtility = new TransferUtility(s3Client))
            {
                foreach (var filePath in files)
                {
                    var isDirectory = (System.IO.File.GetAttributes(filePath) & FileAttributes.Directory) != 0;
                    if (isDirectory)
                    {
                        var request =
                            new TransferUtilityUploadDirectoryRequest
                            {
                                BucketName = bucketName,
                                Directory = filePath,
                                SearchOption = SearchOption.AllDirectories,
                                SearchPattern = "*.*",
                            };
                        request.UploadDirectoryProgressEvent += RequestOnUploadDirectoryProgressEvent;
                        await transferUtility.UploadDirectoryAsync(request);
                    }
                    else
                    {
                        var request =
                            new TransferUtilityUploadRequest()
                            {
                                BucketName = bucketName,
                                Key = Path.GetFileName(filePath),
                                FilePath = filePath,
                            };
                        request.UploadProgressEvent += RequestOnUploadProgressEvent;
                        await transferUtility.UploadAsync(request);
                    }
                }
            }
        }

        private void RequestOnUploadProgressEvent(object sender, UploadProgressArgs e)
        {
            // Process event.

            int pctProgress = (int)(e.TransferredBytes * 100 / e.TotalBytes);
            Console.WriteLine(@"Progress {0}", pctProgress);
            Console.WriteLine(@"{0}/{1}", e.TransferredBytes, e.TotalBytes);
            Console.WriteLine(e.ToString());
            Console.WriteLine(e.TransferredBytes);
            Console.WriteLine(e.TotalBytes);
        }

        private void RequestOnUploadDirectoryProgressEvent(object sender, UploadDirectoryProgressArgs e)
        {
            Console.WriteLine(e.ToString());
        }



        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
