using UnityEngine;

public class CameraBounds : MonoBehaviour
{
    [Header("Límites del mapa")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        Vector3 pos = transform.position;

        // Limitar X
        pos.x = Mathf.Clamp(
            pos.x,
            minX + camWidth,
            maxX - camWidth
        );

        // Limitar Y
        pos.y = Mathf.Clamp(
            pos.y,
            minY + camHeight,
            maxY - camHeight
        );

        transform.position = pos;
    }
}
