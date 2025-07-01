using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileLocationLauncher.Models;
using FileLocationLauncher.Services;
using FileLocationLauncher.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IFileLocationService _fileLocationService;
        private readonly IDialogService _dialogService;
        private readonly IFileOperationService _fileOperationService;
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private ObservableCollection<FileLocationModel> fileLocations = new();

        [ObservableProperty]
        private FileLocationModel? selectedFileLocation;

        [ObservableProperty]
        private string searchText = string.Empty;

        [ObservableProperty]
        private bool isLoading;

        public MainViewModel(
            IFileLocationService fileLocationService,
            IDialogService dialogService,
            IFileOperationService fileOperationService,
            IServiceProvider serviceProvider)
        {
            _fileLocationService = fileLocationService;
            _dialogService = dialogService;
            _fileOperationService = fileOperationService;
            _serviceProvider = serviceProvider;

            LoadFileLocationsAsync();
        }

        [RelayCommand]
        private async Task LoadFileLocationsAsync()
        {
            IsLoading = true;
            try
            {
                var locations = await _fileLocationService.GetAllAsync();
                FileLocations.Clear();
                foreach (var location in locations)
                {
                    FileLocations.Add(location);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync($"Failed to load file locations: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            IsLoading = true;
            try
            {
                var results = await _fileLocationService.SearchAsync(SearchText);
                FileLocations.Clear();
                foreach (var result in results)
                {
                    FileLocations.Add(result);
                }
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync($"Search failed: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void AddFileLocation()
        {
            var addEditWindow = _serviceProvider.GetRequiredService<AddEditFileLocationWindow>();
            var viewModel = _serviceProvider.GetRequiredService<AddEditFileLocationViewModel>();
            addEditWindow.DataContext = viewModel;

            if (addEditWindow.ShowDialog() == true)
            {
                LoadFileLocationsAsync();
            }
        }

        [RelayCommand]
        private void EditFileLocation(FileLocationModel? fileLocation)
        {
            if (fileLocation == null) return;

            var addEditWindow = _serviceProvider.GetRequiredService<AddEditFileLocationWindow>();
            var viewModel = _serviceProvider.GetRequiredService<AddEditFileLocationViewModel>();
            viewModel.LoadFileLocation(fileLocation);
            addEditWindow.DataContext = viewModel;

            if (addEditWindow.ShowDialog() == true)
            {
                LoadFileLocationsAsync();
            }
        }

        [RelayCommand]
        private async Task DeleteFileLocationAsync(FileLocationModel? fileLocation)
        {
            if (fileLocation == null) return;

            var confirmed = await _dialogService.ShowConfirmationDialogAsync(
                $"Are you sure you want to delete '{fileLocation.Name}'?",
                "Delete File Location");

            if (confirmed)
            {
                try
                {
                    await _fileLocationService.DeleteAsync(fileLocation.Id);
                    FileLocations.Remove(fileLocation);
                }
                catch (Exception ex)
                {
                    await _dialogService.ShowErrorDialogAsync($"Failed to delete file location: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task OpenFileLocationAsync(FileLocationModel? fileLocation)
        {
            if (fileLocation == null) return;

            var success = await _fileOperationService.OpenFileOrFolderAsync(fileLocation.FilePath);
            if (!success)
            {
                await _dialogService.ShowErrorDialogAsync(
                    $"Could not open '{fileLocation.FilePath}'. The file or folder may not exist.",
                    "Open Failed");
            }
        }

        [RelayCommand]
        private async Task OpenInExplorerAsync(FileLocationModel? fileLocation)
        {
            if (fileLocation == null) return;

            var success = await _fileOperationService.OpenInExplorerAsync(fileLocation.FilePath);
            if (!success)
            {
                await _dialogService.ShowErrorDialogAsync(
                    "Could not open file location in Explorer.",
                    "Explorer Failed");
            }
        }

        [RelayCommand]
        private async Task ToggleFavoriteAsync(FileLocationModel? fileLocation)
        {
            if (fileLocation == null) return;

            try
            {
                fileLocation.IsFavorite = !fileLocation.IsFavorite;
                await _fileLocationService.UpdateAsync(fileLocation);
            }
            catch (Exception ex)
            {
                await _dialogService.ShowErrorDialogAsync($"Failed to update favorite status: {ex.Message}");
            }
        }
    }
}
