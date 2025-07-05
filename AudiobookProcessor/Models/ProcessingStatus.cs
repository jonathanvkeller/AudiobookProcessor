using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookProcessor.Models;

// This enum represents the stages of the processing pipeline.
public enum ProcessingPhase
{
    Idle,
    Analysis,
    Validation,
    Processing,
    Finalization,
    Completed,
    Error
}

public class ProcessingStatus
{
    public ProcessingPhase phase { get; set; } //
    public int currentFile { get; set; } //
    public int totalFiles { get; set; } //
    public double progressPercentage { get; set; } //
    public string statusMessage { get; set; } //
    public bool isVerbose { get; set; } //
}
