#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using SY;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace ZSYFramework.Editor.Config
{
    public partial class TableEditor : EditorWindow
    {
        [MenuItem("Assets/将表格转为json", false, 201)]
        private static void SelectionRead()
        {
            var files = Selection.assetGUIDs;
            if (files.Length == 0)
            {
                Debug.LogError("没有选中任何文件！");
                return;
            }

            foreach (var file in files)
            {
                var path = Path.GetFullPath(AssetDatabase.GUIDToAssetPath(file));
                if (!path.EndsWith(".csv") && !path.EndsWith(".xlsx"))
                {
                    continue;
                }

                var result = ConfigUtils.ReadExcel(path);
                CreateJson(result, path);
                CreateTemplate(result, path);
                Debug.Log($"已完成:{path}");
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 遍历文件夹，读取所有表格
        /// </summary>
        public static void ReadAllExcel()
        {
            TableEditor.ReadPath();
            if (TableEditor._paths == null)
            {
                Debug.LogError("表格路径为空");
                return;
            }

            foreach (var path in TableEditor._paths)
            {
                var fullPath = Path.GetFullPath(path);
                if (fullPath.Contains("~$"))
                {
                    continue;
                }

                var result = ConfigUtils.ReadExcel(fullPath);
                CreateJson(result, fullPath);
                CreateTemplate(result, fullPath);
                Debug.Log($"已完成==>>{fullPath}");
            }

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 生成json文件
        /// </summary>
        private static void CreateJson(DataSet result, string fileName)
        {
            var array = new Dictionary<int, JObject>();

            try
            {
                for (var s = 0; s < result.Tables.Count; s++)
                {
                    var sheet = result.Tables[s];

                    // 获取表格有多少列 
                    var columns = sheet.Columns.Count;
                    // 获取表格有多少行  
                    var rows = sheet.Rows.Count;
                    // 表格数据列表
                    var dataList = new List<TableData>();

                    //第一行为表头，第二行为类型 ，第三行为字段名 不读取
                    for (var i = 3; i < rows; i++)
                    {
                        for (var j = 0; j < columns; j++)
                        {
                            // 获取表格中指定行指定列的数据 
                            var value = sheet.Rows[i][j].ToString();

                            if (string.IsNullOrEmpty(value))
                            {
                                continue;
                            }

                            var tempData = new TableData
                            {
                                Type = sheet.Rows[1][j].ToString(),
                                FieldName = sheet.Rows[2][j].ToString(),
                                Value = value
                            };
                            dataList.Add(tempData);
                        }

                        if (dataList is not { Count: > 0 }) continue;
                        var tempo = new JObject();
                        var id = 0;
                        foreach (var item in dataList)
                        {
                            if (item.FieldName is "Id" or "id" or "ID")
                            {
                                id = GetValue<int>(item.Value);
                            }

                            switch (item.Type)
                            {
                                case "string":
                                case "String":
                                    tempo[item.FieldName] = GetValue<string>(item.Value);
                                    break;
                                case "int":
                                case "Int":
                                    tempo[item.FieldName] = GetValue<int>(item.Value);
                                    break;
                                case "Float":
                                case "float":
                                    tempo[item.FieldName] = GetValue<float>(item.Value);
                                    break;
                                case "Bool":
                                case "bool":
                                    tempo[item.FieldName] = GetValue<bool>(item.Value);
                                    break;
                                case "string[]":
                                case "String[]":
                                    tempo[item.FieldName] = new JArray(GetList<string>(item.Value));
                                    break;
                                case "int[]":
                                case "Int[]":
                                    tempo[item.FieldName] = new JArray(GetList<int>(item.Value));
                                    break;
                                case "float[]":
                                case "Float[]":
                                    tempo[item.FieldName] = new JArray(GetList<float>(item.Value));
                                    break;
                                case "bool[]":
                                case "Bool[]":
                                    tempo[item.FieldName] = new JArray(GetList<bool>(item.Value));
                                    break;
                                case "Vector3":
                                case "vector3":
                                case "v3":
                                    tempo[item.FieldName] = GetVector3(item.Value);
                                    break;
                            }
                        }

                        array.Add(id, tempo);
                        dataList.Clear();
                    }
                }

                var saveJson = JsonConvert.SerializeObject(array,
                    TableEditor._isCompression ? Formatting.None : Formatting.Indented);
                saveJson = TableEditor.ErrorIsEncryption ? AesSimple.JiaMi(saveJson) : saveJson;

                fileName = fileName.Replace(".xlsx", ".json").Replace(".cvs", ".json");
                File.WriteAllText(fileName, saveJson, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Console.WriteLine("fileName=>" + e);
            }
        }


        /// <summary>
        /// 字符串拆分列表
        /// </summary>
        private static List<T> GetList<T>(string str)
        {
            str = str.Replace("[", "");
            str = str.Replace("]", "");
            str = str.Replace("(", "");
            str = str.Replace(")", "");
            var ss = str.Split(',');
            var length = ss.Length;
            var array = new List<T>(ss.Length);
            for (var i = 0; i < length; i++)
            {
                array.Add(GetValue<T>(ss[i]));
            }

            return array;
        }

        private static T GetValue<T>(object value)
        {
            T t = default;
            try
            {
                t = (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception e)
            {
                Debug.LogError("GetValue  value:" + value.ToString() + " error:" + e.ToString());
            }

            return t;
        }

        /// <summary>
        /// 解析Vector3
        /// </summary>
        private static string GetVector3(string str)
        {
            //只有数组和Vector需要去除括号
            str = str.Replace("[", "");
            str = str.Replace("]", "");
            str = str.Replace("(", "");
            str = str.Replace(")", "");
            var ss = str.Split(',');
            float x = 0, y = 0, z = 0;
            var length = ss.Length;
            if (length > 0)
            {
                x = float.Parse(ss[0]);
                y = length >= 2 ? float.Parse(ss[1]) : 0;
                z = length >= 3 ? float.Parse(ss[2]) : 0;
            }

            var json = "-{" + $"-\"x-\" : {x}, -\"y-\" : {y}, -\"z-\" : {z}" + "}-";
            return json;
        }

        /// <summary>
        /// 生成实体类模板
        /// </summary>
        private static void CreateTemplate(DataSet result, string fileName)
        {
            var field = "";
            for (var i = 0; i < result.Tables[0].Columns.Count; i++)
            {
                var typeStr = result.Tables[0].Rows[1][i].ToString();
                typeStr = typeStr.ToLower();

                var nameStr = result.Tables[0].Rows[2][i].ToString();
                if (string.IsNullOrEmpty(typeStr) || string.IsNullOrEmpty(nameStr))
                {
                    continue;
                }

                var note = result.Tables[0].Rows[0][i].ToString();
                note = note.Replace("\n", "");
                note = note.Replace("\r", "");
                field += $"\r\t/// {note} " +
                         $"\r\tpublic {typeStr} {nameStr}" + " { get; set; }";
            }

            var tempStr = File.ReadAllText(ConfigUtils.ModelPath + "TableModel.txt");
            tempStr = tempStr.Replace("@Name", Path.GetFileNameWithoutExtension(fileName));
            tempStr = tempStr.Replace("@File1", field);

            fileName = fileName.Replace(".xlsx", ".cs").Replace(".cvs", ".cs");
            File.WriteAllText(fileName, tempStr, Encoding.UTF8);
        }

        public struct TableData
        {
            public string FieldName;
            public string Type;
            public string Value;

            public override string ToString()
            {
                return $"fieldName:{FieldName} type:{Type} value:{Value}";
            }
        }
    }
}


#endif