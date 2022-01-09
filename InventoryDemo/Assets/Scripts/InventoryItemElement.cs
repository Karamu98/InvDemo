using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryItemElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    [SerializeField] private Image m_image;

    public GameItemAsset GameItemAsset { get { return m_itemData; } set { m_itemData = value; Refresh(); } }

    private void Awake()
    {
        Refresh();
    }

    public static void Init(InventoryMenuController controller)
    {
        m_controller = controller;
        m_enabled = Color.white;
        m_selected = m_enabled;
        m_selected *= 0.5f;
        //m_selected.a = 1;
        m_disabled = m_enabled;
        m_disabled.a = 0;
    }

    private void Refresh()
    {
        if(m_itemData == null)
        {
            m_image.sprite = null;
            m_image.color = m_disabled;
        }
        else
        {
            m_image.sprite = m_itemData.Icon;
            m_image.color = m_enabled;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_controller.OnCurrentItemChanged(this, transform.GetSiblingIndex());
    }

    public void OnPointerExit(PointerEventData eventData)
    {

    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_controller.OnClicked(this);
    }

    public void Select(bool isSelected)
    {
        m_image.color = isSelected ? m_selected : m_itemData != null ? m_enabled : m_disabled;
    }

    private GameItemAsset m_itemData;
    private static InventoryMenuController m_controller;

    private static Color m_enabled;
    private static Color m_selected;
    private static Color m_disabled;
}
