using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class VoiceRangeVisualizer : MonoBehaviour
{
    [SerializeField] private float radius = 5f;
    [SerializeField] private int segments = 64;
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color color = new Color(0.2f, 0.9f, 1f, 0.7f);

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = false;
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));

        DrawCircle();
    }

    private void DrawCircle()
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            lineRenderer.SetPosition(
                i,
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0.05f,
                    Mathf.Sin(angle) * radius
                )
            );
        }
    }
}