using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionPrompter : MonoBehaviour
{
    [Tooltip("Order is TopLeft, TopRight, BottomLeft, BottomRight, Custom")]
    [SerializeField] private TMPro.TMP_Text[] m_displayPrompts;
    [SerializeField] private TMPro.TMP_SpriteAsset[] m_platformSpriteAssets;


    public static ActionPrompter Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
        Init();
        return;
    }

    private void Init()
    {
        MyInputManager.OnPlatformChangedEvent += OnPlatformChanged;

        m_displayData = new Dictionary<string, string>[(int)Position.Total];
        for(int i = 0; i < (int)Position.Custom+1; ++i)
        {
            m_displayPrompts[i].gameObject.SetActive(false);
            m_displayData[i] = new Dictionary<string, string>();
        }

        OnPlatformChanged(MyInputManager.CurrentActivePlatform);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;

            MyInputManager.OnPlatformChangedEvent -= OnPlatformChanged;
        }
    }

    public void OnPlatformChanged(MyInputManager.Platform newPlatform)
    {
        for(int i = 0; i < m_displayPrompts.Length; ++i)
        {
            m_displayPrompts[i].spriteAsset = m_platformSpriteAssets[(int)MyInputManager.CurrentActivePlatform];
            RefreshPrompter((Position)i);
        }
    }

    public void SetPrompter(Position displayPos, Dictionary<string, string> newData)
    {
        m_displayData[(int)displayPos] = newData;
        RefreshPrompter(displayPos);
    }

    public void UpdatePrompter(Position displayPos, Dictionary<string, string> newData)
    {
        foreach(var kvp in newData)
        {
            m_displayData[(int)displayPos].Add(kvp.Key, kvp.Value);
        }

        RefreshPrompter(displayPos);
    }

    private void RefreshPrompter(Position displayPos)
    {
        string newText = "";
        Dictionary<string, string> elements = m_displayData[(int)displayPos];
        TMPro.TMP_Text field = m_displayPrompts[(int)displayPos];
        if (elements != null && elements.Count > 0)
        {
            field.gameObject.SetActive(true);

            foreach(var kvp in elements)
            {
                if(MyInputManager.Instance.TryGetGlyphBindingFromAction(kvp.Key, out string glyphName))
                {
                    newText += $"<sprite name=\"{glyphName}\"> {kvp.Value} ";
                }
            }
        }
        else
        {
            field.gameObject.SetActive(false);
        }

        field.text = newText;
    }

    Dictionary<string, string>[] m_displayData;

    public enum Position
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Custom,
        Total
    }
}
