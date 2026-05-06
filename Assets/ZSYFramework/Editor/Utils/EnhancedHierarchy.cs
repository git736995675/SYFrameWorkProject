#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZSYFramework.Editor.Utils
{
    /// <summary>
    /// Unity Hierarchy窗口增强工具
    /// 用于在Hierarchy窗口中显示额外的组件图标和控制功能
    /// [InitializeOnLoad]特性确保编辑器加载时自动初始化此类
    /// </summary>
    [InitializeOnLoad]
    public class EnhancedHierarchy
    {
        // 缓存图标纹理
        private static Dictionary<string, Texture> iconCache = new();

        // 自定义样式
        private static GUIStyle buttonStyle;

        /// <summary>
        /// 静态构造函数，用于在编辑器加载时初始化
        /// </summary>
        static EnhancedHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
        }

        /// <summary>
        /// 确保样式已初始化
        /// </summary>
        private static void EnsureStylesInitialized()
        {
            buttonStyle ??= new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(2, 2, 0, 0),
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
        }

        /// <summary>
        /// 在Hierarchy窗口中绘制每个项目
        /// </summary>
        /// <param name="instanceID">实例ID</param>
        /// <param name="selectionRect">选择矩形</param>
        private static void HierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
        {
            EnsureStylesInitialized();

            // 获取当前选中的GameObject
            GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            // 计算各个元素的位置
            float rightMargin = 4f;
            float currentX = selectionRect.xMin - 3 * rightMargin;

            // 使用 EditorGUI 而不是 GUI
            EditorGUI.BeginChangeCheck();

            // 绘制组件指示器
            DrawComponentIndicators(go, ref currentX, selectionRect);

            // 绘制自定义按钮
            DrawCustomButton(go, ref currentX, selectionRect);

            // 检查是否有更改，如果有，则设置为脏状态
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(go);
            }
        }

        /// <summary>
        /// 绘制组件指示器
        /// </summary>
        /// <param name="go">GameObject</param>
        /// <param name="currentX">当前X坐标</param>
        /// <param name="selectionRect">选择矩形</param>
        private static void DrawComponentIndicators(GameObject go, ref float currentX, Rect selectionRect)
        {
            float iconSize = 14f;
            currentX += go.transform.childCount == 0 ? 10 : 0;
            if (go.GetComponent<Camera>())
                DrawIcon(ref currentX, selectionRect, "Camera Icon", iconSize);

            if (go.GetComponent<Light>())
                DrawIcon(ref currentX, selectionRect, "Light Icon", iconSize);

            if (go.GetComponent<Rigidbody>() || go.GetComponent<Rigidbody2D>())
                DrawIcon(ref currentX, selectionRect, "PhysicsRaycaster Icon", iconSize);

            if (go.GetComponent<Canvas>())
                DrawIcon(ref currentX, selectionRect, "Canvas Icon", iconSize);

            if (go.GetComponent<Button>())
            {
                DrawIcon(ref currentX, selectionRect, "Button Icon", iconSize);
            }

            if (go.GetComponent<Toggle>())
            {
                DrawIcon(ref currentX, selectionRect, "Toggle Icon", iconSize);
            }

            if (go.GetComponent<Text>())
            {
                DrawIcon(ref currentX, selectionRect, "Text Icon", iconSize);
            }

            if (go.GetComponent<ScrollRect>())
            {
                DrawIcon(ref currentX, selectionRect, "ScrollRect Icon", iconSize);
            }

            if (go.GetComponent<EventSystem>())
            {
                DrawIcon(ref currentX, selectionRect, "EventSystem Icon", iconSize);
            }
        }

        /// <summary>
        /// 绘制自定义按钮
        /// </summary>
        /// <param name="go">GameObject</param>
        /// <param name="currentX">当前X坐标</param>
        /// <param name="selectionRect">选择矩形</param>
        private static void DrawCustomButton(GameObject go, ref float currentX, Rect selectionRect)
        {
            // 使用 Event.current 检测 Control 键
            bool isControlPressed = Event.current != null && Event.current.control;
            int size = 12;
            Rect iconRect = new(selectionRect.xMax - size * 0.5f, selectionRect.y, size * 1.5f, size);
            var vOpen = EditorGUIUtility.IconContent("ViewToolOrbit On").image;
            var vClose = EditorGUIUtility.IconContent("ClothInspector.ViewValue").image;
            var vIcon = go.activeSelf ? vOpen : vClose;
            if (GUI.Button(iconRect, vIcon, buttonStyle))
            {
                if (isControlPressed)
                {
                    if (go.transform.parent == null || go.transform.parent.childCount <= 1)
                    {
                        go.SetActive(!go.activeSelf);
                        return;
                    }

                    foreach (Transform item in go.transform.parent)
                    {
                        item.gameObject.SetActive(item.gameObject == go);
                    }
                }
                else
                {
                    go.SetActive(!go.activeSelf);
                }
            }

            iconRect.x -= size * 1.6f;
            if (GUI.Button(iconRect, EditorGUIUtility.IconContent("d_TouchInputModule Icon").image, buttonStyle))
            {
                if (go.name.IndexOf("_", StringComparison.Ordinal) == 0) return;
                go.name = $"_{go.name}";
                go.name = go.name.Replace(" ", string.Empty)
                    .Replace("Legacy", string.Empty)
                    .Replace("TMP", string.Empty)
                    .Replace("(", string.Empty)
                    .Replace(")", string.Empty)
                    .Replace("_Button", "_btn")
                    .Replace("_Text", "_txt")
                    .Replace("_RawImage", "_raw")
                    .Replace("_Toggle", "_tog")
                    .Replace("_Slider", "_sld")
                    .Replace("_Panel", "_pl")
                    .Replace("_ScrollView", "_scv")
                    .Replace("_Dropdown", "_dro")
                    .Replace("_InputField", "_inp")
                    .Replace("_Image", "_img")
                    .Replace("_GameObject", "_go");
            }
        }

        /// <summary>
        /// 绘制图标
        /// </summary>
        /// <param name="currentX">当前X坐标</param>
        /// <param name="selectionRect">选择矩形</param>
        /// <param name="iconName">图标名称</param>
        /// <param name="size">图标大小</param>
        private static void DrawIcon(ref float currentX, Rect selectionRect, string iconName, float size)
        {
            // 如果图标缓存中不包含该图标，则从EditorGUIUtility中获取
            if (!iconCache.ContainsKey(iconName))
                iconCache[iconName] = EditorGUIUtility.IconContent(iconName).image;

            // 计算图标位置
            currentX -= size;
            Rect iconRect = new(currentX, selectionRect.y + (selectionRect.height - size) * 0.5f, size, size);
            // 绘制图标
            GUI.DrawTexture(iconRect, iconCache[iconName]);
            // 调整当前X坐标
            currentX -= 2f;
        }
    }
}

#endif