using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FileLocationLauncher.Services
{
    public class DialogService : IDialogService
    {
        public async Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*")
        {
            return await Task.Run(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = filter,
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                return dialog.ShowDialog() == true ? dialog.FileName : null;
            });
        }

        public async Task<string?> ShowOpenFolderDialogAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Try to use Windows Vista+ folder dialog
                    return ShowFolderDialogVista();
                }
                catch
                {
                    // Fallback to OpenFileDialog workaround
                    return ShowFolderDialogFallback();
                }
            });
        }

        private string? ShowFolderDialogVista()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                ValidateNames = false,
                FileName = "Folder Selection",
                Filter = "Folders|*.folder",
                Title = "Select Folder"
            };

            // Use reflection to enable folder selection mode
            var dialogType = dialog.GetType();
            var optionsField = dialogType.GetField("_options",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (optionsField != null)
            {
                var options = (uint)optionsField.GetValue(dialog)!;
                options |= 0x20; // FOS_PICKFOLDERS
                optionsField.SetValue(dialog, options);
            }

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = dialog.FileName;

                // Clean up the path
                if (selectedPath.EndsWith("Folder Selection"))
                {
                    return System.IO.Path.GetDirectoryName(selectedPath);
                }

                return selectedPath;
            }

            return null;
        }

        private string? ShowFolderDialogFallback()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = false,
                CheckPathExists = true,
                ValidateNames = false,
                Title = "Select any file in the desired folder",
                Filter = "All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                return System.IO.Path.GetDirectoryName(dialog.FileName);
            }

            return null;
        }

        public async Task<bool> ShowConfirmationDialogAsync(string message, string title = "Confirmation")
        {
            return await Task.FromResult(
                MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
            );
        }

        public async Task ShowErrorDialogAsync(string message, string title = "Error")
        {
            await Task.Run(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error)
            );
        }

        public async Task ShowInfoDialogAsync(string message, string title = "Information")
        {
            await Task.Run(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information)
            );
        }
    }

    // Alternative implementation using Windows Shell32 API directly
    public class DialogServiceWithNativeApi : IDialogService
    {
        [ComImport]
        [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialog
        {
        }

        [ComImport]
        [Guid("42f14213-2c10-11d2-9cc6-00c04fb1763e")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig]
            int Show([In] IntPtr parent);
            void SetFileTypes([In] uint cFileTypes, [In] IntPtr rgFilterSpec);
            void SetFileTypeIndex([In] uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);
            void Unadvise([In] uint dwCookie);
            void SetOptions([In] uint fos);
            void GetOptions(out uint pfos);
            void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc,
                [In] ref Guid bhid,
                [In] ref Guid riid,
                out IntPtr ppv);
            void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            void GetDisplayName([In] uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);
            void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
        }

        [ComImport]
        [Guid("973510DB-7D7F-452B-8975-74A85828D354")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialogEvents
        {
        }

        private const uint FOS_PICKFOLDERS = 0x20;
        private const uint SIGDN_FILESYSPATH = 0x80058000;

        public async Task<string?> ShowOpenFileDialogAsync(string filter = "All Files (*.*)|*.*")
        {
            return await Task.Run(() =>
            {
                var dialog = new OpenFileDialog
                {
                    Filter = filter,
                    CheckFileExists = true,
                    CheckPathExists = true
                };

                return dialog.ShowDialog() == true ? dialog.FileName : null;
            });
        }

        public async Task<string?> ShowOpenFolderDialogAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var dialog = new FileOpenDialog() as IFileDialog;
                    if (dialog != null)
                    {
                        dialog.SetOptions(FOS_PICKFOLDERS);
                        dialog.SetTitle("Select Folder");

                        var result = dialog.Show(IntPtr.Zero);
                        if (result == 0) // S_OK
                        {
                            dialog.GetResult(out IShellItem shellItem);
                            shellItem.GetDisplayName(SIGDN_FILESYSPATH, out string path);
                            return path;
                        }
                    }
                }
                catch
                {
                    // Fallback to simple dialog
                }

                // Fallback
                var fallbackDialog = new OpenFileDialog
                {
                    CheckFileExists = false,
                    CheckPathExists = true,
                    ValidateNames = false,
                    Title = "Select any file in the desired folder",
                    Filter = "All files (*.*)|*.*"
                };

                if (fallbackDialog.ShowDialog() == true)
                {
                    return System.IO.Path.GetDirectoryName(fallbackDialog.FileName);
                }

                return null;
            });
        }

        public async Task<bool> ShowConfirmationDialogAsync(string message, string title = "Confirmation")
        {
            return await Task.FromResult(
                MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes
            );
        }

        public async Task ShowErrorDialogAsync(string message, string title = "Error")
        {
            await Task.Run(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error)
            );
        }

        public async Task ShowInfoDialogAsync(string message, string title = "Information")
        {
            await Task.Run(() =>
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information)
            );
        }
    }
}
