using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemResources : MonoBehaviour
{
    private void Awake()
    {
        if(!m_instance)
        {
            m_instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }
    }

    private void LoadAll()
    {
        Object[] allItems = Resources.LoadAll("Items/");
        m_itemsMap = new Dictionary<GameItem, GameItemAsset>(allItems.Length);
        foreach (Object obj in allItems)
        {
            if (System.Enum.TryParse(obj.name, out GameItem val))
            {
                m_itemsMap.Add(val, (GameItemAsset)obj);
            }
        }
    }

    public static GameItemAsset Get(GameItem item)
    {
        if(!m_instance)
        {
            return null;
        }

        return m_instance.m_itemsMap[item];
    }


    private Dictionary<GameItem, GameItemAsset> m_itemsMap;
    private static ItemResources m_instance;
}
