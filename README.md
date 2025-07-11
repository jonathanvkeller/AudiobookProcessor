# Audiobook Processor

A brutally simple console utility for preparing audiobook files for Jellyfin. It converts messy collections of audio files into clean, single M4B files with preserved metadata and chapters.

## What It Does

- **Multiple audio files** → Single M4B file with preserved metadata and embedded chapters.
- **Single non-M4B file** → M4B conversion with metadata preservation.
- **Safe processing** → Backs up original files before processing begins.
- **Preserves Metadata** → Keeps key information like title and author intact.

## Why This Exists

Jellyfin prefers single audiobook files with embedded chapters. Publishers often provide folders with dozens of separate MP3 or M4A files. This tool fixes that mismatch without losing your precious metadata.

## Features

- **Brutalist Console UI** - No fancy nonsense, just functional, information-dense output.
- **Real-time Progress** - A live-updating progress bar and ETR timer show the status of long encoding operations.
- **Verbose Logging** - The application prints each step of the process to the console with timestamps.
- **Safe by Default** - Always creates a `.backup` folder of your original files before making any changes.
- **Robust Path Handling** - Automatically handles paths that are pasted with or without surrounding quotes.

## Requirements

- Windows (64-bit)
- FFmpeg and FFprobe (must be in the same folder as the executable). The application is bundled with the required versions.

## Usage

This is a console application. After publishing the `AudiobookProcessor.ConsoleUI` project, you can run it from a terminal (like Windows Terminal or PowerShell).

1. Navigate to the publish directory in your terminal.
2. Run the application, passing the path to your audiobook folder as an argument. Make sure to wrap the path in quotes if it contains spaces.

```powershell
.\AudiobookProcessor.ConsoleUI.exe "C:\Path\To Your\Audiobook Folder"
