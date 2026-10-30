using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SY;
using UnityEngine;
using UnityEngine.Networking;
using ZSYFramework.Runtime.LitJson;

namespace SY
{
    public class ResUtils : MonoSingleton<ResUtils>
    {
        public static string ResInfoListFileOutPutPath => 
            
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            "file://" + Path.Combine(Application.streamingAssetsPath, "ResourcesInfoList.json");
#else
             Path.Combine(Application.streamingAssetsPath, "ResourcesInfoList.json");
#endif

        private ResourcesInfo _resourcesInfo;

        private readonly List<ResInfoLoadWaitCache> _cache = new(); //�����б����ظ�����

        public void GetResInfo(string resName, Action<string, string> callback)
        {
            if (_resourcesInfo == null)
            {
                _cache.Add(new ResInfoLoadWaitCache(resName,callback));
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
            var www = UnityWebRequest.Get(ResInfoListFileOutPutPath);
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
        public readonly Action<string, string>  Callback;

        public ResInfoLoadWaitCache(string resName, Action<string, string> callback)
        {
            this.ResName = resName;
            this.Callback = callback;
        }
    }
}