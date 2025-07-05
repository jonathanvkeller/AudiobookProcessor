using AudiobookProcessor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AudiobookProcessor.Services;

/// <summary>
/// Orchestrates the entire audiobook processing pipeline.
/// </summary>
public class AudioProcessor
{
    private readonly FileService fileService;
    private readonly FFmpegService ffmpegService;
    private readonly MetadataService metadataService;
    private readonly List<string> supportedExtensions = new()
        { ".mp3", ".m4a", ".m4b", ".flac", ".ogg", ".wma" }; //

    public AudioProcessor(
        FileService fileService,
        FFmpegService ffmpegService,
        MetadataService metadataService)
    {
        this.fileService = fileService;
        this.ffmpegService = ffmpegService;
        this.metadataService = metadataService;
    }

    private void reportProgress(IProgress<ProcessingStatus> progress, ProcessingPhase phase, string message)
    {
        progress.Report(new ProcessingStatus { Phase = phase, StatusMessage = message });
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

    /// <summary>
    /// Executes the full conversion pipeline on a given folder, reporting progress.
    /// </summary>
    public async Task processFolderAsync(string folderPath, IProgress<ProcessingStatus> progress)
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"AudiobookProcessor_{Path.GetRandomFileName()}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // Phase 1: Analysis
            reportProgress(progress, ProcessingPhase.Analysis, "Analyzing folder...");
            var analysis = await analyzeFolderAsync(folderPath);

            string finalOutputFile = Path.Combine(tempDirectory, $"{Path.GetFileName(folderPath)}.m4b");

            switch (analysis.RecommendedAction)
            {
                case ProcessingAction.CombineMultipleFiles:
                    // Phase 2 & 3: Validation and Processing
                    reportProgress(progress, ProcessingPhase.Processing, "Gathering file information...");
                    var audioFiles = new List<AudioFile>();
                    foreach (var path in analysis.AudioFilePaths)
                    {
                        audioFiles.Add(await metadataService.getAudioFileAsync(path));
                    }

                    reportProgress(progress, ProcessingPhase.Processing, "Combining audio files...");
                    string combinedFile = Path.Combine(tempDirectory, "combined.tmp");
                    await ffmpegService.combineFilesAsync(analysis.AudioFilePaths.ToArray(), combinedFile);

                    reportProgress(progress, ProcessingPhase.Processing, "Embedding chapters and metadata...");
                    var totalDuration = TimeSpan.FromSeconds(audioFiles.Sum(f => f.Duration.TotalSeconds));
                    var chapters = metadataService.createChapters(audioFiles);
                    string chapterFile = await metadataService.createChapterFileAsync(chapters, totalDuration);

                    await ffmpegService.embedChaptersAsync(combinedFile, chapterFile, finalOutputFile);
                    break;

                case ProcessingAction.ConvertSingleFile:
                    // Phase 2 & 3: Validation and Processing
                    reportProgress(progress, ProcessingPhase.Processing, "Converting single file...");
                    await ffmpegService.convertToM4bAsync(analysis.AudioFilePaths[0], finalOutputFile);
                    break;

                case ProcessingAction.AlreadyProcessed:
                case ProcessingAction.NoAudioFilesFound:
                    reportProgress(progress, ProcessingPhase.Completed, analysis.AnalysisMessage);
                    return; // Nothing to do
            }

            // Phase 4: Finalization
            reportProgress(progress, ProcessingPhase.Finalization, "Finalizing and cleaning up...");
            await fileService.createBackupAsync(folderPath, Path.Combine(folderPath, ".backup"));
            fileService.atomicMove(finalOutputFile, Path.Combine(folderPath, Path.GetFileName(finalOutputFile)));

            reportProgress(progress, ProcessingPhase.Completed, "Processing complete!");
        }
        catch (Exception ex)
        {
            reportProgress(progress, ProcessingPhase.Error, $"An error occurred: {ex.Message}");
            throw; // Re-throw the exception to be handled by the UI layer
        }
        finally
        {
            // Cleanup
            fileService.cleanupTempFiles(tempDirectory);
        }
    }

    public async Task<bool> validateOutputAsync(string filePath)
    {
        return await fileService.validateIntegrityAsync(filePath);
    }
}