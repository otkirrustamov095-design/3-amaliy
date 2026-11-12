using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Virtual joystik. Canvas ichidagi "JoystickBackground" obyektiga qo'yiladi.
/// Barmoq (yoki sichqoncha) bilan surilganda Direction (-1..1) qiymatini hisoblaydi.
/// </summary>
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Tooltip("Joystikning katta doirasi (fon)")]
    public RectTransform background;

    [Tooltip("Joystikning kichik harakatlanuvchi doirasi")]
    public RectTransform handle;

    [Range(0f, 0.5f)]
    public float deadZone = 0.1f;

    /// <summary>Joystik yo'nalishi: x - o'ng/chap, y - oldinga/orqaga</summary>
    public Vector2 Direction { get; private set; }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null) return;

        // Ekrandagi barmoq nuqtasini joystik fonining lokal koordinatasiga o'tkazamiz
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;

        Vector2 radius = background.rect.size * 0.5f;
        Vector2 normalized = new Vector2(local.x / radius.x, local.y / radius.y);
        normalized = Vector2.ClampMagnitude(normalized, 1f);

        // Kichik doirani barmoq tomon suramiz
        handle.anchoredPosition = new Vector2(normalized.x * radius.x, normalized.y * radius.y);

        Direction = normalized.magnitude < deadZone ? Vector2.zero : normalized;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Barmoq qo'yib yuborilganda joystik markazga qaytadi
        Direction = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }

    void OnDisable()
    {
        Direction = Vector2.zero;
        if (handle != null) handle.anchoredPosition = Vector2.zero;
    }
}
