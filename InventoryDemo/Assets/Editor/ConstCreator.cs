using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

[InitializeOnLoad]
public class ConstCreator : EditorWindow
{
    private static string m_fileOutPath;
    private static bool m_buildMakerInitialised = false;
    private static GUIStyle m_headerStyle;

    [MenuItem("Custom/Consts Editor")]
    private static void Init()
    {
        m_buildMakerInitialised = true;

        m_headerStyle = new GUIStyle();
        m_headerStyle.fontStyle = FontStyle.Bold;
        m_headerStyle.alignment = TextAnchor.MiddleCenter;

        m_fileOutPath = $"{Application.dataPath}/Scripts/Constants.cs";

        // Get existing open window or if none, make a new one:
        ConstCreator window = (ConstCreator)EditorWindow.GetWindow(typeof(ConstCreator));
        window.Show();
    }

    private void OnGUI()
    {
        if (!m_buildMakerInitialised)
        {
            Init();
        }

        m_scroll = GUILayout.BeginScrollView(m_scroll);
        GUILayout.BeginHorizontal();
        if (m_constCategories != null)
        {
            if (GUILayout.Button("Save"))
            {
                if(EditorUtility.DisplayDialog("Confirm", "Overwrite Constants.cs?", "Overwrite"))
                {
                    if(SaveConstants())
                    {
                        LoadConstants();
                    }
                }
            }
        }
        if (GUILayout.Button("Load"))
        {
            LoadConstants();
        }
        GUILayout.EndHorizontal();

        if(m_constCategories != null)
        {
            for(int i = m_constCategories.Count - 1; i >= 0; --i)
            {
                EditorGUILayout.BeginHorizontal();
                if (!m_constCategories[i].DrawGUI())
                {
                    if (GUILayout.Button("Delete"))
                    {
                        m_constCategories.RemoveAt(i);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        if(GUILayout.Button("New Category"))
        {
            m_constCategories.Add(new ConstCategory());
        }

        GUILayout.EndScrollView();
    }

    private void LoadConstants()
    {
        m_constCategories = new List<ConstCategory>();

        if(File.Exists(m_fileOutPath))
        {
            string txtFile = File.ReadAllText(m_fileOutPath);

            string[] lines = txtFile.Split('\n');
            int totalledCharactersParsed = 0;
            bool beginParse = false;
            bool findingEnd = false;
            int categoryStartID = -1;
            for(int i = 0; i < lines.Length; ++i)
            {
                string currentLine = lines[i];
                totalledCharactersParsed += currentLine.Length;

                if(beginParse)
                {
                    if(findingEnd)
                    {
                        int categoryEnd = currentLine.IndexOf('}');
                        if(categoryEnd != -1)
                        {
                            categoryEnd = (totalledCharactersParsed - currentLine.Length) + categoryEnd + i;
                            string substrCategory = txtFile.Substring(categoryStartID, categoryEnd - categoryStartID);
                            m_constCategories.Add(new ConstCategory(substrCategory));
                            findingEnd = false;
                        }
                    }
                    else
                    {
                        categoryStartID = currentLine.IndexOf("public class");
                        if(categoryStartID != -1)
                        {
                            categoryStartID = (totalledCharactersParsed - currentLine.Length) + categoryStartID;
                            findingEnd = true;
                        }
                    }
                }

                if (currentLine.Contains(c_fileStartMarker))
                {
                    beginParse = true;
                    continue;
                }
            }
        }
    }

    private bool SaveConstants()
    {
        string outFile = $"{c_fileStartMarker}\n{{\n";

        HashSet<string> catNames = new HashSet<string>();
        foreach (ConstCategory category in m_constCategories)
        {
            if (!catNames.Add(category.CategoryName))
            {
                EditorUtility.DisplayDialog("Error", "One or more categories have identical names.", "Ok");
                return false;
            }
        }
        foreach (ConstCategory category in m_constCategories)
        {
            outFile += category.Serialise();
        }
        outFile += "}";

        File.WriteAllText(m_fileOutPath, outFile);
        return true;
    }

    private List<ConstCategory> m_constCategories;
    private Vector2 m_scroll = Vector2.zero;

    const string c_fileStartMarker = "public class Constants";


    private class ConstCategory
    {
        public ConstCategory(string textToParse)
        {
            string[] lines = textToParse.Split('\n');
            if (lines != null && lines.Length > 0)
            {
                bool hasName = false;
                for(int i = 0; i < lines.Length; ++i)
                {
                    string currentLine = lines[i].Trim();

                    if(!hasName)
                    {
                        int nameIDStart = currentLine.IndexOf(c_categoryNameTerm);
                        if(nameIDStart != -1)
                        {
                            nameIDStart += c_categoryNameTerm.Length;
                            CategoryName = currentLine.Substring(nameIDStart, currentLine.Length - nameIDStart);
                            hasName = true;
                        }
                    }
                    else
                    {
                        int variableIDStart = currentLine.IndexOf(c_variableTerm);
                        if(variableIDStart != -1)
                        {
                            variableIDStart += c_variableTerm.Length;
                            string lineType = GetTypeStringFromLine(currentLine, variableIDStart);
                            if(lineType == c_invalidStringType)
                            {
                                Debug.LogWarning($"Could not parse type on catergory ({CategoryName}) line: {i} ({currentLine})");
                                continue;
                            }

                            string variableName = GetVariableName(currentLine, lineType);
                            if(variableName == c_invalidStringType)
                            {
                                Debug.LogWarning($"Could not parse variable name on catergory ({CategoryName}) line: {i} ({currentLine})");
                                continue;
                            }

                            if(Utilities.Editor.TryGetEditorTypeFromStr(lineType, out Utilities.Editor.EditorLayoutTypes foundType))
                            {
                                object newValue = null;
                                switch (foundType)
                                {
                                    case Utilities.Editor.EditorLayoutTypes.Int:
                                        { if (TryGetValueFromLine(currentLine, out int val)) { newValue = val; } } break;
                                    case Utilities.Editor.EditorLayoutTypes.Float:
                                        { if (TryGetValueFromLine(currentLine, out float val)) { newValue = val; } } break;
                                    case Utilities.Editor.EditorLayoutTypes.String:
                                        { if (TryGetValueFromLine(currentLine, out string val)) { newValue = val; } } break;
                                    case Utilities.Editor.EditorLayoutTypes.Boolean:
                                        { if (TryGetValueFromLine(currentLine, out bool val)) { newValue = val; } } break;
                                    case Utilities.Editor.EditorLayoutTypes.Vector2:
                                        { if (TryGetValueFromLine(currentLine, out Vector2 val)) { newValue = val; } } break;
                                    case Utilities.Editor.EditorLayoutTypes.Vector3:
                                        { if (TryGetValueFromLine(currentLine, out Vector3 val)) { newValue = val; } } break;
                                    case Utilities.Editor.EditorLayoutTypes.Bounds:
                                        { if (TryGetValueFromLine(currentLine, out Bounds val)) { newValue = val; } } break;
                                    case Utilities.Editor.EditorLayoutTypes.Rect:
                                        { if (TryGetValueFromLine(currentLine, out Rect val)) { newValue = val; } } break;
                                }

                                if(newValue != null)
                                {
                                    m_variableData.Add(variableName, newValue);
                                }
                                else
                                {
                                    Debug.LogError($"Couldn't find variable value ({currentLine})");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"Could not parse variable value on catergory ({CategoryName}) line: {i} ({currentLine})");
                            }
                        }
                    }
                }

                if (!hasName)
                {
                    Debug.LogError($"Couldn't parse category ({textToParse})");
                }
            }
        }
        public ConstCategory()
        {
            CategoryName = "NewCategory";
        }

        public string CategoryName;

        public string Serialise()
        {
            string source = $"\tpublic class {CategoryName}\n\t{{\n";
            foreach(KeyValuePair<string, object> kvp in m_variableData)
            {
                if(Utilities.Editor.TryGetEditorType(kvp.Value.GetType(), out Utilities.Editor.EditorLayoutTypes type))
                {
                    if(c_serialiseTypeLookup.TryGetValue(type, out Func<string, object, string> func))
                    {
                        source += $"\t\tpublic const ";
                        source += func(kvp.Key, kvp.Value);
                        source += ";\n";
                    }
                }                
            }

            source += "\t}\n";

            return source;
        }

        public bool DrawGUI()
        {
            m_expanded = EditorGUILayout.BeginFoldoutHeaderGroup(m_expanded, CategoryName);
            if(m_expanded)
            {
                EditorGUILayout.EndHorizontal();
                CategoryName = EditorGUILayout.TextField("Name:", CategoryName);
                EditorGUILayout.Separator();
                Utilities.Editor.EditorDrawDictionary(m_variableData, "", "newKey", 0);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            return m_expanded;
        }

        private string GetTypeStringFromLine(string line, int startIDX)
        {
            string typeTrim = line.Substring(startIDX);
            int typeEndID = typeTrim.IndexOf(' ');
            if(typeEndID == -1)
            {
                return c_invalidStringType;
            }

            return typeTrim.Substring(0, typeEndID);
        }
        private string GetVariableName(string line, string typeName)
        {
            int startIDX = c_variableTerm.Length + typeName.Length + 1;
            if (line.Length <= startIDX)
            {
                return c_invalidStringType;
            }

            string nameTrim = line.Substring(startIDX);
            nameTrim = Utilities.RemoveAllWhitespace(nameTrim);
            int nameEndID = nameTrim.IndexOf('=');
            if (nameEndID == -1)
            {
                return c_invalidStringType;
            }

            return nameTrim.Substring(0, nameEndID);
        }
        private bool TryGetValueFromLine<T>(string line, out T value)
        {
            if(typeof(T) == typeof(string))
            {
                string outVal = "";
                if(TryGetVariableValueStringFromLine(line, out string varValue))
                {
                    outVal = varValue.Substring(1, varValue.Length - 2); // Remove the "'s
                    value = (T)(object)outVal;
                    return true;
                }
            }
            else if(typeof(T) == typeof(bool))
            {
                bool outVal = false;
                if (TryGetVariableValueStringFromLine(line, out string strValue))
                {
                    switch (strValue)
                    {
                        case "true": outVal = true; break;
                        case "false": outVal = false; break;
                        default:
                            {
                                value = default;
                                return false;
                            }
                    }
                }

                value = (T)(object)outVal;
                return true;
            }
            else if(typeof(T) == typeof(int))
            {
                if(TryGetVariableValueStringFromLine(line, out string varValue))
                {
                    if(int.TryParse(varValue, out int result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }
            }
            else if(typeof(T) == typeof(float))
            {
                if(TryGetVariableValueStringFromLine(line, out string varValue))
                {
                    if(float.TryParse(varValue, out float result))
                    {
                        value = (T)(object)result;
                        return true;
                    }
                }
            }

            Debug.LogError($"Type not implemented: {typeof(T)}");

            value = default;
            return false;
        }
        private bool TryGetVariableValueStringFromLine(string inLine, out string varValue)
        {
            varValue = "";

            int startID = inLine.IndexOf("=");
            if (startID == -1)
            {
                return false;
            }
            ++startID;
            string startTrim = inLine.Substring(startID, inLine.Length - startID);
            startID = 0;
            startTrim = Utilities.RemoveAllWhitespace(startTrim);
            int endID = startTrim.IndexOf(';');
            if (endID == -1)
            {
                return false;
            }
            varValue = startTrim.Substring(0, endID - startID);
            return true;
        }


        private Dictionary<string, object> m_variableData = new Dictionary<string, object>();
        private readonly static Dictionary<Utilities.Editor.EditorLayoutTypes, Func<string, object, string>> c_serialiseTypeLookup = new Dictionary<Utilities.Editor.EditorLayoutTypes, Func<string, object, string>>()
        {
            { Utilities.Editor.EditorLayoutTypes.String, (key, value) => $"string {key} = \"{value}\"" },
            { Utilities.Editor.EditorLayoutTypes.Boolean, (key, value) =>  $"bool {DefaultSerialiseVariable(key, value)}" },
            { Utilities.Editor.EditorLayoutTypes.Int, (key, value) =>  $"int {DefaultSerialiseVariable(key, value)}" },
            { Utilities.Editor.EditorLayoutTypes.Float, (key, value) =>  $"float {DefaultSerialiseVariable(key, value)}" },
        };
        private static string DefaultSerialiseVariable(string key, object value) => $"{key} = {value.ToString().ToLower()}";

        private bool m_expanded = false;

        const string c_invalidStringType = "INVALID";
        const string c_categoryNameTerm = "public class ";
        const string c_variableTerm = "public const ";
    }
}
