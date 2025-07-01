using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileLocationLauncher.Models;
using FileLocationLauncher.Services;
using System;
using System.Collections.Generic;
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

        [ObservableProperty]
        private FileLocationModel fileLocation = new();

        [ObservableProperty]
        private bool isEditMode;

        [ObservableProperty]
        private string windowTitle = "Add File Location";

        [ObservableProperty]
        private bool shouldCloseWindow;

        public AddEditFileLocationViewModel(
            IFileLocationService fileLocationService,
            IDialogService dialogService)
        {
            _fileLocationService = fileLocationService;
            _dialogService = dialogService;
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
