using AudiobookProcessor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AudiobookProcessor.Services;

public class AudioProcessor
{
    private readonly FileService fileService;
    private readonly FFmpegService ffmpegService;
    private readonly MetadataService metadataService;
    private readonly List<string> supportedExtensions = new()
        { ".mp3", ".m4a", ".m4b", ".flac", ".ogg", ".wma" };

    public AudioProcessor(
        FileService fileService,
        FFmpegService ffmpegService,
        MetadataService metadataService)
    {
        this.fileService = fileService;
        this.ffmpegService = ffmpegService;
        this.metadataService = metadataService;
    }

    private void reportProgress(IProgress<ProcessingStatus> progress, ProcessingPhase phase, string message, double? percentage = null)
    {
        progress.Report(new ProcessingStatus { phase = phase, statusMessage = message, progressPercentage = percentage ?? 0 });
    }

    public Task<FolderAnalysis> analyzeFolderAsync(string folderPath)
    {
        var analysis = new FolderAnalysis();

        var audioFilePaths = Directory.EnumerateFiles(folderPath, "*.*", SearchOption.AllDirectories)
            .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        analysis.AudioFilePaths = audioFilePaths;

        if (audioFilePaths.Count == 0)
        {
            analysis.RecommendedAction = ProcessingAction.NoAudioFilesFound;
            analysis.AnalysisMessage = "No supported audio files found in this folder.";
        }
        else if (audioFilePaths.Count == 1 && Path.GetExtension(audioFilePaths[0]).ToLowerInvariant() == ".m4b")
        {
            analysis.RecommendedAction = ProcessingAction.AlreadyProcessed;
            analysis.AnalysisMessage = "Folder appears to be already processed.";
        }
        else if (audioFilePaths.Count == 1)
        {
            analysis.RecommendedAction = ProcessingAction.ConvertSingleFile;
            analysis.AnalysisMessage = $"Found one audio file to convert to M4B.";
        }
        else
        {
            analysis.RecommendedAction = ProcessingAction.CombineMultipleFiles;
            analysis.AnalysisMessage = $"Found {audioFilePaths.Count} audio files to combine into a single M4B.";
        }

        return Task.FromResult(analysis);
    }

    public async Task processFolderAsync(string folderPath, IProgress<ProcessingStatus> progress)
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"AudiobookProcessor_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(tempDirectory);
        var audioFiles = new List<AudioFile>();
        var analysis = new FolderAnalysis(); // Define analysis here to access it later

        try
        {
            reportProgress(progress, ProcessingPhase.Analysis, "Analyzing folder...");
            analysis = await analyzeFolderAsync(folderPath);

            reportProgress(progress, ProcessingPhase.Processing, "Gathering file information...");
            foreach (var path in analysis.AudioFilePaths)
            {
                audioFiles.Add(await metadataService.getAudioFileAsync(path));
            }
            var totalDuration = TimeSpan.FromSeconds(audioFiles.Sum(f => f.duration.TotalSeconds));
            string finalOutputFile = Path.Combine(tempDirectory, $"{Path.GetFileName(folderPath)}.m4b");

            switch (analysis.RecommendedAction)
            {
                case ProcessingAction.CombineMultipleFiles:
                    reportProgress(progress, ProcessingPhase.Processing, "Combining audio files...");
                    string combinedFile = Path.Combine(tempDirectory, "combined.mp4");
                    await ffmpegService.combineFilesAsync(analysis.AudioFilePaths.ToArray(), folderPath, combinedFile, totalDuration, progress);

                    reportProgress(progress, ProcessingPhase.Processing, "Embedding chapters and metadata...");
                    var chapters = metadataService.createChapters(audioFiles);
                    string chapterFile = await metadataService.createChapterFileAsync(chapters, totalDuration);

                    await ffmpegService.embedChaptersAsync(combinedFile, chapterFile, finalOutputFile);
                    break;

                case ProcessingAction.ConvertSingleFile:
                    reportProgress(progress, ProcessingPhase.Processing, "Converting single file...");
                    await ffmpegService.convertToM4bAsync(analysis.AudioFilePaths[0], finalOutputFile, totalDuration, progress);
                    break;

                case ProcessingAction.AlreadyProcessed:
                case ProcessingAction.NoAudioFilesFound:
                    reportProgress(progress, ProcessingPhase.Completed, analysis.AnalysisMessage);
                    return;
            }

            reportProgress(progress, ProcessingPhase.Finalization, "Finalizing and cleaning up...");
            await fileService.createBackupAsync(folderPath, Path.Combine(folderPath, ".backup"));
            fileService.atomicMove(finalOutputFile, Path.Combine(folderPath, Path.GetFileName(finalOutputFile)));

            // This is the new cleanup loop
            reportProgress(progress, ProcessingPhase.Finalization, "Cleaning up original files...");
            foreach (var originalFile in analysis.AudioFilePaths)
            {
                File.Delete(originalFile);
            }

            reportProgress(progress, ProcessingPhase.Completed, "Processing complete!");
        }
        catch (Exception ex)
        {
            reportProgress(progress, ProcessingPhase.Error, $"An error occurred: {ex.Message}");
            throw;
        }
        finally
        {
            fileService.cleanupTempFiles(tempDirectory);
        }
    }

    public async Task<bool> validateOutputAsync(string filePath)
    {
        return await fileService.validateIntegrityAsync(filePath);
    }
}