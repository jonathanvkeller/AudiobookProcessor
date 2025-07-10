using AudiobookProcessor.Models;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookProcessor.Services;

public class FFmpegService
{
    private readonly string ffmpegPath = "ffmpeg.exe";
    private readonly string ffprobePath = "ffprobe.exe";

    public async Task convertToM4bAsync(string inputFile, string outputFile, TimeSpan totalDuration, IProgress<ProcessingStatus> progress)
    {
        string arguments = $"-i \"{inputFile}\" -c:a aac -b:a 128k -vn -y \"{outputFile}\"";
        await runProcessAsync(ffmpegPath, arguments, null, totalDuration, progress);
    }

    public async Task combineFilesAsync(string[] inputFiles, string baseFolderPath, string outputFile, TimeSpan totalDuration, IProgress<ProcessingStatus> progress)
    {
        string tempFileListPath = Path.Combine(baseFolderPath, "ffmpeg_concat_list.txt");
        try
        {
            var fileListContent = new StringBuilder();
            foreach (var inputFile in inputFiles)
            {
                var relativePath = Path.GetRelativePath(baseFolderPath, inputFile);
                fileListContent.AppendLine($"file '{relativePath.Replace('\\', '/')}'");
            }
            await File.WriteAllTextAsync(tempFileListPath, fileListContent.ToString());

            string arguments = $"-f concat -safe 0 -i \"{tempFileListPath}\" -c:a aac -vn -b:a 128k -f mp4 -y \"{outputFile}\"";

            await runProcessAsync(ffmpegPath, arguments, baseFolderPath, totalDuration, progress);
        }
        finally
        {
            if (File.Exists(tempFileListPath))
            {
                File.Delete(tempFileListPath);
            }
        }
    }

    public async Task<string> extractMetadataAsync(string inputFile)
    {
        string arguments = $"-v quiet -print_format json -show_format -show_streams \"{inputFile}\"";
        return await runProcessAsync(ffprobePath, arguments);
    }

    public async Task embedChaptersAsync(string inputFile, string chapterFile, string outputFile)
    {
        string arguments = $"-i \"{inputFile}\" -i \"{chapterFile}\" -map 0 -map_metadata 1 -codec copy -y \"{outputFile}\"";
        await runProcessAsync(ffmpegPath, arguments);
    }

    private Task<string> runProcessAsync(string executablePath, string arguments, string workingDirectory = null, TimeSpan? totalDuration = null, IProgress<ProcessingStatus> progress = null)
    {
        var tcs = new TaskCompletionSource<string>();
        var stopwatch = new Stopwatch();

        var process = new Process
        {
            StartInfo =
            {
                FileName = executablePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            },
            EnableRaisingEvents = true
        };

        var output = new StringBuilder();
        var error = new StringBuilder();

        process.Exited += (sender, args) =>
        {
            if (process.ExitCode == 0)
            {
                tcs.SetResult(output.ToString());
            }
            else
            {
                tcs.SetException(new System.Exception($"{Path.GetFileName(executablePath)} failed with exit code {process.ExitCode}: {error}"));
            }
            process.Dispose();
        };

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null) output.AppendLine(args.Data);
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data == null) return;
            error.AppendLine(args.Data);

            if (totalDuration.HasValue && progress != null && args.Data.Contains("time="))
            {
                var timeString = args.Data.Substring(args.Data.IndexOf("time=") + 5, 11);
                if (TimeSpan.TryParse(timeString, CultureInfo.InvariantCulture, out var currentTime))
                {
                    var percentage = (currentTime.TotalSeconds / totalDuration.Value.TotalSeconds) * 100;

                    TimeSpan etr = TimeSpan.Zero;
                    if (percentage > 1)
                    {
                        var estimatedTotalTime = TimeSpan.FromMilliseconds(stopwatch.Elapsed.TotalMilliseconds / (percentage / 100));
                        etr = estimatedTotalTime - stopwatch.Elapsed;
                    }

                    progress.Report(new ProcessingStatus { progressPercentage = Math.Min(100, percentage), etr = etr });
                }
            }
        };

        process.Start();
        stopwatch.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return tcs.Task;
    }
}