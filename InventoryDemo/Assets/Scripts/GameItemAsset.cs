using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="NewItem", menuName ="Custom/Item")]
public class GameItemAsset : ScriptableObject
{
    public GameItemAsset(Sprite sprite, string name, AudioClip interactionSound = null)
    {
        m_UIIcon = sprite;
        m_ItemName = name;
        m_interactionSound = interactionSound;
    }

    public Sprite Icon { get { return m_UIIcon; } }
    public string ItemName { get { return m_ItemName; } }
    public AudioClip InteractionClip { get { return m_interactionSound; } }

    [SerializeField] private Sprite m_UIIcon = default;
    [SerializeField] private string m_ItemName = "Default";
    [SerializeField] private AudioClip m_interactionSound;
}
