using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileLocationLauncher.Models
{
    public class ProjectTypeModel
    {
        public string Name { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "#007ACC";
        public string Description { get; set; } = string.Empty;
    }

    public class ProjectTypeConfiguration
    {
        public List<ProjectTypeModel> ProjectTypes { get; set; } = new List<ProjectTypeModel>();
    }
}
