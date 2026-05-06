#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.BundlePackage
{
    public class BuildToolWindow : EditorWindow
    {
        private string _productName;
        private string _bundleIdentifier;
        private string _version;
        private int _buildNumber;

        private bool _developmentBuild; //�Ƿ�ʹ�÷��ű�
        private bool _autoRunAfterBuild; //�Ƿ��Զ�����
        private bool _assetBundleBuild; //�Ƿ񹹽�AB

        [MenuItem("SY_Tools/打包", false, 301)]
        public static void ShowWindow()
        {
            GetWindow<BuildToolWindow>("Build Tool");
        }

        private void OnEnable()
        {
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            string name, bundle, ver;
            int build;
            BuildTool.GetAppInfo(out name, out bundle, out ver, out build);

            _productName = name;
            _bundleIdentifier = bundle;
            _version = ver;
            _buildNumber = build;
            _developmentBuild = false;
            _autoRunAfterBuild = false;
            _assetBundleBuild = true;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Application Settings", EditorStyles.boldLabel);

            _productName = EditorGUILayout.TextField("Product Name", _productName);
            _bundleIdentifier = EditorGUILayout.TextField("Bundle Identifier", _bundleIdentifier);
            _version = EditorGUILayout.TextField("Version", _version);
            _buildNumber = EditorGUILayout.IntField("Build Number", _buildNumber);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);

            _developmentBuild = EditorGUILayout.Toggle("Development Build", _developmentBuild);
            _autoRunAfterBuild = EditorGUILayout.Toggle("������Զ�����", _autoRunAfterBuild);
            _assetBundleBuild = EditorGUILayout.Toggle("����AssetBundle", _assetBundleBuild);

            EditorGUILayout.Space();
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical();
                PlayerSettings.WebGL.exceptionSupport =
                    (WebGLExceptionSupport)EditorGUILayout.EnumPopup("�쳣����ȼ���",
                        PlayerSettings.WebGL.exceptionSupport);
                PlayerSettings.WebGL.compressionFormat =
                    (WebGLCompressionFormat)EditorGUILayout.EnumPopup("ѹ����ʽ��",
                        PlayerSettings.WebGL.compressionFormat);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Settings"))
            {
                BuildTool.SetAppInfo(_productName, _bundleIdentifier, _version, _buildNumber);
                Debug.Log("Application settings applied!");
            }

            if (GUILayout.Button("Build Current Platform"))
            {
                BuildTool.SetAppInfo(_productName, _bundleIdentifier, _version, _buildNumber);
                BuildTool.Build(null, _developmentBuild, _autoRunAfterBuild, _assetBundleBuild);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Build WebGL"))
            {
                BuildTool.SetAppInfo(_productName, _bundleIdentifier, _version, _buildNumber);
                BuildTool.BuildWebGL(null, _developmentBuild, _autoRunAfterBuild, _assetBundleBuild);
            }

            if (GUILayout.Button("Build Windows"))
            {
                BuildTool.SetAppInfo(_productName, _bundleIdentifier, _version, _buildNumber);
                BuildTool.BuildWindows(null, _developmentBuild, _autoRunAfterBuild, _assetBundleBuild);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Build Android"))
            {
                BuildTool.SetAppInfo(_productName, _bundleIdentifier, _version, _buildNumber);
                BuildTool.BuildAndroid(null, _developmentBuild, _autoRunAfterBuild, _assetBundleBuild);
            }

            if (GUILayout.Button("Build iOS"))
            {
                BuildTool.SetAppInfo(_productName, _bundleIdentifier, _version, _buildNumber);
                BuildTool.BuildIos(null, _developmentBuild, _autoRunAfterBuild, _assetBundleBuild);
            }

            if (GUILayout.Button("Build Mac"))
            {
                BuildTool.SetAppInfo(_productName, _bundleIdentifier, _version, _buildNumber);
                BuildTool.BuildMac(null, _developmentBuild, _autoRunAfterBuild, _assetBundleBuild);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Current Platform: " + EditorUserBuildSettings.activeBuildTarget, MessageType.Info);
        }
    }
}
#endif