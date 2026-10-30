using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZSYFramework.AssetBundle;
using ZSYFramework.Core;
using ZSYFramework.Core.Json;

namespace ZSYFramework.UI
{
/// <summary>
/// UI 层级枚举：普通、弹窗、引导、顶层等。
/// </summary>
public enum UILayer
{
Background = 0, // 背景层
Normal = 100, // 普通UI层
Popup = 200, // 弹窗层
Toast = 300 // 提示层
}

/// <summary>
/// 视图管理器。UI 框架核心，管理视图栈、层级与打开/关闭调度。
/// </summary>
public class ViewManager : MonoSingleton<ViewManager>
{
// 层级管理
private readonly Dictionary<UILayer, Canvas> _layers = new();

// 展示中的view集合
private readonly Dictionary<string, BaseView> _activeViews = new();

// 已加载
private readonly Dictionary<string, BaseView> _loadedViews = new();

// 等待队列中的view
private readonly Dictionary<string, ViewArgs> _waitingViews = new();

//事件集合
private readonly Dictionary<string, List<Action>> _openActions = new();
private readonly Dictionary<string, List<Action>> _closeActions = new();


protected override void Awake()
{
    if (!ViewSetting.Camera)
    {
        ViewSetting.Camera = Camera.main;
    }

    InitializeLayers();
}

// 初始化UI层级
private void InitializeLayers()
{
    foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
    {
        var layerGo = new GameObject(layer.ToString());
        layerGo.transform.SetParent(transform);
        var canvas = layerGo.AddComponent<Canvas>();
        layerGo.AddComponent<GraphicRaycaster>();
        canvas.renderMode = ViewSetting.IsVr ? RenderMode.WorldSpace : RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        if (ViewSetting.IsVr)
        {
            layerGo.transform.localScale = Vector3.one * 0.1f;
        }

        canvas.sortingOrder = (int)layer;

        // 增加适配
        var canvasScaler = layerGo.AddComponent<CanvasScaler>();
        canvasScaler.referenceResolution = new Vector2(768, 1366);
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        canvasScaler.matchWidthOrHeight = 0;
        canvasScaler.referencePixelsPerUnit = 100;
        _layers[layer] = canvas;
    }
}

private void Update()
{
    if (!ViewSetting.IsVr || !ViewSetting.IsFollow) return;
    RefreshCanvasPos();
}

/// <summary>
/// 刷新Canvas位置
/// </summary>
public void RefreshCanvasPos()
{
    if (!ViewSetting.IsVr) return;
    if (!ViewSetting.Camera) return;
    foreach (var layer in _layers.Values)
    {
        var forwardPosition = ViewSetting.Camera.transform.position +
                              ViewSetting.Camera.transform.forward * ViewSetting.CameraZoom;
        layer.transform.eulerAngles = ViewSetting.Camera.transform.eulerAngles;
        layer.transform.position = forwardPosition;
    }
}

/// <summary>
///  打开UI界面
/// </summary>
public void ShowView<T>(ViewArgs args = null) where T : BaseView
{
    ShowView(typeof(T).Name, args);
}

/// <summary>
/// 展示或者刷新界面，如界面展示中就刷新
/// </summary>
public void ShowOrRefresh<T>(ViewArgs args)
{
    var uiName = typeof(T).Name;
    if (HasView(uiName))
    {
        GetModel(uiName).RefreshArgs(args);
    }
    else
    {
        ShowView(uiName, args);
    }
}

public void ShowView(string uiName, ViewArgs args = null)
{
    if (_activeViews.ContainsKey(uiName))
    {
        SYLog.Warning($"The view already exists. {uiName}", LogCategory.UI, this);
        return;
    }

    if (_loadedViews.TryGetValue(uiName, out var view))
    {
        view.ShowView();
        _activeViews[uiName] = view;
        InvokeOpenActions(uiName);
        _loadedViews.Remove(uiName);
        return;
    }

    ABLoadManager.Instance.Load<BaseModel>($"{uiName}Model", (model) =>
    {
        model.Initialize(args);

        ABLoadManager.Instance.LoadPrefab($"{uiName}", (goPrefab) =>
        {
            RefreshCanvasPos();
            // 实例化并挂接到层级
            var viewGo = Instantiate(goPrefab, _layers[model.layer].transform, false);
            var view = viewGo.GetComponent(uiName) as BaseView;
            if (view == null) return;
            view.ViewName = uiName;
            view.Initialize(model);
            view.ShowView();
            _activeViews[uiName] = view;
            InvokeOpenActions(uiName);
        });
    });
}



/// <summary>
/// 只加载不展示
/// </summary>
public void LoadView<T>(ViewArgs args = null, Action callback = null) where T : BaseView
{
    LoadView(typeof(T).Name, args, callback);
}

public void LoadView<T>( Action callback = null) where T : BaseView
{
    LoadView(typeof(T).Name, null, callback);
}

public void LoadView(string uiName, ViewArgs args = null, Action callback = null)
{
    if (_activeViews.ContainsKey(uiName) || _loadedViews.ContainsKey(uiName))
    {
        SYLog.Warning($"LoadView The view already exists. {uiName}", LogCategory.UI, this);
        return;
    }

    ABLoadManager.Instance.Load<BaseModel>($"{uiName}Model", (model) =>
    {
        model.Initialize(args);
        ABLoadManager.Instance.LoadPrefab($"{uiName}", (goPrefab) =>
        {
            var viewGo = Instantiate(goPrefab, _layers[model.layer].transform, false);
            var view = viewGo.GetComponent(uiName) as BaseView;
            if (view == null) return;
            view.ViewName = uiName;
            view.Initialize(model);
            view.gameObject.SetActive(false);
            _loadedViews[uiName] = view;
            callback?.Invoke();
        });
    });
}

/// <summary>
/// 将界面增加到等待队列中，有界面关闭时检查缓存，有就打开
/// </summary>
public void AddWaitView<T>(ViewArgs args = null) where T : BaseView
{
    AddWaitView(typeof(T).Name, args);
}

public void AddWaitView(string uiName, ViewArgs args = null)
{
    _waitingViews.TryAdd(uiName, args);
}

public void CheckWaitingView()
{
    if (_waitingViews.Count == 0) return;

    // 打开所有等待的View
    foreach (var viewInfo in _waitingViews)
    {
        ShowView(viewInfo.Key, viewInfo.Value);
    }
    _waitingViews.Clear();
}

private void InvokeOpenActions(string uiName)
{
    if (!_openActions.TryGetValue(uiName, out var actions)) return;
    foreach (var action in actions)
    {
        action?.Invoke();
    }

    _openActions.Remove(uiName);
}

/// <summary>
/// 关闭UI界面
/// </summary>
public void CloseView<T>() where T : BaseView
{
    var uiName = typeof(T).Name;
    CloseView(uiName);
}

public void CloseView(string uiName)
{
    if (!_activeViews.TryGetValue(uiName, out var view)) return;
    view.HideView();
    _activeViews.Remove(uiName);

    if (!_closeActions.TryGetValue(uiName, out var actions)) return;
    foreach (var action in actions)
    {
        action?.Invoke();
    }

    _closeActions.Remove(uiName);
}

/// <summary>
/// 检测ui是否存在
/// </summary>
public bool HasView<T>() where T : BaseView
{
    return HasView(typeof(T).Name);
}

public bool HasView(string uiName)
{
    return _activeViews.ContainsKey(uiName);
}

/// <summary>
/// 关闭当前所有界面
/// </summary>
public void CloseAllView()
{
    if (_activeViews == null || _activeViews.Count == 0) return;
    foreach (var view in _activeViews)
    {
        view.Value.HideView();
    }

    _activeViews.Clear();
}

public void RegisterOpenViewAction(string uiName, Action action)
{
    if (!_openActions.TryGetValue(uiName, out var actions))
    {
        actions = new List<Action>();
        _openActions.Add(uiName, actions);
    }

    if (!actions.Contains(action))
    {
        actions.Add(action);
    }
}

public void RegisterCloseViewAction(string uiName, Action action)
{
    if (!_closeActions.TryGetValue(uiName, out var actions))
    {
        actions = new List<Action>();
        _closeActions.Add(uiName, actions);
    }

    if (!actions.Contains(action))
    {
        actions.Add(action);
    }
}

public BaseModel GetModel(string uiName)
{
    return _activeViews.TryGetValue(uiName, out var view) ? view.Model : null;
}

public BaseModel GetModel<T>() where T : BaseModel
{
    var uiName = typeof(T).Name.Replace("Model", "");
    return _activeViews.TryGetValue(uiName, out var view) ? view.Model : null;
}
}
}
