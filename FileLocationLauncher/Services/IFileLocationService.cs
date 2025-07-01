using FileLocationLauncher.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.Services
{
    public interface IFileLocationService
    {
        Task<IEnumerable<FileLocationModel>> GetAllAsync();
        Task<FileLocationModel?> GetByIdAsync(Guid id);
        Task<FileLocationModel> AddAsync(FileLocationModel fileLocation);
        Task<FileLocationModel> UpdateAsync(FileLocationModel fileLocation);
        Task<bool> DeleteAsync(Guid id);
        Task<IEnumerable<FileLocationModel>> SearchAsync(string searchTerm);
    }
}
