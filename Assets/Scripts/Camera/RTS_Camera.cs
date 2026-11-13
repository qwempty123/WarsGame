using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Camera))]
public class RTS_Camera : MonoBehaviour
{
    [Header("Зум колесиком мыши")]
    [SerializeField] private float zoomSpeed = 4f;
    [SerializeField] private float minOrthoSize = 3f;
    [SerializeField] private float maxOrthoSize = 25f;

    [Header("Перетаскивание камеры (средняя кнопка мыши)")]
    [SerializeField] private bool enablePan = true;
    [SerializeField] private KeyCode panButton = KeyCode.Mouse2;

    [Header("Ограничение камеры по карте (опционально)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private float minX = -100f, maxX = 100f;
    [SerializeField] private float minY = -100f, maxY = 100f;

    private Camera cam;
    private Vector3 dragOrigin;
    private bool isDragging;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
    }

    void Update()
    {
        HandleZoom();
        HandlePan();
        ClampToBounds();
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minOrthoSize, maxOrthoSize);
        }
    }

    void HandlePan()
    {
        if (!enablePan) return;

        if (Input.GetKeyDown(panButton))
        {
            isDragging = true;
            dragOrigin = Input.mousePosition;
        }

        if (Input.GetKeyUp(panButton))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 currentMouse = Input.mousePosition;
            Vector3 diff = dragOrigin - currentMouse;

            float heightWorld = cam.orthographicSize * 2f;
            float worldScale = heightWorld / Screen.height;
            Vector3 move = new Vector3(diff.x, diff.y, 0f) * worldScale;

            transform.position += move;
            dragOrigin = currentMouse;
        }
    }

    void ClampToBounds()
    {
        if (!useBounds) return;

        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
    }

    // ==================== ВИЗУАЛИЗАЦИЯ В SCENE ====================
    // Рисуется только когда объект выделен в иерархии
    void OnDrawGizmosSelected()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return;

        // 1. Границы карты (оранжевый прямоугольник)
        if (useBounds)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Vector3 boundsCenter = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, transform.position.z);
            Vector3 boundsSize = new Vector3(maxX - minX, maxY - minY, 0.1f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);

            // Заливка для наглядности
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.08f);
            Gizmos.DrawCube(boundsCenter, boundsSize);

#if UNITY_EDITOR
            Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
            Handles.Label(boundsCenter + Vector3.up * 2f, "Границы камеры");
#endif
        }

        // 2. Текущая область видимости камеры (зелёный прямоугольник)
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        float orthoHeight = cam.orthographicSize * 2f;
        float orthoWidth = orthoHeight * cam.aspect;
        Vector3 viewCenter = transform.position;
        Vector3 viewSize = new Vector3(orthoWidth, orthoHeight, 0.1f);
        Gizmos.DrawWireCube(viewCenter, viewSize);

        // Заливка области видимости
        Gizmos.color = new Color(0f, 1f, 0f, 0.05f);
        Gizmos.DrawCube(viewCenter, viewSize);

        // 3. Перекрестие позиции камеры (белый)
        Gizmos.color = Color.white;
        float crossSize = 0.5f;
        Gizmos.DrawLine(transform.position + Vector3.left * crossSize, transform.position + Vector3.right * crossSize);
        Gizmos.DrawLine(transform.position + Vector3.up * crossSize, transform.position + Vector3.down * crossSize);

#if UNITY_EDITOR
        // Подпись текущего размера
        Handles.color = Color.green;
        Handles.Label(viewCenter + Vector3.up * (orthoHeight * 0.55f),
            $"View: {cam.orthographicSize:F1} (Size)");
#endif
    }
    // ================================================================
}