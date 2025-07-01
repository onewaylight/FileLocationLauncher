using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.Services
{
    public interface IFileOperationService
    {
        Task<bool> OpenFileOrFolderAsync(string path);
        Task<bool> OpenInExplorerAsync(string path);
        Task<bool> OpenWithDefaultAppAsync(string filePath);
    }
}
