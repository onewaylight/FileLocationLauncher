using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.Services
{
    public class FileOperationService : IFileOperationService
    {
        public async Task<bool> OpenFileOrFolderAsync(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Path.Exists(path))
                    return false;

                await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> OpenInExplorerAsync(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return false;

                await Task.Run(() =>
                {
                    if (File.Exists(path))
                    {
                        Process.Start("explorer.exe", $"/select,\"{path}\"");
                    }
                    else if (Directory.Exists(path))
                    {
                        Process.Start("explorer.exe", $"\"{path}\"");
                    }
                    else
                    {
                        var directory = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        {
                            Process.Start("explorer.exe", $"\"{directory}\"");
                        }
                    }
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> OpenWithDefaultAppAsync(string filePath)
        {
            return await OpenFileOrFolderAsync(filePath);
        }
    }
}
