using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ZSYFramework.Core
{
    /// <summary>
    /// 框架总入口与模块调度器。场景中唯一的驱动节点：按优先级驱动所有
    /// <see cref="IModule"/> 的初始化（OnInit）、帧更新（OnUpdate）与销毁（OnShutdown）。
    /// 建议放置在启动场景，也可不放置——首次访问 <see cref="Instance"/> 时会自动创建。
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        private static GameRoot _instance;

        /// <summary>全局实例。不存在时自动创建并加入 DontDestroyOnLoad 场景。</summary>
        public static GameRoot Instance => _instance != null ? _instance : EnsureCreated();

        // 按优先级从高到低始终有序
        private readonly List<IModule> _modules = new();

        // 每帧需要更新的模块子集
        private readonly List<IModule> _updateModules = new();

        private bool _initialized;

        [Tooltip("初始化完成后是否打印模块清单，便于排查初始化顺序问题。")]
        [SerializeField] private bool logModuleList;

        /// <summary>确保 GameRoot 存在，不存在则动态创建。</summary>
        public static GameRoot EnsureCreated()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("[Global] GameRoot");
            _instance = go.AddComponent<GameRoot>();
            if (Application.isPlaying) DontDestroyOnLoad(go);
            return _instance;
        }

        /// <summary>
        /// 注册模块。初始化开始前注册的模块按优先级统一初始化；
        /// 初始化开始后注册的模块会被立即初始化（用于运行期动态扩展）。
        /// </summary>
        public static void RegisterModule(IModule module)
        {
            if (module == null) return;
            var root = Instance;
            if (root._modules.Contains(module)) return;

            // 保持按 Priority 从大到小有序插入
            var index = root._modules.FindIndex(m => m.Priority < module.Priority);
            if (index < 0) root._modules.Add(module);
            else root._modules.Insert(index, module);

            if (root._initialized)
            {
                module.OnInit();
                root.RebuildUpdateList();
                SYLog.Info($"[GameRoot] 晚注册模块已初始化: {module.ModuleName}", LogCategory.General);
            }
        }

        /// <summary>
        /// 以委托方式注册模块。用于把既有 Manager（如各 MonoSingleton）快速接入调度，
        /// 无需改动其继承结构。
        /// </summary>
        /// <param name="moduleName">模块名称。</param>
        /// <param name="priority">初始化优先级，越大越先初始化。</param>
        /// <param name="onInit">初始化回调。</param>
        /// <param name="onUpdate">每帧回调，传 null 则不参与帧更新。</param>
        /// <param name="onShutdown">销毁回调。</param>
        public static void RegisterAdapter(string moduleName, int priority = 0,
            Action onInit = null, Action onUpdate = null, Action onShutdown = null)
        {
            RegisterModule(new ModuleAdapter(moduleName, priority,
                enableUpdate: onUpdate != null, onInit, onUpdate, onShutdown));
        }

        /// <summary>按类型查找已注册模块，未注册返回 null。</summary>
        public T GetModule<T>() where T : class, IModule
        {
            return _modules.OfType<T>().FirstOrDefault();
        }

        /// <summary>按名称查找已注册模块，未注册返回 null。</summary>
        public IModule GetModule(string moduleName)
        {
            return _modules.FirstOrDefault(m => m.ModuleName == moduleName);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (Application.isPlaying) DontDestroyOnLoad(gameObject);
            // 不在 Awake 立即初始化：留给启动脚本在场景加载期间完成模块注册，
            // 统一延迟到第一帧 Update 前按优先级初始化
        }

        private void InitializeAll()
        {
            if (_initialized) return;
            _initialized = true;

            foreach (var module in _modules)
            {
                module.OnInit();
            }

            RebuildUpdateList();

            if (logModuleList)
            {
                var names = string.Join(" -> ", _modules.Select(m => $"{m.ModuleName}({m.Priority})"));
                SYLog.Info($"[GameRoot] 模块初始化顺序: {names}", LogCategory.General);
            }
        }

        private void RebuildUpdateList()
        {
            _updateModules.Clear();
            _updateModules.AddRange(_modules.Where(m => m.EnableUpdate));
        }

        private void Update()
        {
            if (!_initialized) InitializeAll();

            for (var i = 0; i < _updateModules.Count; i++)
            {
                _updateModules[i].OnUpdate();
            }
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null;

            // 按初始化的相反顺序销毁
            for (var i = _modules.Count - 1; i >= 0; i--)
            {
                try
                {
                    _modules[i].OnShutdown();
                }
                catch (Exception e)
                {
                    SYLog.Error($"[GameRoot] 模块 {_modules[i].ModuleName} 销毁异常: {e.Message}", LogCategory.General);
                }
            }

            _modules.Clear();
            _updateModules.Clear();
        }
    }
}
