using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryMenuController : MonoBehaviour
{
    [SerializeField] private int m_columnCount = 6;
    [SerializeField] private InventoryItemElement m_startItem;
    [SerializeField] private TMPro.TMP_Text m_currentItemTextDisplay;
    [SerializeField] private InventoryItemElement[] m_allItems;
    [SerializeField] private InventoryCursor m_cursor;
    [SerializeField] private AudioClip m_defaultInteractionClip;
    [SerializeField] private AudioClip m_deleteClip;
    [SerializeField] private AudioClip m_cancelClip;

    private void Awake()
    {
        InventoryItemElement.Init(this);
        m_resolutions = new[]
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1920, 1080),
            new Vector2Int(3840, 2160)
        };

        Vector2Int cur = m_resolutions[m_currentResIDX];
        Screen.SetResolution(cur.x, cur.y, false);
    }

    private void Start()
    {
        if (m_startItem != null)
        {
            Canvas.ForceUpdateCanvases();
            OnCurrentItemChanged(m_startItem, m_startItem.transform.GetSiblingIndex());

            Dictionary<string, string> resControls = new Dictionary<string, string>()
            {
                {InputAction.Default_DecrementResolution, "Res-" },
                {InputAction.Default_IncrementResolution, "Res+" },
            };
            ActionPrompter.Instance.SetPrompter(ActionPrompter.Position.TopLeft, resControls);
        }
    }

    private void Update()
    {
        // Random Inv/Delete
        if(MyInputManager.Local.GetButtonDown(InputAction.Inventory_Special))
        {
            if(m_selectedElement != null)
            {
                m_selectedElement.GameItemAsset = null;
                OnClicked(null);
                RefreshItemName();

                AudioManager.Instance.TryPlaySound(m_deleteClip);
            }
            else
            {
                GenerateRandomItems(5);
            }
        }

        bool resChange = false;
        if (MyInputManager.Local.GetButtonDown(InputAction.Default_DecrementResolution))
        {
            m_currentResIDX = Mathf.Clamp(m_currentResIDX - 1, 0, m_resolutions.Length - 1);
            resChange = true;
        }
        else if(MyInputManager.Local.GetButtonDown(InputAction.Default_IncrementResolution))
        {
            m_currentResIDX = Mathf.Clamp(m_currentResIDX + 1, 0, m_resolutions.Length - 1);
            resChange = true;
        }
        if(resChange)
        {
            Vector2Int cur = m_resolutions[m_currentResIDX];
            Screen.SetResolution(cur.x, cur.y, false);
        }

        if (MyInputManager.Local.GetButtonDown(InputAction.Inventory_Select))
        {
            // Move/Swap
            if(m_selectedElement != null)
            {
                SwapItems(m_selectedElement, m_currentElement);
                OnClicked(null);
                RefreshItemName();
            }
            else
            {
                // Select
                if (m_currentElement.GameItemAsset != null)
                {
                    OnClicked(m_currentElement);
                }
            }
        }

        // Deselect
        if(MyInputManager.Local.GetButtonDown(InputAction.Inventory_Back))
        {
            if(m_selectedElement != null)
            {
                OnClicked(null);

                AudioManager.Instance.TryPlaySound(m_cancelClip);
            }
        }

        // Menu nav
        Vector2 navInput = MyInputManager.Instance.Get2DBufferedAxis(InputAction.Inventory_MoveX, InputAction.Inventory_MoveY, true);
        int nextIDX = m_currentElementIDX;
        if (navInput.x < 0.0f)
        {
            int id = nextIDX - 1;
            if(nextIDX == 0 || nextIDX % m_columnCount == 0)
            {
                id += m_columnCount;
            }
            nextIDX = Mathf.Clamp(id, 0, m_allItems.Length - 1);
        }
        if (navInput.x > 0.0f)
        {
            int id = nextIDX + 1;
            if(id % m_columnCount == 0)
            {
                id -= m_columnCount;
            }
            nextIDX = Mathf.Clamp(id, 0, m_allItems.Length - 1);
        }
        if (navInput.y < 0.0f)
        {
            int testIDX = nextIDX + m_columnCount;
            if(testIDX < m_allItems.Length)
            {
                nextIDX = testIDX;
            }
        }
        if (navInput.y > 0.0f)
        {
            int testIDX = nextIDX - m_columnCount;
            if (testIDX >= 0)
            {
                nextIDX = testIDX;
            }
        }
        if(nextIDX != m_currentElementIDX && nextIDX >= 0 && nextIDX < m_allItems.Length)
        {
            OnCurrentItemChanged(m_allItems[nextIDX], nextIDX);
        }

        if (MyInputManager.Local.GetAnyButtonDown())
        {
            UpdatePrompter();
        }
    }

    private void GenerateRandomItems(int count)
    {
        if(m_allItems == null || m_allItems.Length == 0)
        {
            return;
        }

        foreach(InventoryItemElement ele in m_allItems)
        {
            ele.GameItemAsset = null;
        }

        List<int> itemSelections = Enumerable.Range(0, m_allItems.Length).ToList();
        for(int i = 0; i < count; ++i)
        {
            int randItem = Random.Range(0, (int)GameItem.Total);
            GameItemAsset item = ItemResources.Get((GameItem)randItem);

            int idx = Random.Range(0, itemSelections.Count - 2);
            int elementIdx = itemSelections[idx];
            m_allItems[elementIdx].GameItemAsset = item;
            itemSelections.RemoveAt(idx);
        }

        RefreshItemName();
    }

    private void SwapItems(InventoryItemElement first, InventoryItemElement second)
    {
        // Play first sfx or default
        AudioClip clip = m_defaultInteractionClip;
        if (first.GameItemAsset.InteractionClip != null)
        {
            clip = first.GameItemAsset.InteractionClip;
        }
        AudioManager.Instance.TryPlaySound(clip);


        GameItemAsset old = first.GameItemAsset;
        first.GameItemAsset = second.GameItemAsset;
        second.GameItemAsset = old;
    }

    public void OnCurrentItemChanged(InventoryItemElement newElement, int childIDX)
    {
        m_currentElement = newElement;
        if(m_currentElement != null)
        {
            m_cursor.gameObject.transform.position = m_currentElement.transform.position;
            m_cursor.PlaySound();
        }

        m_currentElementIDX = childIDX;

        RefreshItemName();

        UpdatePrompter();
    }

    private void UpdatePrompter()
    {
        Dictionary<string, string> m_promptInfo = new Dictionary<string, string>();

        if(m_selectedElement != null)
        {
            m_promptInfo.Add(InputAction.Inventory_Back, "Cancel");
            m_promptInfo.Add(InputAction.Inventory_Special, "Discard");

            if (m_selectedElement != m_currentElement && m_currentElement.GameItemAsset != null)
            {
                m_promptInfo.Add(InputAction.Inventory_Select, "Swap");
            }

            if(!m_promptInfo.ContainsKey(InputAction.Inventory_Select))
            {
                m_promptInfo.Add(InputAction.Inventory_Select, "Move");
            }
        }
        else if(m_currentElement.GameItemAsset != null)
        {
            m_promptInfo.Add(InputAction.Inventory_Select, "Select");
        }
        
        if(!m_promptInfo.ContainsKey(InputAction.Inventory_Special))
        {
            m_promptInfo.Add(InputAction.Inventory_Special, "Random");
        }

        ActionPrompter.Instance.SetPrompter(ActionPrompter.Position.BottomLeft, m_promptInfo);
    }

    private void RefreshItemName()
    {
        m_currentItemTextDisplay.text = "";

        if (m_currentElement.GameItemAsset != null)
        {
            m_currentItemTextDisplay.text = m_currentElement.GameItemAsset.ItemName;
        }
    }

    public void OnClicked(InventoryItemElement newElement)
    {
        if(m_selectedElement != null)
        {
            m_selectedElement.Select(false);
        }
        m_selectedElement = newElement;

        Sprite newImg = null;
        if (m_selectedElement != null)
        {
            m_selectedElement.Select(true);
            newImg = m_selectedElement.GameItemAsset.Icon;
        }

        m_cursor.SetSelectedImage(newImg);
    }


    private int m_currentElementIDX = 0;
    private InventoryItemElement m_currentElement;
    private InventoryItemElement m_selectedElement;

    private Vector2Int[] m_resolutions;
    private int m_currentResIDX = 0;
}
