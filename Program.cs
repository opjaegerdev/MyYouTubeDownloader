using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MyYoutubeDownloader;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace MyYoutubeDownloader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string configFileName = "AppSettings.json";
            string defaultDownloadFolder = "DownloadedVideos";

            // Check if configuration file exists
            if (!File.Exists(configFileName))
            {
                Console.WriteLine($"Configuration file '{configFileName}' not found. Let's create one.");

                if (!Directory.Exists(defaultDownloadFolder))
                {
                    Console.WriteLine($"Default download folder '{defaultDownloadFolder}' not found. Creating it.");
                    Directory.CreateDirectory(defaultDownloadFolder);
                }

                var userAppSettings = GetAppSettingsFromUser();
                SaveAppSettingsToFile(userAppSettings, configFileName);
            }

            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFileName, optional: false, reloadOnChange: true)
                .Build();

            var appSettings = configuration.GetSection("DownloadSettings").Get<AppSettings>();

            // Get the download folder from configuration
            string downloadFolder = Path.Combine(Directory.GetCurrentDirectory(), appSettings.defaultDownloadFolder);
            bool openDownloadOnFinishToggle = appSettings.openDownloadFolderOnFinish;
            bool openDownloadOnFinishAlways = appSettings.openDownloadFolderOnFinishAlways;

            // Ensure the directory exists
            if (!Directory.Exists(downloadFolder))
            {
                Directory.CreateDirectory(downloadFolder);
            }

            while (true)
            {
                Console.Write("Enter the YouTube video URL (or type 'exit' to quit): ");
                string videoUrl = Console.ReadLine();

                if (videoUrl.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // Get the video ID from the URL
                var videoId = YoutubeHelper.ParseVideoId(videoUrl);

                if (string.IsNullOrEmpty(videoId))
                {
                    Console.WriteLine("Invalid YouTube URL.");
                    continue;
                }

                var youtube = new YoutubeClient();
                var video = await youtube.Videos.GetAsync(videoId);
                var videoTitleClean = FileNameHelper.SanitizeYouTubeTitle(video.Title);
                var videoTitleUnique = FileNameHelper.EnsureUniqueFileName(downloadFolder, videoTitleClean);

                Console.WriteLine($"Downloading: {videoTitleUnique}");

                // Set the output file paths
                var videoPath = Path.Combine(downloadFolder, $"{videoTitleUnique}.mp4");
                var audioPath = Path.Combine(downloadFolder, $"{videoTitleUnique}.mp3");

                try
                {
                    // Get the stream manifest
                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);

                    // Get the highest quality video and audio streams
                    var videoStreamInfo = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
                    var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                    if (videoStreamInfo == null || audioStreamInfo == null)
                    {
                        Console.WriteLine("No suitable streams found.");
                        continue;
                    }

                    // Download the video and audio streams
                    await youtube.Videos.Streams.DownloadAsync(videoStreamInfo, videoPath);
                    await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioPath);

                    // Mux the video and audio streams together
                    var outputFilePath = Path.Combine(downloadFolder, $"{videoTitleUnique}_final.mp4");
                    MuxVideoAndAudio(videoPath, audioPath, outputFilePath);

                    // Clean up intermediate files
                    File.Delete(videoPath);
                    File.Delete(audioPath);

                    Console.WriteLine($"Download completed: {outputFilePath}");

                    if (openDownloadOnFinishToggle)
                    {
                        // Open the download folder in File Explorer
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = downloadFolder,
                            UseShellExecute = true,
                            Verb = "open"
                        });

                        // Toggle off if not set to always open
                        if (!openDownloadOnFinishAlways)
                        {
                            openDownloadOnFinishToggle = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                Console.WriteLine();
            }
        }

        private static AppSettings GetAppSettingsFromUser()
        {
            string downloadFolder = "DownloadedVideos";
            bool openAlways = false;

            Console.Write("Enable open download folder on finish (true/false, default: true): ");
            string openOnFinishInput = Console.ReadLine();
            bool openOnFinish = string.IsNullOrWhiteSpace(openOnFinishInput) || bool.Parse(openOnFinishInput);

            if (openOnFinish)
            {
                Console.Write("Enable open download folder on finish every finish (true/false, default: false): ");
                string openAlwaysInput = Console.ReadLine();
                openAlways = !string.IsNullOrWhiteSpace(openAlwaysInput) && bool.Parse(openAlwaysInput);
            }

            return new AppSettings
            {
                defaultDownloadFolder = downloadFolder,
                openDownloadFolderOnFinish = openOnFinish,
                openDownloadFolderOnFinishAlways = openAlways
            };
        }

        private static void SaveAppSettingsToFile(AppSettings settings, string fileName)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(new { DownloadSettings = settings }, options);
            File.WriteAllText(fileName, json);
        }

        private static void MuxVideoAndAudio(string videoPath, string audioPath, string outputPath)
        {
            var ffmpegPath = "ffmpeg"; // Ensure ffmpeg is installed and accessible from the PATH
            var args = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -b:a 192k -movflags +faststart \"{outputPath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    output.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    error.AppendLine(e.Data);
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            bool exited = process.WaitForExit(60000); // Wait for a maximum of 60 seconds

            if (!exited)
            {
                process.Kill();
                throw new Exception("FFmpeg process timed out.");
            }

            if (process.ExitCode != 0)
            {
                throw new Exception($"FFmpeg exited with error code {process.ExitCode}: {error.ToString()}");
            }

            Console.WriteLine(output.ToString());
        }
    }
}

