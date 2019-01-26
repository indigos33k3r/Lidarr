using System.IO;
using NLog;
using NzbDrone.Core.Parser.Model;
using System.Diagnostics;
using System.Linq;
using NzbDrone.Common.Http;
using NzbDrone.Common.Extensions;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using NzbDrone.Common.Serializer;
using System;
using NzbDrone.Common.EnvironmentInfo;
using System.Threading;

namespace NzbDrone.Core.Parser
{
    public interface IFingerprintingService
    {
        AcoustId GetFingerprint(string filename);
        void Lookup(List<LocalTrack> tracks, double threshold);
        Dictionary<int, List<string>> Lookup(List<string> files, double threshold);
    }

    public class AcoustId
    {
        public double Duration;
        public string Fingerprint;
    }

    public class FingerprintingService : IFingerprintingService
    {
        private const string _acoustIdUrl = "http://api.acoustid.org/v2/lookup";
        private const string _acoustIdApiKey = "QANd68ji1L";
        
        private readonly Logger _logger;
        private readonly IHttpClient _httpClient;
        private readonly string _fpcalcPath;

        private IHttpRequestBuilderFactory _customerRequestBuilder;

        public FingerprintingService(Logger logger,
                                     IHttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;

            _customerRequestBuilder = new HttpRequestBuilder(_acoustIdUrl).CreateFactory();
            _fpcalcPath = GetFpcalcPath();
        }

        public bool IsSetup => _fpcalcPath.IsNotNullOrWhiteSpace();

        private string GetFpcalcPath()
        {
            string path = null;
            if (OsInfo.IsLinux)
            {
                // must be on users path on Linux
                path = "fpcalc";

                // check that the command exists
                Process p = new Process();
                p.StartInfo.FileName = "which";
                p.StartInfo.Arguments = $"{path}";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;

                p.Start();
                // To avoid deadlocks, always read the output stream first and then wait.  
                string output = p.StandardOutput.ReadToEnd();  
                p.WaitForExit(1000);

                if (p.ExitCode != 0)
                {
                    _logger.Debug("fpcalc not found");
                    return null;
                }

                _logger.Trace("fpcalc exists on path");
            }
            else
            {
                // on OSX / Windows, we have put fpcalc in the application folder
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fpcalc");
                if (OsInfo.IsWindows)
                {
                    path += ".exe";
                }
            }
            
            _logger.Debug($"fpcalc path: {path}");
            return path;
        }

        public AcoustId GetFingerprint(string file)
        {
            if (IsSetup && File.Exists(file))
            {
                Process p = new Process();
                p.StartInfo.FileName = _fpcalcPath;
                p.StartInfo.Arguments = $"-json \"{file}\"";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                int timeout = 10000;

                _logger.Trace("Executing {0} {1}", p.StartInfo.FileName, p.StartInfo.Arguments);

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                // see https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why?lq=1
                // this is most likely overkill...
                using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                 {
                     using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                     {
                         DataReceivedEventHandler outputHandler = delegate(object sender, DataReceivedEventArgs e)
                             {
                                 if (e.Data == null)
                                 {
                                     outputWaitHandle.Set();
                                 }
                                 else
                                 {
                                     output.AppendLine(e.Data);
                                 }
                             };

                         DataReceivedEventHandler errorHandler = delegate(object sender, DataReceivedEventArgs e)
                             {
                                 if (e.Data == null)
                                 {
                                     errorWaitHandle.Set();
                                 }
                                 else
                                 {
                                     error.AppendLine(e.Data);
                                 }
                             };
                         
                         p.OutputDataReceived += outputHandler;
                         p.ErrorDataReceived += errorHandler;

                         p.Start();

                         p.BeginOutputReadLine();
                         p.BeginErrorReadLine();

                         if (p.WaitForExit(timeout) &&
                             outputWaitHandle.WaitOne(timeout) &&
                             errorWaitHandle.WaitOne(timeout))
                         {
                             // Process completed.
                             if (p.ExitCode != 0)
                             {
                                 throw new Exception("fpcalc failed with error: " + error.ToString());
                             }
                             else
                             {
                                 return Json.Deserialize<AcoustId>(output.ToString());
                             }
                         }
                         else
                         {
                             // Timed out.  Remove handlers to avoid object disposed error
                             p.OutputDataReceived -= outputHandler;
                             p.ErrorDataReceived -= errorHandler;
                             
                             throw new Exception("fpcalc timed out." + error.ToString());
                         }
                     }
                 }
            }

            return new AcoustId();
        }

        private static byte[] Compress(byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        public void Lookup(List<LocalTrack> tracks, double threshold)
        {
            if (!IsSetup)
            {
                return;
            }
            
            var results = Lookup(tracks.Select(x => x.Path).ToList(), threshold);
            if (results != null)
            {
                foreach (var result in results)
                {
                    tracks[result.Key].AcoustIdResults = result.Value;
                }
            }
        }

        public Dictionary<int, List<string>> Lookup(List<string> files, double threshold = 0)
        {
            if (!IsSetup)
            {
                return null;
            }

            var httpRequest = _customerRequestBuilder.Create()
                .WithRateLimit(0.334)
                .Build();

            var sb = new StringBuilder($"client={_acoustIdApiKey}&format=json&meta=recordingids&batch=1", 2000);
            for(int i = 0; i < files.Count; i++)
            {
                var fingerprint = GetFingerprint(files[i]);
                sb.AppendFormat("&duration.{0}={1:F0}&fingerprint.{0}={2}",
                                i, fingerprint.Duration, fingerprint.Fingerprint);
            }
            
            // they prefer a gzipped body
            httpRequest.SetContent(Compress(Encoding.UTF8.GetBytes(sb.ToString())));
            httpRequest.Headers.Add("Content-Encoding", "gzip");
            httpRequest.Headers.ContentType = "application/x-www-form-urlencoded";

            var httpResponse = _httpClient.Post<LookupResponse>(httpRequest);

            if (httpResponse.HasHttpError)
            {
                throw new HttpException(httpRequest, httpResponse);
            }

            var response = httpResponse.Resource;

            if (!string.IsNullOrEmpty(response.ErrorMessage))
            {
                _logger.Debug("Webservice error: {0}", response.ErrorMessage);
                return null;
            }

            var output = new Dictionary<int, List<string>>();
            foreach (var fileResponse in response.Fingerprints)
            {
                if (fileResponse.Results.Count == 0)
                {
                    _logger.Debug("No results for given fingerprint.");
                    continue;
                }

                foreach (var result in fileResponse.Results.Where(x => x.Recordings != null))
                {
                    _logger.Trace("Found: {0}, {1}, {2}", result.Id, result.Score, string.Join(", ", result.Recordings.Select(x => x.Id)));
                }

                var ids = fileResponse.Results.Where(x => x.Score > threshold && x.Recordings != null).SelectMany(y => y.Recordings.Select(z => z.Id)).Distinct().ToList();
                _logger.Trace("All recordings: {0}", string.Join("\n", ids));

                output.Add(fileResponse.index, ids);
            }

            _logger.Debug("Fingerprinting complete.");
            return output;
        }

        private class LookupResponse
        {
            public string StatusCode { get; set; }
            public string ErrorMessage { get; set; }
            public List<LookupResultListItem> Fingerprints { get; set; }
        }

        private class LookupResultListItem
        {
            public int index { get; set; }
            public List<LookupResult> Results { get; set; }
        }

        private class LookupResult
        {
            public string Id { get; set; }
            public double Score { get; set; }
            public List<RecordingResult> Recordings { get; set; }
        }

        private class RecordingResult
        {
            public string Id { get; set; }
        }
    }
}