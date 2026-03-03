using System.Collections.Generic;

namespace Framework.Editor.Utils
{
    public abstract class FileTypeFilterUtility
    {
        // 文件类型筛选
        public enum FileTypeFilter
        {
            All,
            Images,
            TextFiles,
            Audio,
            Videos,
            Table,
            Scripts,
            Unity,
            Archive,
            Executable
        }

        // 文件类型映射
        private static Dictionary<FileTypeFilter, string> _fileTypeExtensions = new()
        {
            { FileTypeFilter.All, "*.*" },
            { FileTypeFilter.Images, "*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tga;*.psd;*.tif;*.tiff" },
            {
                FileTypeFilter.TextFiles,
                "*.txt;*.json;*.xml;*.yml;*.yaml;*.md;*.html;*.doc;*.doc;*.docx;*.pdf;*.ppt;*.pptx"
            },
            { FileTypeFilter.Audio, "*.mp3;*.wav;*.ogg;*.aac;*.aif;*.flac;*.ape;*.aiff;*.mid;*.amr;*.silk12;" },
            { FileTypeFilter.Videos, "*.mp4;*.mov;*.avi;*.mpg;*.mpeg;*.mkv;*.wmv;*.webm;" },
            { FileTypeFilter.Table, "*.csv;*.xlsx;*.xls;" },
            { FileTypeFilter.Scripts, "*.cs;*.js;*.py;*.cpp;*.py;*.h;*.java;*.lua;*.shader;*.cgi;*.hlsl;" },
            { FileTypeFilter.Unity, "*.prefab;*.unity;*.asset;*.mat;*.fbx;*.obj;" },
            { FileTypeFilter.Archive, "*.zip;*.rar;*.7z;*.tar;*.gz;" },
            { FileTypeFilter.Executable, "*.exe;*.dlls;*.msi;*.bat;*.sh;" },
        };

        /// <summary>
        /// 获取指定类型的后缀列表
        /// </summary>
        public static string GetFilterExtensionsByType(FileTypeFilter typeFilter)
        {
            return _fileTypeExtensions.GetValueOrDefault(typeFilter, "");
        }
    }
}