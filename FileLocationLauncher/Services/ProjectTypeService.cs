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
    public class ProjectTypeService : IProjectTypeService
    {
        private readonly string _configFilePath;
        private readonly List<ProjectTypeModel> _projectTypes;
        private readonly object _lockObject = new object();

        public ProjectTypeService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "FileLocationOpener");
            Directory.CreateDirectory(appFolder);
            _configFilePath = Path.Combine(appFolder, "project-types.json");
            _projectTypes = new List<ProjectTypeModel>();

            LoadProjectTypesSync();
        }

        public async Task<IEnumerable<ProjectTypeModel>> GetAllProjectTypesAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return _projectTypes.OrderBy(p => p.Name).ToList();
                }
            });
        }

        public async Task<ProjectTypeModel?> GetProjectTypeByNameAsync(string name)
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return _projectTypes.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                }
            });
        }

        public async Task AddProjectTypeAsync(ProjectTypeModel projectType)
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    if (!_projectTypes.Any(p => p.Name.Equals(projectType.Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        _projectTypes.Add(projectType);
                        SaveProjectTypesSync();
                    }
                }
            });
        }

        public async Task UpdateProjectTypeAsync(ProjectTypeModel projectType)
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    var existing = _projectTypes.FirstOrDefault(p => p.Name.Equals(projectType.Name, StringComparison.OrdinalIgnoreCase));
                    if (existing != null)
                    {
                        existing.Icon = projectType.Icon;
                        existing.Color = projectType.Color;
                        existing.Description = projectType.Description;
                        SaveProjectTypesSync();
                    }
                }
            });
        }

        public async Task DeleteProjectTypeAsync(string name)
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    var projectType = _projectTypes.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (projectType != null)
                    {
                        _projectTypes.Remove(projectType);
                        SaveProjectTypesSync();
                    }
                }
            });
        }

        private void LoadProjectTypesSync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<ProjectTypeConfiguration>(json);

                    if (config?.ProjectTypes != null)
                    {
                        _projectTypes.Clear();
                        _projectTypes.AddRange(config.ProjectTypes);
                        Debug.WriteLine($"Loaded {_projectTypes.Count} project types from file");
                        return;
                    }
                }

                // Create default project types if file doesn't exist
                CreateDefaultProjectTypes();
                SaveProjectTypesSync();
                Debug.WriteLine("Created default project types");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load project types: {ex.Message}");
                CreateDefaultProjectTypes();
            }
        }

        private void CreateDefaultProjectTypes()
        {
            _projectTypes.Clear();
            _projectTypes.AddRange(new[]
            {
                new ProjectTypeModel { Name = "Visual Studio Solution", Icon = "🏗️", Color = "#007ACC", Description = "Visual Studio solution files" },
                new ProjectTypeModel { Name = "Visual Studio Project", Icon = "📁", Color = "#007ACC", Description = "Visual Studio project files" },
                new ProjectTypeModel { Name = "Web Application", Icon = "🌐", Color = "#61DAFB", Description = "Web applications and websites" },
                new ProjectTypeModel { Name = "Desktop Application", Icon = "🖥️", Color = "#0078D4", Description = "Desktop applications" },
                new ProjectTypeModel { Name = "Mobile Application", Icon = "📱", Color = "#A4C639", Description = "Mobile apps for iOS/Android" },
                new ProjectTypeModel { Name = "Library", Icon = "📚", Color = "#FF6B35", Description = "Code libraries and frameworks" },
                new ProjectTypeModel { Name = "Documentation", Icon = "📝", Color = "#6C757D", Description = "Documentation and guides" },
                new ProjectTypeModel { Name = "Configuration", Icon = "⚙️", Color = "#FFC107", Description = "Configuration files and settings" },
                new ProjectTypeModel { Name = "Script", Icon = "📜", Color = "#28A745", Description = "Scripts and automation" },
                new ProjectTypeModel { Name = "Database", Icon = "🗄️", Color = "#DC3545", Description = "Database files and schemas" },
                new ProjectTypeModel { Name = "API", Icon = "🔗", Color = "#17A2B8", Description = "API projects and services" },
                new ProjectTypeModel { Name = "Other", Icon = "📄", Color = "#6C757D", Description = "Other file types" }
            });
        }

        private void SaveProjectTypesSync()
        {
            try
            {
                var config = new ProjectTypeConfiguration
                {
                    ProjectTypes = _projectTypes
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var json = JsonSerializer.Serialize(config, options);
                File.WriteAllText(_configFilePath, json);
                Debug.WriteLine($"Saved {_projectTypes.Count} project types to file");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save project types: {ex.Message}");
            }
        }
    }
}
