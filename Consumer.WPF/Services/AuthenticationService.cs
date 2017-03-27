using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentity;
using Amazon.CognitoIdentity.Model;
using Amazon.Runtime;
using Consumer.Models;
using Newtonsoft.Json;

namespace Consumer.Services
{
    public class AuthenticationService
    {
        public AuthenticationService()
        {
            Credentials = new CognitoAWSCredentials("us-east-1:3ffa0f90-eb58-4365-9908-209bc289e61c", RegionEndpoint.USEast1);
        }

        public CognitoAWSCredentials Credentials { get; }

        private readonly HttpClient _client = new HttpClient();

        public async Task<List<Exam>> GetExams()
        {
            var response = await _client.GetAsync("https://lix-dev.pyramidchallenges.com/api/exam");
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<Exam>>(content);
        }

        private readonly Regex _regex = new Regex("/^.*[\\\\\\/] /");
        public Me Me;

        public async Task Download(string path, string fileName, IProgress<double> progress, CancellationToken token, Stream outputStream)
        {

            path = _regex.Replace(path, "");
            var url = $"https://lix-dev.pyramidchallenges.com/api/file/{WebUtility.UrlEncode(path)}/{WebUtility.UrlEncode(fileName)}";
            var response = await _client.GetAsync(url, token);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"The request returned with HTTP status code {response.StatusCode}");
            }

            var total = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = total != -1 && progress != null;

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                var totalRead = 0L;
                var buffer = new byte[4096];
                var isMoreToRead = true;

                do
                {
                    token.ThrowIfCancellationRequested();

                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, token);

                    if (read == 0)
                    {
                        isMoreToRead = false;
                    }
                    else
                    {
                        var data = new byte[read];
                        buffer.ToList().CopyTo(index: 0, array: data, arrayIndex: 0, count: read);

                        await outputStream.WriteAsync(data, 0, read, token);

                        totalRead += read;

                        if (canReportProgress)
                        {
                            progress.Report(totalRead * 1d / (total * 1d) * 100);
                        }
                    }
                } while (isMoreToRead);
            }
        }

        public async Task<ImmutableCredentials> Login()
        {
            //TODO: Pass username/password
            var values = new Dictionary<string, string>
            {
                { "username", "" },
                { "password", "" }
            };

            var content = new FormUrlEncodedContent(values);

            var response = await _client.PostAsync("https://lix-dev.pyramidchallenges.com/auth/signin", content);

            if (response.StatusCode != HttpStatusCode.OK) return null;

            var meResponse = await _client.GetAsync("https://lix-dev.pyramidchallenges.com/me");
            var contentString = await meResponse.Content.ReadAsStringAsync();
            Me = JsonConvert.DeserializeObject<Me>(contentString);
            Credentials.AddLogin(Me.LoginKey, Me.Token);

            // initialize a set of anonymous AWS credentials for our API calls
            AnonymousAWSCredentials cred = new AnonymousAWSCredentials();
            AmazonCognitoIdentityClient cognitoClient = new AmazonCognitoIdentityClient(
                cred, // the anonymous credentials
                RegionEndpoint.USEast1 // the Amazon Cognito region
            );

            GetIdRequest idRequest = new GetIdRequest
            {
                IdentityPoolId = Credentials.IdentityPoolId,
                Logins = new Dictionary<string, string>
                {
                    {Me.LoginKey, Me.Token}
                }
            };
            // set the Dictionary of logins if you are authenticating users 
            // through an identity provider

            // The identity id is in the IdentityId parameter of the response object
            await cognitoClient.GetIdAsync(idRequest)
                 .ContinueWith(idResponse =>
                 {
                     Me.IdentityId = idResponse.Result.IdentityId;

                 });
            return Credentials.GetCredentialsAsync().Result;
        }
    }
}