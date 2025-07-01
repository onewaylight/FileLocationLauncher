using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.Models
{
    public partial class FileLocationModel : ObservableObject
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [ObservableProperty]
        private string name = string.Empty;

        [ObservableProperty]
        private string filePath = string.Empty;

        [ObservableProperty]
        private string description = string.Empty;

        [ObservableProperty]
        private string projectType = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastModified { get; set; } = DateTime.Now;

        [ObservableProperty]
        private string tags = string.Empty;

        [ObservableProperty]
        private bool isFavorite;

        public string FileExtension => System.IO.Path.GetExtension(FilePath);

        public string DirectoryPath => System.IO.Path.GetDirectoryName(FilePath) ?? string.Empty;

        public bool FileExists => System.IO.File.Exists(FilePath) || System.IO.Directory.Exists(FilePath);
    }
}
