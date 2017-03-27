using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Consumer.Annotations;

namespace Consumer.ViewModels
{
    public class UploadViewModel : INotifyPropertyChanged
    {
        private readonly string _bucketName;
        private readonly string _filePath;
        private readonly AmazonS3Client _s3Client;
        private readonly Dictionary<int, long> _totalBytesSent;
        private readonly string _key;
        private long _totalFileSize;

        public UploadViewModel(AmazonS3Client s3Client, string filePath, string bucketName, string examId, string identityId)
        {
            _s3Client = s3Client;
            _filePath = filePath;
            FileName = Path.GetFileName(filePath);
            _key = $"{identityId}/{examId}/{FileName}";
            _bucketName = bucketName;
            _totalFileSize = new FileInfo(_filePath).Length;
            _totalBytesSent = new Dictionary<int, long>();
            Upload();
        }

        public string FileName { get; }

        public long TransferredBytes
        {
            get
            {
                if (_totalBytesSent != null && _totalBytesSent.Count > 0)
                    return (long) decimal.Divide(_totalBytesSent.Sum(x => x.Value) * 100, _totalFileSize);
                return 0;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private async void Upload()
        {
            var partETags = new List<PartETag>();
            // 1. Initialize.
            var initiateRequest = new InitiateMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = _key
            };

            var initResponse =
                _s3Client.InitiateMultipartUpload(initiateRequest);

            // 2. Upload Parts.
            long partSize = 5242880; // 5 MB
            try
            {
                long filePosition = 0;
                var totalPartNumber = 1;
                for (; filePosition < _totalFileSize; totalPartNumber++)
                {
                    // Create request to upload a part.
                    var uploadRequest = new UploadPartRequest
                    {
                        BucketName = _bucketName,
                        Key = _key,
                        UploadId = initResponse.UploadId,
                        PartNumber = totalPartNumber,
                        PartSize = partSize,
                        FilePosition = filePosition,
                        FilePath = _filePath
                    };
                    uploadRequest.StreamTransferProgress += (_, args) => OnProgress(uploadRequest, args);
                    // Upload part and add response to our list.
                    var request = await _s3Client.UploadPartAsync(uploadRequest);

                    filePosition += partSize;
                    var petag = new PartETag(request.PartNumber, request.ETag);
                    partETags.Add(petag);
                }

                // Step 3: complete.
                var completeRequest = new CompleteMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = _key,
                    UploadId = initResponse.UploadId,
                    PartETags = partETags
                };
                var completeUploadResponse =
                    _s3Client.CompleteMultipartUpload(completeRequest);
            }
            catch (Exception exception)
            {
                Console.WriteLine(@"Exception occurred: {0}", exception.Message);
                var abortMpuRequest = new AbortMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = _key,
                    UploadId = initResponse.UploadId
                };
                _s3Client.AbortMultipartUpload(abortMpuRequest);
            }
        }

        private void OnProgress(UploadPartRequest uploadRequest, StreamTransferProgressArgs e)
        {
            if (!_totalBytesSent.ContainsKey(uploadRequest.PartNumber))
                _totalBytesSent.Add(uploadRequest.PartNumber, value: 0);
            else
                _totalBytesSent[uploadRequest.PartNumber] = e.TransferredBytes;
            OnPropertyChanged(nameof(TransferredBytes));
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}