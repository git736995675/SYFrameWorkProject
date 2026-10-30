#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SY;
using Unity.Plastic.Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.ResourceBuilder
{
    /// <summary>
    /// AssetBundles资源管理编辑器
    /// </summary>
    public class ResourceBuilderWindow : EditorWindow
    {
        /// <summary>
        ///  打包资源目录
        /// </summary>
        private static string BundleFilesPath => Path.Combine(BundleConfigLocator.ConfigDirectory, "BundleResPath.txt");

        private static List<string> _bundleTargets;

        private static bool _isCompression = true;


        [MenuItem("SY-Tool/AssetBundle/AssetBundles管理", false, 1)]
        public static void ShowWindow()
        {
            GetWindow<ResourceBuilderWindow>("AssetBundles管理");
        }

        public static void ReadPath()
        {
            _bundleTargets ??= File.Exists(BundleFilesPath)
                ? File.ReadAllLines(BundleFilesPath).ToList()
                : new List<string>();
        }

        private static void AddPath(string outputFilePath)
        {
            if (_bundleTargets == null || _bundleTargets.Contains(outputFilePath)) return;
            _bundleTargets.Add(outputFilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(BundleFilesPath) ?? string.Empty);
            File.WriteAllLines(BundleFilesPath, _bundleTargets, Encoding.UTF8);
        }

        private void OnGUI()
        {
            ReadPath();
            EditorGUILayout.Space(5);

            DrawPathList();
          
          


            if (GUILayout.Button("处理重名文件"))
            {
                RenameFile();
            }

            EditorGUILayout.Space(5);
            GUILayout.Label($"Bundle输出路径:{Application.streamingAssetsPath}", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (GUILayout.Button("清理所有AssetBundleName"))
            {
                ClearAllAssetBundleNames();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("构建当前平台AssetBundles"))
            {
                BuildBundles(EditorUserBuildSettings.activeBuildTarget);
            }

            if (GUILayout.Button("构建Windows AssetBundles"))
            {
                BuildBundles(BuildTarget.StandaloneWindows);
            }

            EditorGUILayout.Space(5);
            _isCompression = GUILayout.Toggle(_isCompression, "Json是否进行压缩");

            EditorGUILayout.Space(5);
            GUILayout.Label($"资源清单输出路径:{ResUtils.ResInfoListFileOutPutPath}");
            EditorGUILayout.Space(5);

            if (GUILayout.Button("刷新资源信息清单"))
            {
                GenerateInfoListFile();
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("打开资源清单"))
            {
                Application.OpenURL(ResUtils.ResInfoListFileOutPutPath);
            }
        }

        private static void DrawPathList()
        {
            GUILayout.Label($"打包资源路径信息", EditorStyles.boldLabel);

            GUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (var target in _bundleTargets)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"目标路径 : {target}", EditorStyles.boldLabel);
                if (GUILayout.Button("移除", GUILayout.Width(100)))
                {
                    var tempList = File.ReadAllLines(BundleFilesPath).ToList();
                    if (tempList.Contains(target))
                    {
                        tempList.Remove(target);
                    }
                    File.WriteAllLines(BundleFilesPath, tempList, Encoding.UTF8);
                    _bundleTargets = tempList.ToList();
                    AssetDatabase.Refresh();
                }
                GUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("添加打包目录"))
            {
                var openFolderPanel = EditorUtility.OpenFolderPanel("目标目录", Application.dataPath, "");
                if (string.IsNullOrEmpty(openFolderPanel)) return;
                AddPath(openFolderPanel.Replace(Application.dataPath, "").Replace("\\", "/").Remove(0, 1));
                AssetDatabase.Refresh();
            }

            if (GUILayout.Button("清空打包目录"))
            {
                _bundleTargets?.Clear();
                File.Delete(BundleFilesPath);
                AssetDatabase.Refresh();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private static void ClearAllAssetBundleNames()
        {
            var allAssetBundleNames = AssetDatabase.GetAllAssetBundleNames();
            foreach (var assetBundleName in allAssetBundleNames)
            {
                AssetDatabase.RemoveAssetBundleName(assetBundleName, true);
            }
        }

        private static void RenameFile()
        {
            if (_bundleTargets == null)
            {
                SYLog.Error("没有找到目标目录");
                return;
            }

            foreach (var bundleDir in _bundleTargets)
            {
                var fullPath = Path.Combine(Application.dataPath, bundleDir);
                if (!Directory.Exists(fullPath)) continue;

                var subDirs = new DirectoryInfo(fullPath).GetDirectories();
                foreach (var subDir in subDirs)
                {
                    var fileList = new List<string>();
                    var bundlePath = PathUtils.AbsoluteToAssetPath(subDir.FullName);
                    var assetPaths = Directory.GetFiles(bundlePath, "*", SearchOption.AllDirectories);
                    foreach (var path in assetPaths)
                    {
                        if (extensions.Contains(Path.GetExtension(path))) continue;
                        var fileName = Path.GetFileName(path);
                        if (fileList.Contains(fileName))
                        {
                            var assetPath = PathUtils.AbsoluteToAssetPath(path);
                            var parentName = PathUtils.GetParentDirectoryName(path);
                            var newName = $"{parentName}_{fileName}";

                            AssetDatabase.RenameAsset(assetPath, newName);
                            AssetDatabase.RenameAsset($"{assetPath}.meta", $"{newName}.meta");
                            fileList.Add(newName);
                        }
                        else
                        {
                            fileList.Add(fileName);
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void BuildBundles(BuildTarget target)
        {
            if (target is BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneWindows)
            {
                target = BuildTarget.StandaloneWindows;
            }

            var path = Path.Combine(Application.streamingAssetsPath, target.ToString());
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
            ClearAllAssetBundleNames();
            SetBundleNames();
            BuildPipeline.BuildAssetBundles(path, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode, target);
            GenerateInfoListFile();
            GenerateVersionFile(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.RenameAsset($"Assets/StreamingAssets/{target}/{target}", $"{target}.unity3d");

            foreach (var file in Directory.GetFiles(path))
            {
                if (!Path.HasExtension(file)) // 如果没有后缀
                {
                    File.Move(file, file + ".unity3d");
                }
            }

            AssetDatabase.Refresh();
        }

        private static readonly List<string> extensions = new()
        {
            ".meta", ".xlsx", ".cs", ".unity", ".zip"
        };

        // 设置Bundle名称（示例：文件夹名作为Bundle名）
        private static void SetBundleNames()
        {
            if (_bundleTargets == null)
            {
                SYLog.Error("没有找到目标目录");
                return;
            }

            foreach (var bundleDir in _bundleTargets)
            {
                var fullPath = Path.Combine(Application.dataPath, bundleDir);
                if (!Directory.Exists(fullPath)) continue;
                var subDirs = new DirectoryInfo(fullPath).GetDirectories();
                foreach (var subDir in subDirs)
                {
                    var bundleName = subDir.Name.ToLower();
                    var bundlePath = PathUtils.AbsoluteToAssetPath(subDir.FullName);
                    var assetPaths = Directory.GetFiles(bundlePath, "*", SearchOption.AllDirectories);
                    foreach (var path in assetPaths)
                    {
                        if (extensions.Contains(Path.GetExtension(path))) continue;
                        var importer = AssetImporter.GetAtPath(path);
                        if (!importer)
                        {
                            SYLog.Warning("资源异常:" + path, LogCategory.Resource);
                            continue;
                        }

                        importer.assetBundleName = bundleName;
                        importer.assetBundleVariant = "unity3d";
                    }
                }
            }
        }

        public static void GenerateInfoListFile()
        {
            ReadPath();

            var resInfo = new ResourcesInfo()
            {
                PathList = new List<string>(),
                AbNameList = new List<string>(),
                SuffixList = new List<string>(),
                ResourcesInfoList = new Dictionary<string, string>(),
            };

            foreach (var bundleDir in _bundleTargets)
            {
                var fullPath = Path.Combine(Application.dataPath, bundleDir);
                if (!Directory.Exists(fullPath)) continue;

                var subDirs = new DirectoryInfo(fullPath).GetDirectories();
                foreach (var subDir in subDirs)
                {
                    var bundlePath = subDir.FullName.Replace("\\", "/").Replace(Application.dataPath.Replace("Assets", "").Replace("\\", "/"), "");
                    var assetPaths = Directory.GetFiles(bundlePath, "*", SearchOption.AllDirectories);
                    foreach (var path in assetPaths)
                    {
                        if (extensions.Contains(Path.GetExtension(path))) continue;

                        var resPath = Path.GetDirectoryName(path)?.Replace("\\", "/");
                        var pathIndex = resInfo.PathList.IndexOf(resPath);
                        if (pathIndex == -1)
                        {
                            resInfo.PathList.Add(resPath);
                            pathIndex = resInfo.PathList.Count - 1;
                        }

                        var abName = $"{subDir.Name.ToLower()}.unity3d";
                        var abIndex = resInfo.AbNameList.IndexOf(abName);
                        if (abIndex == -1)
                        {
                            resInfo.AbNameList.Add(abName);
                            abIndex = resInfo.AbNameList.Count - 1;
                        }

                        var suffix = Path.GetExtension(path);
                        var suffixIndex = resInfo.SuffixList.IndexOf(suffix);
                        if (suffixIndex == -1)
                        {
                            resInfo.SuffixList.Add(suffix);
                            suffixIndex = resInfo.SuffixList.Count - 1;
                        }

                        var fileName = Path.GetFileNameWithoutExtension(path);
                        if (resInfo.ResourcesInfoList.ContainsKey(fileName))
                        {
                            var code = 0;
                            while (resInfo.ResourcesInfoList.ContainsKey(fileName))
                            {
                                code++;
                                fileName = $"{fileName}_{code}";
                            }
                        }

                        resInfo.ResourcesInfoList.TryAdd(fileName, $"{pathIndex}_{abIndex}_{suffixIndex}");
                    }
                }
            }

            var json = JsonConvert.SerializeObject(resInfo, _isCompression ? Formatting.None : Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(ResUtils.ResInfoListFileOutPutPath) ?? string.Empty);
            File.WriteAllText(ResUtils.ResInfoListFileOutPutPath, json, Encoding.UTF8);
        }

        // 生成版本文件（MD5校验）
        private static void GenerateVersionFile(BuildTarget target)
        {
            var bundleHashes = new Dictionary<string, string>();
            var manifestPath = Path.Combine(Application.streamingAssetsPath, target.ToString(), $"{target.ToString()}");
            var manifestBundle = UnityEngine.AssetBundle.LoadFromFile(manifestPath);
            var manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");

            foreach (var bundleName in manifest.GetAllAssetBundles())
            {
                var path = Path.Combine(Application.streamingAssetsPath, target.ToString(), bundleName);
                using var md5 = System.Security.Cryptography.MD5.Create();
                using var stream = File.OpenRead(path);
                var hash = md5.ComputeHash(stream);
                bundleHashes[bundleName] = System.BitConverter.ToString(hash).Replace("-", "");
            }

            UnityEngine.AssetBundle.UnloadAllAssetBundles(true);
            var versionJson = JsonConvert.SerializeObject(bundleHashes);
            File.WriteAllText(Path.Combine(Application.streamingAssetsPath, target.ToString(), "version.json"), versionJson, Encoding.UTF8);
        }
    }
}

#endif