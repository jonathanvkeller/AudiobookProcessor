using AudiobookProcessor.Models;
using AudiobookProcessor.Services;
using Microsoft.UI.Xaml;
using System;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace AudiobookProcessor
{
    public sealed partial class MainWindow : Window
    {
        private readonly FileService fileService;
        private readonly FFmpegService ffmpegService;
        private readonly MetadataService metadataService;
        private readonly AudioProcessor audioProcessor;

        private string selectedFolderPath;

        public MainWindow()
        {
            this.InitializeComponent();

            this.fileService = new FileService();
            this.ffmpegService = new FFmpegService();
            this.metadataService = new MetadataService(this.ffmpegService);
            this.audioProcessor = new AudioProcessor(this.fileService, this.ffmpegService, this.metadataService);
        }

        private async void selectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Downloads;
            folderPicker.FileTypeFilter.Add("*");

            var windowHandle = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(folderPicker, windowHandle);

            var folder = await folderPicker.PickSingleFolderAsync();

            if (folder != null)
            {
                selectedFolderPath = folder.Path;
                folderPathTextBlock.Text = selectedFolderPath;

                var analysis = await audioProcessor.analyzeFolderAsync(selectedFolderPath);
                analysisResultTextBlock.Text = analysis.AnalysisMessage;

                bool canProcess = analysis.RecommendedAction == ProcessingAction.CombineMultipleFiles ||
                                  analysis.RecommendedAction == ProcessingAction.ConvertSingleFile;

                processButton.IsEnabled = canProcess;
            }
        }

        private async void processButton_Click(object sender, RoutedEventArgs e)
        {
            selectFolderButton.IsEnabled = false;
            processButton.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = false; // Set to determinate
            logTextBox.Text = "";
            logTextBox.Visibility = Visibility.Visible;

            var progress = new Progress<ProcessingStatus>(p =>
            {
                // Update both the text and the progress bar value
                if (!string.IsNullOrEmpty(p.statusMessage))
                {
                    logTextBox.Text += $"{p.statusMessage}\n";
                }
                if (p.progressPercentage > 0)
                {
                    progressBar.Value = p.progressPercentage;
                }
            });

            try
            {
                await audioProcessor.processFolderAsync(selectedFolderPath, progress);
            }
            catch (Exception ex)
            {
                logTextBox.Text += $"\nERROR: {ex.Message}";
            }
            finally
            {
                selectFolderButton.IsEnabled = true;
                processButton.IsEnabled = true;
                progressBar.Visibility = Visibility.Collapsed;
                progressBar.IsIndeterminate = true; // Reset for next run
                progressBar.Value = 0;
            }
        }
    }
}