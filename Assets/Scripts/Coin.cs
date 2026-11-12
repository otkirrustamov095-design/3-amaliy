using UnityEngine;

/// <summary>Aylanib turadigan va yuqori-pastga tebranadigan tanga (bonus).</summary>
public class Coin : MonoBehaviour
{
    public float spinSpeed = 120f;
    public float bobHeight = 0.15f;
    public float bobSpeed = 2f;

    Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    public void Collect()
    {
        Destroy(gameObject);
    }
}
