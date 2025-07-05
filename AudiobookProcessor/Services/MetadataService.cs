using AudiobookProcessor.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AudiobookProcessor.Services;

public class MetadataService
{
    private readonly FFmpegService ffmpegService;

    public MetadataService(FFmpegService ffmpegService)
    {
        this.ffmpegService = ffmpegService;
    }

    /// <summary>
    /// Gets a fully populated AudioFile object, including duration and metadata, from a file path.
    /// </summary>
    public async Task<AudioFile> getAudioFileAsync(string filePath)
    {
        var jsonString = await ffmpegService.extractMetadataAsync(filePath);

        if (string.IsNullOrEmpty(jsonString))
        {
            throw new InvalidDataException($"FFprobe returned no data for file: {filePath}");
        }

        var audioFile = new AudioFile { FilePath = filePath };
        JsonNode jsonNode = JsonNode.Parse(jsonString);

        // Get format info (duration, format name)
        JsonNode formatNode = jsonNode?["format"];
        if (formatNode != null)
        {
            audioFile.Format = formatNode["format_name"]?.GetValue<string>();
            if (decimal.TryParse(formatNode["duration"]?.GetValue<string>(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal durationSeconds))
            {
                audioFile.Duration = TimeSpan.FromSeconds((double)durationSeconds);
            }
        }

        // Get metadata tags
        JsonNode tags = formatNode?["tags"];
        if (tags != null)
        {
            audioFile.Metadata = new AudioMetadata
            {
                title = tags["title"]?.GetValue<string>(),
                author = tags["artist"]?.GetValue<string>(),
                narrator = tags["composer"]?.GetValue<string>()
            };
        }

        return audioFile;
    }

    // ... rest of the methods remain the same
    public AudioMetadata mergeMetadata(IEnumerable<AudioMetadata> metadataSources)
    {
        var merged = new AudioMetadata();
        merged.title = metadataSources.FirstOrDefault(m => !string.IsNullOrEmpty(m.title))?.title;
        merged.author = metadataSources.FirstOrDefault(m => !string.IsNullOrEmpty(m.author))?.author;
        merged.narrator = metadataSources.FirstOrDefault(m => !string.IsNullOrEmpty(m.narrator))?.narrator;
        return merged;
    }

    public List<Chapter> createChapters(IEnumerable<AudioFile> audioFiles)
    {
        var chapters = new List<Chapter>();
        var runningTime = TimeSpan.Zero;

        foreach (var file in audioFiles)
        {
            var chapter = new Chapter
            {
                title = Path.GetFileNameWithoutExtension(file.filePath),
                startTime = runningTime
            };
            chapters.Add(chapter);
            runningTime += file.duration;
        }

        return chapters;
    }

    public async Task<string> createChapterFileAsync(IEnumerable<Chapter> chapters, TimeSpan totalDuration)
    {
        var content = new StringBuilder();
        content.AppendLine(";FFMETADATA1");

        var chapterList = chapters.ToList();
        for (int i = 0; i < chapterList.Count; i++)
        {
            var chapter = chapterList[i];
            long startTimeMs = (long)chapter.startTime.TotalMilliseconds;
            long endTimeMs;

            if (i < chapterList.Count - 1)
            {
                endTimeMs = (long)chapterList[i + 1].startTime.TotalMilliseconds;
            }
            else
            {
                endTimeMs = (long)totalDuration.TotalMilliseconds;
            }

            content.AppendLine("[CHAPTER]");
            content.AppendLine("TIMEBASE=1/1000");
            content.AppendLine($"START={startTimeMs}");
            content.AppendLine($"END={endTimeMs}");
            content.AppendLine($"title={chapter.title}");
        }

        string tempFilePath = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFilePath, content.ToString());

        return tempFilePath;
    }
}