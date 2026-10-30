#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using SY;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ZSYFramework.Editor.View
{
    public class ViewGenerateEditor : EditorWindow
    {
        [MenuItem("SY_Tools/UI生成", false, 21)]
        public static void ShowWindow()
        {
            GetWindow<ViewGenerateEditor>("UI生成");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            if (GUILayout.Button("为选中的物体生成UI脚本及预制体"))
            {
                GenerateScripts();
            }
        }

        private static string RootDirectory
        {
            get
            {
                var guids = AssetDatabase.FindAssets("ViewManager");

                var path = "";
                foreach (var guid in guids)
                {
                    path =AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("Runtime/View/ViewManager.cs"))
                    {
                        break;
                    }
                }
                
                var root = Application.dataPath.Replace("Assets", "");
                var rootDirectory = root + path.Replace("ViewManager.cs", "");
                return rootDirectory;
            }
        }
        
        private static string ClassOutPutPath => Path.Combine(Application.dataPath, "View/");//类输出目录

        [MenuItem("GameObject/生成UI脚本及预制体", false, 0)]
        public static void GenerateScripts()
        {
             var selectionGameObjs = Selection.gameObjects;
            foreach (var obj in selectionGameObjs)
            {
                obj.GetOrAddComponent<ViewWaitGenerateTag>();
                var prefabName = obj.name.Contains("View") ? obj.name : obj.name + "View";
                GenerateScripts(obj, prefabName);
            }

            PlayerPrefs.SetInt("isGenerate", 1);
            AssetDatabase.Refresh();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        public static void DidReloadCallback()
        {
            var isGenerate = PlayerPrefs.GetInt("isGenerate", 0);
            if (isGenerate == 0) return;
            PlayerPrefs.SetInt("isGenerate", 0);
            Generate();
        }

        //[MenuItem("GameObject/生成UI预制体")]
        private static void Generate()
        {
            var selectionGameObjs = FindObjectsByType<ViewWaitGenerateTag>(FindObjectsSortMode.None);
            foreach (var obj in selectionGameObjs)
            {
                var go = obj.gameObject;
                DestroyImmediate(obj);
                var prefabName = go.name.Contains("View") ? go.name : go.name + "View";
                GeneratePrefab(go, prefabName);
            }
            AssetDatabase.Refresh();
        }

        private static void GeneratePrefab(GameObject prefab, string prefabName)
        {
            prefabName = prefabName.Replace(" ", "").Replace("(", "").Replace(")", "");
            var outPath = $"{ClassOutPutPath}{prefabName}/".Replace('\\', '/');
            Directory.CreateDirectory(outPath);

            Type scriptType = null;
            Type modelTYpe = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.FullName == prefabName)
                    {
                        scriptType = type;
                    }

                    var modelName = $"{prefabName}Model";
                    if (type.FullName == modelName)
                    {
                        modelTYpe = type;
                    }
                }
            }

            prefab.name = prefabName;
            var view = prefab.GetComponent(scriptType);
            if (view == null)
            {
                prefab.AddComponent(scriptType);
            }

            AutoAssignTool.ProcessSelectedPrefab(prefab);
            FindButtonsAndToggles(prefab);
            PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, $"{outPath}{prefabName}.prefab", InteractionMode.AutomatedAction);

            var data = CreateInstance(modelTYpe);
            outPath = PathUtils.AbsoluteToAssetPath($"{outPath}{prefabName}Model.asset");

            AssetDatabase.CreateAsset(data, outPath);
            AssetDatabase.SaveAssets();
        }

        private static void FindButtonsAndToggles(GameObject selectedPrefab)
        {
            var scripts = selectedPrefab.GetComponentInChildren<BaseView>(true);
            scripts.buttons = selectedPrefab.GetComponentsInChildren<Button>();
            scripts.toggles = selectedPrefab.GetComponentsInChildren<Toggle>();
        }
        
        private static void GenerateScripts(GameObject prefab, string prefabName)
        {
            prefabName = prefabName.Replace(" ", "").Replace("(", "").Replace(")", "");
            var path = $"{ClassOutPutPath}{prefabName}";
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = $"{path}/{prefabName}";

            // view类
            var vModel = File.ReadAllText(Path.Combine(RootDirectory, "Models/VModel.txt"));
            var attStr = GenerateAttribute(prefab, prefabName, path);
            var vModelText = vModel.Replace("@Name", prefabName).Replace("@Attribute", attStr);
            File.WriteAllText($"{path}.cs", vModelText, Encoding.UTF8);

            // Model类
            var mModel = File.ReadAllText(Path.Combine(RootDirectory, "Models/MModel.txt"));
            var mModelText = mModel.Replace("@NameArgs", $"{prefabName}Args");
            mModelText = mModelText.Replace("@Name", $"{prefabName}Model");
            File.WriteAllText($"{path}Model.cs", mModelText, Encoding.UTF8);
        }
        
        // 属性类生成  txt_Text  btn_Button  img_Image  tog_Toggle sld_Slider scv_ScrollView inp_InputField
        private static string GenerateAttribute(GameObject prefab, string prefabName, string path)
        {
            var attributes = new[]
            {
                "LocalizationText","Text", "TMP_Text", "RawImage", "TMP_InputField", "Button", "Slider", "Toggle", "ScrollRect",
                "InputField","ToggleGroup","Canvas", "Dropdown","TMP_Dropdown","Image", "Transform"
            };
     
            var aTtribute = "";
            foreach (Transform child in prefab.transform)
            {
                var transforms = child.GetComponentsInChildren<Transform>();
                foreach (var obj in transforms)
                {
                    if (obj.name.IndexOf("_", StringComparison.Ordinal) != 0) continue;
                    foreach (var attribute in attributes)
                    {
                        var component = obj.GetComponent(attribute);
                        if (component == null) continue;
                        aTtribute +=($"\t[SerializeField] private {attribute} {obj.name};\n");
                        break;
                    }
                }
            }
            return aTtribute;
        }
    }
}
#endif