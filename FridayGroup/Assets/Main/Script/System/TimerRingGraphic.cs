using UnityEngine;
using UnityEngine.UI;


[AddComponentMenu("UI/Timer Ring Graphic")]
public sealed class TimerRingGraphic : MaskableGraphic
{
    [SerializeField, Range(0f, 1f)] private float fillAmount = 1f;
    [SerializeField] private Color gaugeColor = new Color(0.18f, 0.78f, 0.28f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField, Min(1f)] private float thickness = 10f;
    [SerializeField, Range(12, 180)] private int segmentCount = 96;

    public float FillAmount
    {
        get => fillAmount;
        set
        {
            float clampedValue = Mathf.Clamp01(value);
            if (Mathf.Approximately(fillAmount, clampedValue))
            {
                return;
            }

            fillAmount = clampedValue;
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        Rect rect = GetPixelAdjustedRect();
        float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float innerRadius = Mathf.Max(0f, outerRadius - thickness);

        AddArc(vertexHelper, rect.center, innerRadius, outerRadius, 1f, backgroundColor);
        AddArc(vertexHelper, rect.center, innerRadius, outerRadius, fillAmount, gaugeColor);
    }

    private void AddArc(
        VertexHelper vertexHelper,
        Vector2 center,
        float innerRadius,
        float outerRadius,
        float amount,
        Color arcColor)
    {
        if (amount <= 0f || outerRadius <= 0f)
        {
            return;
        }

        int arcSegments = Mathf.Max(1, Mathf.CeilToInt(segmentCount * amount));
        int firstVertex = vertexHelper.currentVertCount;

        for (int i = 0; i <= arcSegments; i++)
        {
            float progress = amount * i / arcSegments;
            // 残量を反時計回りに描くことで、空いた部分が12時位置から
            // 時計回りに増えていくタイマー表示にする。
            float angle = (90f + progress * 360f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

            AddVertex(vertexHelper, center + direction * innerRadius, arcColor);
            AddVertex(vertexHelper, center + direction * outerRadius, arcColor);
        }

        for (int i = 0; i < arcSegments; i++)
        {
            int index = firstVertex + i * 2;
            vertexHelper.AddTriangle(index, index + 1, index + 3);
            vertexHelper.AddTriangle(index, index + 3, index + 2);
        }
    }

    private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        vertexHelper.AddVert(vertex);
    }
}
