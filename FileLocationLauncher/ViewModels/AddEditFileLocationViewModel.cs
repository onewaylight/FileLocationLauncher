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

            // Initialize with empty FileLocation
            FileLocation = new FileLocationModel();

            // Load project types asynchronously
            _ = Task.Run(async () => await LoadProjectTypesAsync());
        }

        partial void OnSelectedProjectTypeChanged(ProjectTypeModel? value)
        {
            if (value != null)
            {
                FileLocation.ProjectType = value.Name;
            }
        }

        // Handle when text is manually entered in ComboBox
        partial void OnFileLocationChanged(FileLocationModel? value)
        {
            if (value != null && !string.IsNullOrEmpty(value.ProjectType))
            {
                try
                {
                    // Update selected project type when FileLocation.ProjectType changes
                    var matchingType = AvailableProjectTypes?.FirstOrDefault(p => p.Name == value.ProjectType);
                    if (matchingType != null && SelectedProjectType != matchingType)
                    {
                        SelectedProjectType = matchingType;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OnFileLocationChanged error: {ex.Message}");
                }
            }
        }

        private async Task LoadProjectTypesAsync()
        {
            try
            {
                var projectTypes = await _projectTypeService.GetAllProjectTypesAsync();

                // Must update UI on main thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    AvailableProjectTypes.Clear();

                    foreach (var projectType in projectTypes)
                    {
                        AvailableProjectTypes.Add(projectType);
                    }
                });
            }
            catch (Exception ex)
            {
                // Log the error and show user-friendly message
                System.Diagnostics.Debug.WriteLine($"Failed to load project types: {ex.Message}");

                Application.Current.Dispatcher.Invoke(async () =>
                {
                    await _dialogService.ShowErrorDialogAsync($"Failed to load project types: {ex.Message}");
                });
            }
        }

        public async void LoadFileLocation(FileLocationModel model)
        {
            IsEditMode = true;
            WindowTitle = "Edit File Location";

            // Wait for project types to load if they haven't already
            if (!AvailableProjectTypes.Any())
            {
                await LoadProjectTypesAsync();
            }

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

            // Set selected project type after FileLocation is set
            await Task.Delay(50); // Small delay to ensure UI is ready
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
            try
            {
                // Simple input dialog using MessageBox (you could create a proper dialog later)
                var result = await Task.Run(() =>
                {
                    return System.Windows.MessageBox.Show(
                        "Enter new project type name in the combo box and it will be saved automatically.",
                        "Add Project Type",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AddNewProjectType error: {ex.Message}");
                await _dialogService.ShowErrorDialogAsync($"Failed to add project type: {ex.Message}");
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
            // Manually validate required fields
            if (string.IsNullOrWhiteSpace(FileLocation.Name))
            {
                _dialogService.ShowErrorDialogAsync("Name is required.", "Validation Error");
                return false;
            }

            if (FileLocation.Name.Length > 100)
            {
                _dialogService.ShowErrorDialogAsync("Name cannot exceed 100 characters.", "Validation Error");
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
