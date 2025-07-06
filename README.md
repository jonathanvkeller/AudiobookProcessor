# AudiobookProcessor

A brutally simple Windows utility for preparing audiobook files for Jellyfin. Converts messy collections of audio files into clean, single M4B files with preserved metadata and chapters.

## What It Does

- **Multiple MP3/M4A files** → Single M4B with embedded chapters
- **Single non-M4B file** → M4B conversion with metadata preservation
- **Already processed M4B** → Skips and reports clean
- **Preserves all metadata** → Cover art, narrator, series info, chapters
- **Safe processing** → Validates before overwriting, atomic operations

## Why This Exists

Jellyfin wants single audiobook files with embedded chapters. Publishers give you 47 separate MP3 files or weird formats. This tool fixes that mismatch without losing your precious metadata.

## Requirements

- Windows 10/11
- .NET 8.0+ (bundled in standalone exe)
- FFmpeg (bundled)

## Usage

1. Run AudiobookProcessor.exe
2. Select audiobook folder
3. Review what will happen
4. Click "Process Folder"
5. Wait for completion
6. Import into Jellyfin

## Features

- **Brutalist UI** - No fancy nonsense, just functionality
- **Real-time progress** - See exactly what's happening
- **Verbose logging** - Optional detailed output for troubleshooting
- **Fail-fast approach** - Stops on errors rather than corrupting files
- **Metadata preservation** - Keeps all your audiobook info intact
- **Chapter support** - Properly embedded chapters for navigation

## Supported Input Formats

- MP3 (ID3v2 metadata)
- M4A/M4B (iTunes metadata)
- FLAC (Vorbis comments)
- OGG (Vorbis comments)
- WMA (Windows Media metadata)

## Output Format

All files are converted to M4B (MPEG-4 audiobook format) with:

- AAC audio codec
- Embedded chapters
- Preserved metadata
- Cover art
- Proper audiobook tagging

## Development

Built with WinUI 3 and bundled FFmpeg. No external dependencies, no web frameworks, no unnecessary complexity.

## License

MIT License - Use it however you want.

## Contributing

This is a personal utility project. Feel free to fork and modify for your needs. PRs welcome if they maintain the simplicity and brutalist approach.
