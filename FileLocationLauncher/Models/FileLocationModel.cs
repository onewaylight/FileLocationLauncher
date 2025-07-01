using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.Models
{
    public class FileLocationModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string ProjectType { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime LastModified { get; set; } = DateTime.Now;

        public string Tags { get; set; } = string.Empty;

        public bool IsFavorite { get; set; }

        public string FileExtension => System.IO.Path.GetExtension(FilePath);

        public string DirectoryPath => System.IO.Path.GetDirectoryName(FilePath) ?? string.Empty;

        public bool FileExists => System.IO.File.Exists(FilePath) || System.IO.Directory.Exists(FilePath);
    }
}
