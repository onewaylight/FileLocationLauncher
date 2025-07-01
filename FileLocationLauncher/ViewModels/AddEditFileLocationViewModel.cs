using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileLocationLauncher.Models;
using FileLocationLauncher.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileLocationLauncher.ViewModels
{
    public partial class AddEditFileLocationViewModel : ObservableObject
    {
        private readonly IFileLocationService _fileLocationService;
        private readonly IDialogService _dialogService;
        private readonly IProjectTypeService _projectTypeService;

        [ObservableProperty]
        private FileLocationModel fileLocation = new();

        [ObservableProperty]
        private bool isEditMode;

        [ObservableProperty]
        private string windowTitle = "Add File Location";

        [ObservableProperty]
        private bool shouldCloseWindow;

        [ObservableProperty]
        private ObservableCollection<ProjectTypeModel> availableProjectTypes = new();

        [ObservableProperty]
        private ProjectTypeModel? selectedProjectType;

        public AddEditFileLocationViewModel(
            IFileLocationService fileLocationService,
            IDialogService dialogService,
            IProjectTypeService projectTypeService)
        {
            _fileLocationService = fileLocationService;
            _dialogService = dialogService;
            _projectTypeService = projectTypeService;

            LoadProjectTypesAsync();
        }

        partial void OnSelectedProjectTypeChanged(ProjectTypeModel? value)
        {
            if (value != null)
            {
                FileLocation.ProjectType = value.Name;
            }
        }

        private async Task LoadProjectTypesAsync()
        {
            try
            {
                var projectTypes = await _projectTypeService.GetAllProjectTypesAsync();
                AvailableProjectTypes.Clear();

                foreach (var projectType in projectTypes)
                {
                    AvailableProjectTypes.Add(projectType);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync($"Failed to load project types: {ex.Message}");
            }
        }

        public void LoadFileLocation(FileLocationModel model)
        {
            IsEditMode = true;
            WindowTitle = "Edit File Location";
            FileLocation = new FileLocationModel
            {
                Id = model.Id,
                Name = model.Name,
                FilePath = model.FilePath,
                Description = model.Description,
                ProjectType = model.ProjectType,
                Tags = model.Tags,
                IsFavorite = model.IsFavorite,
                CreatedDate = model.CreatedDate,
                LastModified = model.LastModified
            };

            // Set selected project type
            SelectedProjectType = AvailableProjectTypes.FirstOrDefault(p => p.Name == model.ProjectType);
        }

        [RelayCommand]
        private async Task BrowseFileAsync()
        {
            var filePath = await _dialogService.ShowOpenFileDialogAsync(
                "All Files (*.*)|*.*|" +
                "Visual Studio Solution (*.sln)|*.sln|" +
                "Visual Studio Project (*.csproj;*.vbproj)|*.csproj;*.vbproj|" +
                "Code Files (*.cs;*.vb;*.js;*.ts;*.py;*.java)|*.cs;*.vb;*.js;*.ts;*.py;*.java");

            if (!string.IsNullOrEmpty(filePath))
            {
                FileLocation.FilePath = filePath;
                if (string.IsNullOrEmpty(FileLocation.Name))
                {
                    FileLocation.Name = System.IO.Path.GetFileNameWithoutExtension(filePath);
                }

                // Auto-detect project type based on file extension
                await AutoDetectProjectTypeAsync(filePath);

                // Force UI update
                OnPropertyChanged(nameof(FileLocation));
            }
        }

        [RelayCommand]
        private async Task BrowseFolderAsync()
        {
            var folderPath = await _dialogService.ShowOpenFolderDialogAsync();
            if (!string.IsNullOrEmpty(folderPath))
            {
                FileLocation.FilePath = folderPath;
                if (string.IsNullOrEmpty(FileLocation.Name))
                {
                    FileLocation.Name = System.IO.Path.GetFileName(folderPath);
                }

                // Auto-detect project type based on folder contents
                await AutoDetectProjectTypeAsync(folderPath);

                // Force UI update
                OnPropertyChanged(nameof(FileLocation));
            }
        }

        private async Task AutoDetectProjectTypeAsync(string path)
        {
            try
            {
                var extension = System.IO.Path.GetExtension(path).ToLower();
                string detectedType = extension switch
                {
                    ".sln" => "Visual Studio Solution",
                    ".csproj" or ".vbproj" => "Visual Studio Project",
                    ".html" or ".css" or ".js" => "Web Application",
                    ".exe" => "Desktop Application",
                    ".dll" => "Library",
                    ".md" or ".txt" or ".doc" or ".pdf" => "Documentation",
                    ".config" or ".json" or ".xml" => "Configuration",
                    ".bat" or ".ps1" or ".sh" => "Script",
                    ".sql" or ".db" or ".mdf" => "Database",
                    _ => "Other"
                };

                // If it's a folder, check for common project files
                if (System.IO.Directory.Exists(path))
                {
                    if (System.IO.Directory.GetFiles(path, "*.sln").Any())
                        detectedType = "Visual Studio Solution";
                    else if (System.IO.Directory.GetFiles(path, "*.csproj").Any() ||
                             System.IO.Directory.GetFiles(path, "*.vbproj").Any())
                        detectedType = "Visual Studio Project";
                    else if (System.IO.Directory.GetFiles(path, "package.json").Any())
                        detectedType = "Web Application";
                }

                var projectType = AvailableProjectTypes.FirstOrDefault(p => p.Name == detectedType);
                if (projectType != null)
                {
                    SelectedProjectType = projectType;
                }
            }
            catch
            {
                // Auto-detection failed, ignore
            }
        }

        [RelayCommand]
        private async Task AddNewProjectTypeAsync()
        {
            // Simple input dialog for adding new project type
            // In a real application, you'd want a proper dialog
            var newTypeName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new project type name:",
                "Add Project Type",
                "");

            if (!string.IsNullOrWhiteSpace(newTypeName))
            {
                try
                {
                    var newProjectType = new ProjectTypeModel
                    {
                        Name = newTypeName.Trim(),
                        Icon = "📄",
                        Color = "#6C757D",
                        Description = $"Custom project type: {newTypeName}"
                    };

                    await _projectTypeService.AddProjectTypeAsync(newProjectType);
                    await LoadProjectTypesAsync();

                    SelectedProjectType = AvailableProjectTypes.FirstOrDefault(p => p.Name == newTypeName);
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorDialogAsync($"Failed to add project type: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!ValidateModel())
                return;

            try
            {
                if (IsEditMode)
                {
                    await _fileLocationService.UpdateAsync(FileLocation);
                }
                else
                {
                    await _fileLocationService.AddAsync(FileLocation);
                }

                ShouldCloseWindow = true;
                CloseWindow();
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync($"Failed to save: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            CloseWindow();
        }

        private void CloseWindow()
        {
            // Find the window that contains this view model
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.DialogResult = ShouldCloseWindow;
                    window.Close();
                    break;
                }
            }
        }

        private bool ValidateModel()
        {
            var context = new ValidationContext(FileLocation);
            var results = new System.Collections.Generic.List<ValidationResult>();

            if (!Validator.TryValidateObject(FileLocation, context, results, true))
            {
                var errorMessage = string.Join("\n", results.ConvertAll(r => r.ErrorMessage));
                _dialogService.ShowErrorDialogAsync(errorMessage, "Validation Error");
                return false;
            }

            if (string.IsNullOrWhiteSpace(FileLocation.FilePath))
            {
                _dialogService.ShowErrorDialogAsync("File path is required.", "Validation Error");
                return false;
            }

            return true;
        }
    }
}
