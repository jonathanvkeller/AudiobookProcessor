using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AudiobookProcessor.Models;

public enum ProcessingAction
{
    CombineMultipleFiles,
    ConvertSingleFile,
    AlreadyProcessed,
    NoAudioFilesFound,
}

public class FolderAnalysis
{
    public List<string> AudioFilePaths { get; set; } = new();
    public ProcessingAction RecommendedAction { get; set; }
    public string AnalysisMessage { get; set; }
}
