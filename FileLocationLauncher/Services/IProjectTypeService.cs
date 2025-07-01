using FileLocationLauncher.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.Services
{
    public interface IProjectTypeService
    {
        Task<IEnumerable<ProjectTypeModel>> GetAllProjectTypesAsync();
        Task<ProjectTypeModel?> GetProjectTypeByNameAsync(string name);
        Task AddProjectTypeAsync(ProjectTypeModel projectType);
        Task UpdateProjectTypeAsync(ProjectTypeModel projectType);
        Task DeleteProjectTypeAsync(string name);
    }
}
