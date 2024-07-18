using System;
using System.IO;
using System.Text.RegularExpressions;

public static class FileNameHelper
{
    public static string SanitizeYouTubeTitle(string title)
    {
        // Define a regular expression pattern to match unwanted characters.
        string pattern = @"[^a-zA-Z0-9\s]";

        // Replace matched characters with an empty string.
        string sanitizedTitle = Regex.Replace(title, pattern, "");

        // Trim leading and trailing spaces.
        sanitizedTitle = sanitizedTitle.Trim();

        return sanitizedTitle;
    }

    public static string EnsureUniqueFileName(string directory, string fileName)
    {
        string sanitizedFileName = SanitizeYouTubeTitle(fileName);
        string filePath = Path.Combine(directory, sanitizedFileName);

        int copyIndex = 1;
        string newFilePath = filePath;

        // Check if the file exists and append "copy[i]" if it does
        while (File.Exists(newFilePath))
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedFileName);
            string extension = Path.GetExtension(sanitizedFileName);
            newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension} copy{copyIndex}{extension}");
            copyIndex++;
        }

        return newFilePath;
    }
}
