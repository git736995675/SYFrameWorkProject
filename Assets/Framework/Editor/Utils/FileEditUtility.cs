using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Framework.Editor.Utils
{
    public abstract class FileEditUtility
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
        private static readonly Dictionary<FileTypeFilter, string> FileTypeExtensions = new()
        {
            { FileTypeFilter.All, "*.*" },
            { FileTypeFilter.Images, "*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.tga;*.psd;*.tif;*.tiff" },
            { FileTypeFilter.TextFiles, "*.txt;*.json;*.xml;*.yml;*.yaml;*.md;*.html;*.doc;*.doc;*.docx;*.pdf;*.ppt;*.pptx" },
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
            return FileTypeExtensions.GetValueOrDefault(typeFilter, "");
        }
        
        /// <summary>
        /// Hash算法类型
        /// </summary>
        public enum HashAlgorithmType
        {
            MD5,
            SHA1,
            SHA256
        }
        
        /// <summary>
        /// 计算文件的hash值
        /// </summary>
        public static string CalculateHash(string filePath, HashAlgorithmType algorithm)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                HashAlgorithm hashAlgorithm;
                switch (algorithm)
                {
                    case HashAlgorithmType.MD5:
                        hashAlgorithm = MD5.Create();
                        break;
                    case HashAlgorithmType.SHA1:
                        hashAlgorithm = SHA1.Create();
                        break;
                    case HashAlgorithmType.SHA256:
                        hashAlgorithm = SHA256.Create();
                        break;
                    default:
                        return "Unsupported algorithm";
                }

                using (hashAlgorithm)
                {
                    var hash = hashAlgorithm.ComputeHash(stream);
                    var sb = new StringBuilder();
                    foreach (var b in hash)
                    {
                        sb.Append(b.ToString("x2"));
                    }

                    return sb.ToString();
                }
            }
            catch
            {
                return "Error";
            }
        }
        
        
    }
}