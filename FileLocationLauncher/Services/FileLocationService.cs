using FileLocationLauncher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileLocationLauncher.Services
{
    public class FileLocationService : IFileLocationService
    {
        private readonly string _dataFilePath;
        private readonly List<FileLocationModel> _fileLocations;
        private readonly object _lockObject = new object();

        public FileLocationService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "FileLocationOpener");
            Directory.CreateDirectory(appFolder);
            _dataFilePath = Path.Combine(appFolder, "filelocations.json");
            _fileLocations = new List<FileLocationModel>();

            Debug.WriteLine($"Data file path: {_dataFilePath}");

            try
            {
                LoadDataSync();
                Debug.WriteLine($"Loaded {_fileLocations.Count} file locations");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load initial data: {ex.Message}");
            }
        }

        public async Task<IEnumerable<FileLocationModel>> GetAllAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return _fileLocations.OrderByDescending(f => f.LastModified).ToList();
                }
            });
        }

        public async Task<FileLocationModel?> GetByIdAsync(Guid id)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return _fileLocations.FirstOrDefault(f => f.Id == id);
                }
            });
        }

        public async Task<FileLocationModel> AddAsync(FileLocationModel fileLocation)
        {
            return await Task.Run(() =>
            {
                Debug.WriteLine($"Adding file location: {fileLocation.Name}");

                lock (_lockObject)
                {
                    fileLocation.Id = Guid.NewGuid();
                    fileLocation.CreatedDate = DateTime.Now;
                    fileLocation.LastModified = DateTime.Now;
                    _fileLocations.Add(fileLocation);

                    Debug.WriteLine($"Added to memory. Total count: {_fileLocations.Count}");

                    try
                    {
                        SaveDataSync();
                        Debug.WriteLine("Successfully saved to file");
                        return fileLocation;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to save: {ex.Message}");
                        // Remove from memory if save failed
                        _fileLocations.Remove(fileLocation);
                        throw;
                    }
                }
            });
        }

        public async Task<FileLocationModel> UpdateAsync(FileLocationModel fileLocation)
        {
            return await Task.Run(() =>
            {
                Debug.WriteLine($"Updating file location: {fileLocation.Name}");

                lock (_lockObject)
                {
                    var existing = _fileLocations.FirstOrDefault(f => f.Id == fileLocation.Id);
                    if (existing != null)
                    {
                        existing.Name = fileLocation.Name;
                        existing.FilePath = fileLocation.FilePath;
                        existing.Description = fileLocation.Description;
                        existing.ProjectType = fileLocation.ProjectType;
                        existing.Tags = fileLocation.Tags;
                        existing.IsFavorite = fileLocation.IsFavorite;
                        existing.LastModified = DateTime.Now;

                        try
                        {
                            SaveDataSync();
                            Debug.WriteLine("Successfully updated and saved");
                            return existing;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to save update: {ex.Message}");
                            throw;
                        }
                    }
                    throw new ArgumentException("File location not found");
                }
            });
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            return await Task.Run(() =>
            {
                Debug.WriteLine($"Deleting file location: {id}");

                lock (_lockObject)
                {
                    var fileLocation = _fileLocations.FirstOrDefault(f => f.Id == id);
                    if (fileLocation != null)
                    {
                        _fileLocations.Remove(fileLocation);

                        try
                        {
                            SaveDataSync();
                            Debug.WriteLine("Successfully deleted and saved");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Failed to save after delete: {ex.Message}");
                            // Add back to memory if save failed
                            _fileLocations.Add(fileLocation);
                            throw;
                        }
                    }
                    return false;
                }
            });
        }

        public async Task<IEnumerable<FileLocationModel>> SearchAsync(string searchTerm)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    if (string.IsNullOrWhiteSpace(searchTerm))
                        return _fileLocations.OrderByDescending(f => f.LastModified).ToList();

                    var term = searchTerm.ToLower();
                    return _fileLocations.Where(f =>
                        f.Name.ToLower().Contains(term) ||
                        f.Description.ToLower().Contains(term) ||
                        f.ProjectType.ToLower().Contains(term) ||
                        f.Tags.ToLower().Contains(term)
                    ).OrderByDescending(f => f.LastModified).ToList();
                }
            });
        }

        private void LoadDataSync()
        {
            Debug.WriteLine("Loading data from file...");

            if (!File.Exists(_dataFilePath))
            {
                Debug.WriteLine("File does not exist, creating empty list");
                return;
            }

            try
            {
                var json = File.ReadAllText(_dataFilePath);
                Debug.WriteLine($"Read JSON: {json.Substring(0, Math.Min(100, json.Length))}...");

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var loadedData = JsonSerializer.Deserialize<List<FileLocationModel>>(json);
                    if (loadedData != null)
                    {
                        _fileLocations.Clear();

                        // Clean up file paths while loading
                        foreach (var item in loadedData)
                        {
                            CleanUpFilePath(item);
                            _fileLocations.Add(item);
                        }

                        Debug.WriteLine($"Successfully loaded {_fileLocations.Count} items");

                        // Save cleaned data back to file
                        if (loadedData.Any(item => NeedsPathCleanup(item.FilePath)))
                        {
                            Debug.WriteLine("Found paths needing cleanup, saving cleaned data");
                            SaveDataSync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load data: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private void CleanUpFilePath(FileLocationModel fileLocation)
        {
            if (string.IsNullOrEmpty(fileLocation.FilePath))
                return;

            var originalPath = fileLocation.FilePath;
            var cleanedPath = originalPath;

            // Remove "Folder Selection.folder" if it exists
            if (cleanedPath.EndsWith("Folder Selection.folder", StringComparison.OrdinalIgnoreCase))
            {
                cleanedPath = cleanedPath.Replace("Folder Selection.folder", "").TrimEnd('\\', '/');
            }

            // Remove ".folder" extension if it exists
            if (cleanedPath.EndsWith(".folder", StringComparison.OrdinalIgnoreCase))
            {
                var directory = Path.GetDirectoryName(cleanedPath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    cleanedPath = directory;
                }
            }

            // Update the file location if path was cleaned
            if (cleanedPath != originalPath)
            {
                Debug.WriteLine($"Cleaned path: '{originalPath}' -> '{cleanedPath}'");
                fileLocation.FilePath = cleanedPath;
            }
        }

        private bool NeedsPathCleanup(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) &&
                   (filePath.EndsWith("Folder Selection.folder", StringComparison.OrdinalIgnoreCase) ||
                    filePath.EndsWith(".folder", StringComparison.OrdinalIgnoreCase));
        }

        private void SaveDataSync()
        {
            Debug.WriteLine($"Saving {_fileLocations.Count} items to file...");

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(_fileLocations, options);
                Debug.WriteLine($"Serialized JSON length: {json.Length}");

                // Write to temporary file first
                var tempFile = _dataFilePath + ".tmp";
                File.WriteAllText(tempFile, json);
                Debug.WriteLine("Written to temp file");

                // Replace original file
                if (File.Exists(_dataFilePath))
                {
                    File.Delete(_dataFilePath);
                }
                File.Move(tempFile, _dataFilePath);
                Debug.WriteLine("Moved temp file to final location");

                // Verify the file was written correctly
                var verifyJson = File.ReadAllText(_dataFilePath);
                Debug.WriteLine($"Verification read length: {verifyJson.Length}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save data: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public void Dispose()
        {
            // Nothing to dispose in this implementation
        }
    }
}
