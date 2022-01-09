using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

public class NewItemHelper : EditorWindow
{
    private const string m_itemsPath = @"Assets/Resources/Items/";


    [MenuItem("Custom/Create Item")]
    private static void Init()
    {
        NewItemHelper window = (NewItemHelper)EditorWindow.GetWindow(typeof(NewItemHelper));
        window.Show();
    }

    public void OnGUI()
    {
        EditorGUILayout.LabelField("Sprite");
        m_curSelectedSprite = (Sprite)EditorGUILayout.ObjectField(m_curSelectedSprite, typeof(Sprite), true);
        EditorGUILayout.LabelField("Interaction Sound (Optional)");
        m_curSelectedClip = (AudioClip)EditorGUILayout.ObjectField(m_curSelectedSprite, typeof(AudioClip), true);
        EditorGUILayout.LabelField("Item Name");
        m_curName = EditorGUILayout.TextField(m_curName);

        if(GUILayout.Button("Create") && m_curSelectedSprite && !string.IsNullOrWhiteSpace(m_curName))
        {
            GameItemAsset newItem = new GameItemAsset(m_curSelectedSprite, m_curName);
            string fileName = Regex.Replace(m_curName, @"\s+", "");
            if(newItem && fileName.Length > 0)
            {
                AssetDatabase.CreateAsset(newItem, $"{m_itemsPath}{fileName}.asset");
                m_curName = "";
            }
        }

        if (m_curSelectedSprite != null)
        {
            Rect curRect = EditorGUILayout.GetControlRect();
            curRect.y += curRect.height * 8;

            DrawTexturePreview(curRect, m_curSelectedSprite);
        }
    }

    private void DrawTexturePreview(Rect position, Sprite sprite)
    {
        Vector2 fullSize = new Vector2(sprite.texture.width, sprite.texture.height);
        Vector2 size = new Vector2(sprite.textureRect.width, sprite.textureRect.height);

        Rect coords = sprite.textureRect;
        coords.x /= fullSize.x;
        coords.width /= fullSize.x;
        coords.y /= fullSize.y;
        coords.height /= fullSize.y;

        Vector2 ratio;
        ratio.x = position.width / size.x;
        ratio.y = position.height / size.y;
        float minRatio = Mathf.Min(ratio.x, ratio.y);

        Vector2 center = position.center;
        position.width = size.x * minRatio * 8;
        position.height = size.y * minRatio * 8;
        position.center = center;

        GUI.DrawTextureWithTexCoords(position, sprite.texture, coords);
    }

    private Sprite m_curSelectedSprite = null;
    private AudioClip m_curSelectedClip = null;
    private string m_curName = "";
}
