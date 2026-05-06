#if UNITY_EDITOR

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace SY
{
    public class AutoAssignTool
    {
        public static void ProcessSelectedPrefab(GameObject selectedPrefab)
        {
            var scripts = selectedPrefab.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var script in scripts)
            {
                if (!script) continue; // 处理丢失的脚本

                var serializedObject = new SerializedObject(script);
                var fields = script.GetType()
                    .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var field in fields)
                {
                    // 只处理可序列化字段（public或标记了SerializeField的private字段）
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField))) continue;

                    // 获取字段类型
                    var fieldType = field.FieldType;

                    // 在预制体中查找匹配名称的物体
                    var target = FindChildByName(selectedPrefab.transform, field.Name);
                    if (target == null)
                    {
                        Debug.LogWarning($"No object named '{field.Name}' found for field in {script.name}", script);
                        continue;
                    }

                    // 检查类型匹配并赋值
                    if (fieldType == typeof(GameObject))
                    {
                        serializedObject.FindProperty(field.Name).objectReferenceValue = target.gameObject;
                    }
                    else if (fieldType.IsSubclassOf(typeof(Component)))
                    {
                        var component = target.GetComponent(fieldType);
                        if (component != null)
                        {
                            serializedObject.FindProperty(field.Name).objectReferenceValue = component;
                        }
                        else
                        {
                            Debug.LogWarning($"Component {fieldType.Name} not found on {target.name}", script);
                        }
                    }

                    serializedObject.ApplyModifiedProperties();
                }
            }
        }


        private static Transform FindChildByName(Transform parent, string name)
        {
            var queue = new Queue<Transform>();
            queue.Enqueue(parent);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.name == name) return current;

                foreach (Transform child in current)
                {
                    queue.Enqueue(child);
                }
            }

            return null;
        }
    }
}
#endif