using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LitJson;
using UnityEngine;
using UnityEngine.Networking;
using ZSYFramework.Core;

namespace ZSYFramework.AssetBundle
{
    /// <summary>
    /// 资源工具集。封装路径解析、资源信息查询等加载辅助逻辑。
    /// </summary>
    public class ResourceUtils : MonoSingleton<ResourceUtils>
    {
        /// <summary>清单文件的真实磁盘路径。供文件系统 API (File/Directory) 使用, 不带协议前缀。</summary>
        public static string ResInfoListFileOutPutPath =>
            Path.Combine(Application.streamingAssetsPath, "ResourcesInfoList.json");

        /// <summary>清单文件的 URI 形式。供 UnityWebRequest / Application.OpenURL 使用。</summary>
        public static string ResInfoListFileOutPutUri => "file://" + ResInfoListFileOutPutPath;

        private ResourcesInfo _resourcesInfo;

        private readonly List<ResInfoLoadWaitCache> _cache = new(); //�����б����ظ�����

        public void GetResInfo(string resName, Action<string, string> callback)
        {
            if (_resourcesInfo == null)
            {
                _cache.Add(new ResInfoLoadWaitCache(resName, callback));
                if (_cache.Count > 1) return;
                StartCoroutine(LoadText((success, json) =>
                {
                    if (!success)
                    {
                        SYLog.Error($"Failed to load resource info: {resName}");
                        return;
                    }

                    _resourcesInfo = JsonMapper.ToObject<ResourcesInfo>(json);

                    if (_cache.Count <= 0) return;
                    foreach (var cache in _cache)
                    {
                        GetResInfo(cache.ResName, cache.Callback);
                    }

                    _cache.Clear();
                }));
                return;
            }

            if (_resourcesInfo.ResourcesInfoList.TryGetValue(resName, out var infoStr))
            {
                var strs = infoStr.Split("_");
                int.TryParse(strs[0], out var pathIndex);
                int.TryParse(strs[1], out var abIndex);
                int.TryParse(strs[2], out var suffixIndex);
                var fullName = $"{resName}{_resourcesInfo.SuffixList[suffixIndex]}";
                var fullPath = Path.Combine(_resourcesInfo.PathList[pathIndex], fullName);
                var abName = _resourcesInfo.AbNameList[abIndex];
                callback?.Invoke(abName, fullPath);
                return;
            }

            SYLog.Error($"Failed to load resource info: {resName}");
        }

        private static IEnumerator LoadText(Action<bool, string> callback)
        {
            var www = UnityWebRequest.Get(ResInfoListFileOutPutUri);
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                SYLog.Error($"Failed to load LoadText {www.error}");
                callback?.Invoke(false, www.error);
            }
            else
            {
                var text = Encoding.UTF8.GetString(www.downloadHandler.data).TrimStart('\uFEFF');
                callback?.Invoke(true, text);
            }
        }
    }

    /// <summary>
    /// ��Դ�嵥ʵ��
    /// </summary>
    public class ResourcesInfo
    {
        public List<string> PathList;
        public List<string> AbNameList;
        public List<string> SuffixList;
        public Dictionary<string, string> ResourcesInfoList;
    }

    /// <summary>
    /// ������Դ�嵥ʱ�Ļ���
    /// </summary>
    public struct ResInfoLoadWaitCache
    {
        public readonly string ResName;
        public readonly Action<string, string> Callback;

        public ResInfoLoadWaitCache(string resName, Action<string, string> callback)
        {
            this.ResName = resName;
            this.Callback = callback;
        }
    }
}
