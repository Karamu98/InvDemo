using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Utilities
{
    public static string RemoveAllWhitespace(string inText)
    {
        string outText = "";
        foreach(char character in inText)
        {
            if(!char.IsWhiteSpace(character))
            {
                outText += character;
            }
        }

        return outText;
    }

#if UNITY_EDITOR
    public class Editor
    {
        public static void EditorDrawDictionary<T, X>(Dictionary<T, X> inputDictionary, string name = "", T defaultTVal = default, X defaultXVal = default)
        {
            GUILayout.BeginVertical();
            if (name != "")
            {
                GUILayout.Label(name);
            }

            if (inputDictionary.Count == 0)
            {
                GUILayout.Label("This dictionary doesn't have any items.");
            }

            foreach (var item in inputDictionary)
            {
                GUILayout.BeginHorizontal();
                T key = item.Key;
                X value = item.Value;

                EditorGUI.BeginChangeCheck();
                var newKey = EntryField(key);
                if (EditorGUI.EndChangeCheck())
                {
                    try
                    {
                        inputDictionary.Remove(key);
                        inputDictionary.Add(newKey, value);
                    }
                    catch (Exception e)
                    {
                        Debug.Log(e.Message);
                    }
                    break;
                }

                EditorGUI.BeginChangeCheck();
                value = EntryField(value);
                if (EditorGUI.EndChangeCheck())
                {
                    inputDictionary[key] = value;
                    break;
                }


                // Elements can have multiple types, show menu to select
                if (typeof(X) == typeof(object))
                {
                    if(TryGetEditorType(value.GetType(), out EditorLayoutTypes outType))
                    {
                        EditorLayoutTypes newType = EntryField(outType);
                        if (newType != outType)
                        {
                            // Type changed
                            Type newSysType = m_editorTypeEnumToType[newType];
                            if (newSysType == typeof(string))
                            {
                                inputDictionary[key] = (X)((object)"");
                            }
                            else
                            {
                                inputDictionary[key] = (X)Activator.CreateInstance(newSysType);
                            }

                            break;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Unsupported type for layout field {value.GetType().ToString()}");
                    }
                }

                if (GUILayout.Button("-"))
                {
                    inputDictionary.Remove(key);
                    break;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add"))
            {
                inputDictionary.Add(defaultTVal, defaultXVal);
            }
            if (GUILayout.Button("Clear"))
            {
                inputDictionary.Clear();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        public static T EntryField<T>(T value)
        {
            Type type = typeof(T);

            if (type == typeof(object))
            {
                object currentValue = (object)value;
                if (currentValue == null)
                {
                    currentValue = "newValue";
                }
                type = currentValue.GetType();
            }

            Func<object, object> func;
            if (m_typeFieldLookup.TryGetValue(type, out func))
            {
                return (T)func(value);
            }

            if (type.IsEnum)
            {
                return (T)(object)EditorGUILayout.EnumPopup((Enum)(object)value);
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                return (T)(object)EditorGUILayout.ObjectField((UnityEngine.Object)(object)value, type, true);
            }

            Debug.Log("Type is not supported: " + type);
            return value;
        }
        public static bool TryGetEditorType(Type inType, out EditorLayoutTypes outType) => TryGetEditorTypeFromStr(inType.ToString(), out outType);
        public static bool TryGetEditorTypeFromStr(string inStr, out EditorLayoutTypes outType)
        {
            outType = EditorLayoutTypes.Int;

            string valueTypeName = inStr.ToLower();
            int pathTrim = valueTypeName.LastIndexOf('.');
            if (pathTrim != -1)
            {
                ++pathTrim;
                valueTypeName = valueTypeName.Substring(pathTrim, valueTypeName.Length - pathTrim);
            }

            valueTypeName = TypeNameCheck(valueTypeName);
            if (Enum.TryParse(valueTypeName, true, out EditorLayoutTypes foundType))
            {
                outType = foundType;
                return true;
            }
            return false;
        }

        private static string TypeNameCheck(string inType)
        {
            string outStr = inType.ToLower();
            if (m_typeRenames.TryGetValue(inType, out string correctValue))
            {
                outStr = correctValue;
            }

            return outStr;
        }

        private static readonly Dictionary<Type, Func<object, object>> m_typeFieldLookup = new Dictionary<Type, Func<object, object>>()
        {
            { typeof(int), (value) => EditorGUILayout.IntField((int)value) },
            { typeof(float), (value) => EditorGUILayout.FloatField((float)value) },
            { typeof(string), (value) => EditorGUILayout.TextField((string)value) },
            { typeof(bool), (value) => EditorGUILayout.Toggle((bool)value) },
            { typeof(Vector2), (value) => EditorGUILayout.Vector2Field(GUIContent.none, (Vector2)value) },
            { typeof(Vector3), (value) => EditorGUILayout.Vector3Field(GUIContent.none, (Vector3)value) },
            { typeof(Bounds), (value) => EditorGUILayout.BoundsField((Bounds)value) },
            { typeof(Rect), (value) => EditorGUILayout.RectField((Rect)value) },
        };

        private static readonly Dictionary<EditorLayoutTypes, Type> m_editorTypeEnumToType = new Dictionary<EditorLayoutTypes, Type>()
        {
            { EditorLayoutTypes.Int, typeof(int) },
            { EditorLayoutTypes.Float, typeof(float) },
            { EditorLayoutTypes.String, typeof(string) },
            { EditorLayoutTypes.Boolean, typeof(bool) },
            { EditorLayoutTypes.Vector2, typeof(Vector2) },
            { EditorLayoutTypes.Vector3, typeof(Vector3) },
            { EditorLayoutTypes.Bounds, typeof(Bounds) },
            { EditorLayoutTypes.Rect, typeof(Rect) },
        };
        private static readonly Dictionary<string, string> m_typeRenames = new Dictionary<string, string>()
        {
            { "single", "Float" },
            { "int32", "Int" },
            { "bool", "Boolean" }
        };

        public enum EditorLayoutTypes
        {
            Int,
            Float,
            String,
            Boolean,
            Vector2,
            Vector3,
            Bounds,
            Rect
        }
    }
#endif
}
