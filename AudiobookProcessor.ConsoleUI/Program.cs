using AudiobookProcessor.Models;
using AudiobookProcessor.Services;
using System;
using System.IO;

// Instantiate all the services
var fileService = new FileService();
var ffmpegService = new FFmpegService();
var metadataService = new MetadataService(ffmpegService);
var audioProcessor = new AudioProcessor(fileService, ffmpegService, metadataService);

Console.WriteLine("Audiobook Processor Initialized");

while (true) // Main application loop
{
    Console.WriteLine("\nPlease enter the full path to an audiobook folder (or type 'exit' to close):");
    string folderPath = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(folderPath))
    {
        continue;
    }

    if (folderPath.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (Directory.Exists(folderPath))
    {
        var analysis = await audioProcessor.analyzeFolderAsync(folderPath);
        Console.WriteLine(analysis.AnalysisMessage);

        bool canProcess = analysis.RecommendedAction == ProcessingAction.CombineMultipleFiles ||
                          analysis.RecommendedAction == ProcessingAction.ConvertSingleFile;

        if (canProcess)
        {
            Console.Write("Process this folder? (Y/N): ");
            var responseKey = Console.ReadKey(true).Key;

            if (responseKey == ConsoleKey.Y)
            {
                Console.WriteLine("\n"); // Add a space before processing starts

                // Set up progress reporting
                var progress = new Progress<ProcessingStatus>(p =>
                {
                    // If we get a status message, print it as a log line.
                    if (!string.IsNullOrEmpty(p.statusMessage))
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {p.statusMessage}");
                    }

                    // If we get a percentage, draw the progress bar.
                    if (p.progressPercentage > 0)
                    {
                        Console.CursorVisible = false;
                        int barWidth = 50;
                        int progressBlocks = (int)((p.progressPercentage / 100) * barWidth);
                        string bar = $"[{new string('█', progressBlocks)}{new string('-', barWidth - progressBlocks)}]";

                        // Use SetCursorPosition to overwrite the same line for the progress bar
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write($"{bar} {(int)p.progressPercentage}% ");

                        // Reset cursor to the end of the line if it's the final update
                        if (p.progressPercentage >= 100)
                        {
                            Console.WriteLine();
                        }
                    }
                });

                try
                {
                    await audioProcessor.processFolderAsync(folderPath, progress);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n--- ERROR ---\n{ex.Message}\n-------------");
                }
                finally
                {
                    Console.CursorVisible = true;
                }
            }
            else
            {
                Console.WriteLine("\nOperation cancelled.");
            }
        }
    }
    else
    {
        Console.WriteLine("Error: Directory not found. Please check the path and try again.");
    }
}