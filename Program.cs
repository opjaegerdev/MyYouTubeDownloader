using YoutubeExplode;
using System;
using System.IO;
using System.Threading.Tasks;
using YoutubeExplode.Converter;
using YoutubeExplode.Videos.Streams;

namespace MyYoutubeDownloader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
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
                var videoTitle = video.Title;

                Console.WriteLine($"Downloading: {videoTitle}");

                // Set the output file path
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{videoTitle}.mp4");

                try
                {
                    // Get the stream manifest
                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
                    var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

                    // Download the video
                    await youtube.Videos.Streams.DownloadAsync(streamInfo, outputPath);

                    Console.WriteLine($"Download completed: {outputPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

                Console.WriteLine();
            }
        }
    }
}
