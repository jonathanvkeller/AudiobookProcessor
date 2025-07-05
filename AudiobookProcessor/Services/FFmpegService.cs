using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AudiobookProcessor.Services;

/// <summary>
/// Manages all interactions with the ffmpeg.exe command-line tool.
/// </summary>
public class FFmpegService
{
    // A placeholder for the path to the ffmpeg executable.
    private readonly string ffmpegPath = "ffmpeg.exe";

    /// <summary>
    /// Converts a single audio file to M4B format.
    /// </summary>
    public async Task convertToM4bAsync(string inputFile, string outputFile)
    {
        // -i: specifies the input file.
        // -c:a aac: sets the audio codec to AAC.
        // -c:v copy: copies the video stream (cover art) without re-encoding.
        // -y: overwrites the output file if it exists.
        string arguments = $"-i \"{inputFile}\" -c:a aac -c:v copy -y \"{outputFile}\"";

        await runProcessAsync(arguments);
    }

    /// <summary>
    /// Combines multiple audio files into a single file.
    /// </summary>
    public async Task combineFilesAsync(string[] inputFiles, string outputFile)
    {
        string tempFileListPath = string.Empty;
        try
        {
            // Create a temporary file to list the inputs for FFmpeg's concat demuxer.
            tempFileListPath = Path.GetTempFileName();

            // Build the content for the file list. Each line must be in the format: file '/path/to/file.mp3'
            var fileListContent = new StringBuilder();
            foreach (var inputFile in inputFiles)
            {
                // Using single quotes and forward slashes for compatibility with FFmpeg's file list format.
                fileListContent.AppendLine($"file '{inputFile.Replace('\\', '/')}'");
            }
            await File.WriteAllTextAsync(tempFileListPath, fileListContent.ToString());

            // -f concat: use the concat demuxer.
            // -safe 0: required for using absolute paths in the file list.
            // -i: specifies the input file (our generated list).
            // -c copy: stream copy the audio without re-encoding.
            // -y: overwrite the output file if it exists.
            string arguments = $"-f concat -safe 0 -i \"{tempFileListPath}\" -c copy -y \"{outputFile}\"";

            await runProcessAsync(arguments);
        }
        finally
        {
            // Always clean up the temporary file list, even if an error occurs.
            if (!string.IsNullOrEmpty(tempFileListPath) && File.Exists(tempFileListPath))
            {
                File.Delete(tempFileListPath);
            }
        }
    }

    /// <summary>
    /// Extracts metadata from a file using FFmpeg/FFprobe.
    /// </summary>
    public async Task<string> extractMetadataAsync(string inputFile)
    {
        // Implementation to come later.
        await Task.CompletedTask;
        return string.Empty;
    }

    /// <summary>
    /// Embeds chapter markers into an M4B file.
    /// </summary>
    public async Task embedChaptersAsync(string inputFile, string chapterFile, string outputFile)
    {
        // Implementation to come later.
        await Task.CompletedTask;
    }

    /// <summary>
    /// A private helper to run an FFmpeg command and wait for it to complete.
    /// </summary>
    private async Task runProcessAsync(string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = processStartInfo };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new System.Exception($"FFmpeg failed with exit code {process.ExitCode}: {error}");
        }
    }
}
