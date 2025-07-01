using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.Services
{
    public interface IDialogService
    {
        Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*");
        Task<string?> ShowOpenFolderDialogAsync();
        Task<bool> ShowConfirmationDialogAsync(string message, string title = "Confirmation");
        Task ShowErrorDialogAsync(string message, string title = "Error");
        Task ShowInfoDialogAsync(string message, string title = "Information");
    }
}
