using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BalikKurtar.Managers;

namespace BalikKurtar.UI
{
    /// <summary>
    /// Bir UI nesnesinin üzerine gelindiğinde ve tıklanıldığında
    /// otomatik olarak AudioManager üzerinden ses çalmasını sağlar.
    /// Sadece butona eklemek yeterlidir.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        [Header("Özel Sesler (Boş bırakılırsa Default çalar)")]
        [SerializeField] private AudioClip customHoverSound;
        [SerializeField] private AudioClip customClickSound;

        private Selectable selectable;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            // Buton inaktifse ses çalma
            if (selectable != null && !selectable.interactable) return;

            if (AudioManager.Instance != null)
            {
                if (customHoverSound != null)
                    AudioManager.Instance.PlaySFX(customHoverSound);
                else
                    AudioManager.Instance.PlayHover();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (selectable != null && !selectable.interactable) return;

            if (AudioManager.Instance != null)
            {
                if (customClickSound != null)
                    AudioManager.Instance.PlaySFX(customClickSound);
                else
                    AudioManager.Instance.PlayClick();
            }
        }
    }
}
