using AudiobookProcessor.Models;
using AudiobookProcessor.Services;
using System;
using System.IO;

// Set Window Size
int windowWidth = 120;
int windowHeight = 30;
if (OperatingSystem.IsWindows())
{
    Console.SetWindowSize(windowWidth, windowHeight);
    Console.SetBufferSize(windowWidth, windowHeight);
}


// Instantiate services
var fileService = new FileService();
var ffmpegService = new FFmpegService();
var metadataService = new MetadataService(ffmpegService);
var audioProcessor = new AudioProcessor(fileService, ffmpegService, metadataService);
var consoleLock = new object();

Console.WriteLine("Audiobook Processor Initialized");

while (true) // Main application loop
{
    Console.WriteLine("\nPlease enter the full path to an audiobook folder (or type 'exit' to close):");
    string folderPath = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(folderPath)) continue;

    // This is the new line to clean the input path
    folderPath = folderPath.Trim('"');

    if (folderPath.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

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
            Console.WriteLine();

            if (responseKey == ConsoleKey.Y)
            {
                Console.WriteLine();
                Console.WriteLine("--- Log Output ---");
                int progressBarTop = Console.CursorTop;
                Console.WriteLine();

                var progress = new Progress<ProcessingStatus>(p =>
                {
                    lock (consoleLock)
                    {
                        var (currentLeft, currentTop) = (Console.CursorLeft, Console.CursorTop);

                        Console.SetCursorPosition(0, progressBarTop);
                        Console.CursorVisible = false;

                        int barWidth = 50;
                        int progressBlocks = (int)((p.progressPercentage / 100) * barWidth);
                        string bar = $"[{new string('█', progressBlocks)}{new string('-', barWidth - progressBlocks)}]";
                        string etrString = p.etr > TimeSpan.Zero ? $" (ETR: {p.etr:mm\\:ss})" : "";
                        string line = $"Progress: {bar} {(int)p.progressPercentage}%{etrString}";

                        Console.Write(line.PadRight(Console.WindowWidth - 1));

                        if (!string.IsNullOrEmpty(p.statusMessage))
                        {
                            Console.SetCursorPosition(0, currentTop);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {p.statusMessage}");
                        }
                        else
                        {
                            Console.SetCursorPosition(currentLeft, currentTop);
                        }

                        Console.CursorVisible = true;
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