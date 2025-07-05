using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudiobookProcessor.Models;

public class AudioFile
{
    public string filePath { get; set; } //
    public string format { get; set; } //
    public TimeSpan duration { get; set; } //
    public long fileSize { get; set; } //
    public AudioMetadata metadata { get; set; } //
    public List<Chapter> chapters { get; set; } //
}
