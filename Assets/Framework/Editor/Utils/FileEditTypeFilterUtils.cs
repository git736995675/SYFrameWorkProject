using System.Collections.Generic;

namespace Framework.Editor.Utils
{
    public class FileEditTypeFilterUtils 
    {
    
        // 文件类型筛选
        private enum FileTypeFilter
        {
            All,
            Images,
            Videos,
            Scripts,
            TextFiles,
            Table,
            Prefabs,
            Materials,
            Custom
        }
    
    
        // 文件类型映射
        private Dictionary<FileTypeFilter, string[]> _fileTypeExtensions = new Dictionary<FileTypeFilter, string[]>()
        {
            { FileTypeFilter.All, new string[] { ".*" } },
            { FileTypeFilter.Images, new string[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga", ".tif", ".tiff", ".psd", ".gif" } },
            { FileTypeFilter.Videos, new string[] { ".mp4", ".mov", ".avi", ".mpg", ".mpeg", ".wmv", ".webm" } },
            { FileTypeFilter.Scripts, new string[] { ".cs", ".js", ".shader", ".cgi", ".hlsl" } },
            { FileTypeFilter.TextFiles, new string[] { ".txt", ".json", ".xml", ".yml", ".yaml", ".md", ".html", ".csv" } },
            { FileTypeFilter.Table, new string[] { ".csv", ".xlsx", ".xls" } },
            { FileTypeFilter.Prefabs, new string[] { ".prefab" } },
            { FileTypeFilter.Materials, new string[] { ".mat" } },
            { FileTypeFilter.Custom, new string[] { } }
        };
   
    }
}
