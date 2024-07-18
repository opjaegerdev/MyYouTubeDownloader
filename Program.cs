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
            Console.Write("Enter the YouTube video URL: ");
            string videoUrl = Console.ReadLine();

            // Get the video ID from the URL
            var videoId = YoutubeHelper.ParseVideoId(videoUrl);

            if (string.IsNullOrEmpty(videoId))
            {
                Console.WriteLine("Invalid YouTube URL.");
                return;
            }

            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(videoId);
            var videoTitle = video.Title;

            Console.WriteLine($"Downloading: {videoTitle}");

            // Set the output file path
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{videoTitle}.mp4");

            // Get the stream manifest
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoId);
            var streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();

            // Download the video
            await youtube.Videos.Streams.DownloadAsync(streamInfo, outputPath);

            Console.WriteLine($"Download completed: {outputPath}");
        }
    }
}
