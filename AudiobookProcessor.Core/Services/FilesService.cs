using System.IO;
using System.Threading.Tasks;

namespace AudiobookProcessor.Services;

/// <summary>
/// Responsible for safe file handling and validation.
/// </summary>
public class FileService
{
    /// <summary>
    /// Creates a complete, structure-preserving backup of a folder.
    /// This operation is performed on a background thread to keep the UI responsive.
    /// </summary>
    public Task createBackupAsync(string sourceFolderPath, string backupFolderPath)
    {
        return Task.Run(() =>
        {
            // Ensure the root backup directory exists.
            Directory.CreateDirectory(backupFolderPath);

            // Get all files, including those in subdirectories.
            var files = Directory.GetFiles(sourceFolderPath, "*.*", SearchOption.AllDirectories);

            foreach (string sourceFile in files)
            {
                // Determine the file's relative path to maintain the folder structure.
                string relativePath = Path.GetRelativePath(sourceFolderPath, sourceFile);
                string destinationFile = Path.Combine(backupFolderPath, relativePath);

                // Ensure the subdirectory exists in the backup location.
                string destinationDirectory = Path.GetDirectoryName(destinationFile);
                if (destinationDirectory != null)
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                // Copy the file, allowing it to overwrite an existing file.
                File.Copy(sourceFile, destinationFile, true);
            }
        });
    }

    /// <summary>
    /// Safely moves a file from a source path to a destination.
    /// </summary>
    public void atomicMove(string sourcePath, string destinationPath)
    {
        File.Move(sourcePath, destinationPath, true);
    }

    /// <summary>
    /// Verifies the integrity of a processed file.
    /// </summary>
    public Task<bool> validateIntegrityAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            // A file is considered valid if it exists and has a size greater than 0.
            bool isValid = fileInfo.Exists && fileInfo.Length > 0;
            return Task.FromResult(isValid);
        }
        catch (IOException)
        {
            // If any error occurs while accessing the file info, treat it as invalid.
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Cleans up any temporary files or folders.
    /// </summary>
    public void cleanupTempFiles(string tempFolderPath)
    {
        if (Directory.Exists(tempFolderPath))
        {
            Directory.Delete(tempFolderPath, true);
        }
    }
}