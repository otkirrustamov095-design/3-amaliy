using UnityEngine;

/// <summary>
/// UI ni telefonning "safe area" qismiga moslaydi (kesik/notch, burchaklar) -
/// turli ekran o'lchamlariga moslashuvchan interfeys uchun.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeArea : MonoBehaviour
{
    RectTransform rectTransform;
    Rect lastSafeArea;
    Vector2Int lastScreenSize;

    void OnEnable()
    {
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafeArea || lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height)
            Apply();
    }

    void Apply()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        lastSafeArea = Screen.safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        Vector2 min = lastSafeArea.position;
        Vector2 max = lastSafeArea.position + lastSafeArea.size;
        min.x /= Screen.width;
        min.y /= Screen.height;
        max.x /= Screen.width;
        max.y /= Screen.height;

        rectTransform.anchorMin = min;
        rectTransform.anchorMax = max;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
