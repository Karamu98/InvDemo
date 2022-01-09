using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryCursor : MonoBehaviour
{
    [SerializeField] private Image m_selectedImage;
    [SerializeField] private AudioClip m_clickSFX;

    public void SetSelectedImage(Sprite newSprite)
    {
        m_selectedImage.sprite = newSprite;
        m_selectedImage.gameObject.SetActive(newSprite != null);
    }

    public void PlaySound()
    {
        AudioManager.Instance.TryPlaySound(m_clickSFX, 0.1f);
    }
}
