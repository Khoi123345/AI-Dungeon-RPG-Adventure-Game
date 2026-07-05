using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Import thư viện EventSystems để nhận sự kiện click chuột

/// <summary>
/// Gắn component này vào bất kỳ GameObject nào có Button để tự động phát âm thanh khi bấm.
/// Bằng cách thừa kế IPointerClickHandler, script sẽ không bị mất sự kiện âm thanh kể cả khi code UI gọi Button.onClick.RemoveAllListeners().
/// </summary>
[RequireComponent(typeof(Button))]
public class PlaySoundOnButton : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Nếu để trống, sẽ phát âm thanh click mặc định của SoundManager. Nếu chọn clip, sẽ phát clip này thay thế.")]
    [SerializeField] private AudioClip customClickSound;

    private Button button;

    private void Start()
    {
        button = GetComponent<Button>();
    }

    /// <summary>
    /// Hàm tự động được Unity EventSystem gọi khi người dùng click vào GameObject này
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Chỉ phát âm thanh nếu nút bấm ở trạng thái tương tác được (Interactable)
        if (button != null && !button.interactable)
        {
            return;
        }

        PlaySound();
    }

    private void PlaySound()
    {
        if (SoundManager.Instance != null)
        {
            if (customClickSound != null)
            {
                SoundManager.Instance.PlaySFX(customClickSound);
            }
            else
            {
                SoundManager.Instance.PlayDefaultClickSound();
            }
        }
        else
        {
            // Fallback trong trường hợp chưa kéo thả hoặc chưa khởi tạo SoundManager
            if (customClickSound != null)
            {
                AudioSource.PlayClipAtPoint(customClickSound, Camera.main.transform.position);
            }
            else
            {
                Debug.LogWarning("[PlaySoundOnButton] SoundManager.Instance is null and no customClickSound is assigned.");
            }
        }
    }
}
