using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MyYoutubeDownloader;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

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

            // Set the output file path
            var outputPath = Path.Combine(downloadFolder, $"{videoTitleUnique}.mp4");

            try
            {
                // Get the stream manifest
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
                var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                // Download the video
                await youtube.Videos.Streams.DownloadAsync(streamInfo, outputPath);

                Console.WriteLine($"Download completed: {outputPath}");

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
        //Console.Write("Enter the download folder name (default: 'DownloadedVideos'): ");
        //string downloadFolder = Console.ReadLine();
        //if (string.IsNullOrWhiteSpace(downloadFolder))
        //{
        //    downloadFolder = "DownloadedVideos";
        //}

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
}