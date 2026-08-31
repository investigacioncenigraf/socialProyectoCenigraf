using UnityEngine;

public class CameraBounds : MonoBehaviour
{
    [Header("Límites del mapa")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    // The follow controller applies this constraint before dispatching position.
    // Do not move the Transform here: the Redux camera state stays authoritative.
    public Vector2 ConstrainPosition(Vector2 position, float orthographicSize, float aspect)
    {
        float halfHeight = Mathf.Max(0f, orthographicSize);
        float halfWidth = halfHeight * Mathf.Max(0f, aspect);
        position.x = ClampAxis(position.x, minX, maxX, halfWidth);
        position.y = ClampAxis(position.y, minY, maxY, halfHeight);
        return position;
    }

    private static float ClampAxis(float value, float minimum, float maximum, float halfSize)
    {
        float low = Mathf.Min(minimum, maximum);
        float high = Mathf.Max(minimum, maximum);
        return high - low <= halfSize * 2f
            ? (low + high) * 0.5f
            : Mathf.Clamp(value, low + halfSize, high - halfSize);
    }
}
