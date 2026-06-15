using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public class ArcLayoutGroup : MonoBehaviour
{
    [Header("Arc")]
    [SerializeField] float radius = 300f;
    [SerializeField, Range(1f, 360f)] float arcSpanDegrees = 60f;
    [SerializeField] bool invertArch = false;

    [Header("Item")]
    [SerializeField] bool applyRotation = true;

    void OnEnable() => Rebuild();

#if UNITY_EDITOR
    void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this != null) Rebuild();
        };
    }
#endif

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        List<RectTransform> children = GetActiveChildren();
        int n = children.Count;
        if (n == 0) return;

        float sign = invertArch ? -1f : 1f;
        float centerAngleDeg = invertArch ? 270f : 90f;

        for (int i = 0; i < n; i++)
        {
            float t = n > 1 ? (float)i / (n - 1) : 0.5f;
            float angleDeg = centerAngleDeg + (0.5f - t) * arcSpanDegrees;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float x = Mathf.Cos(angleRad) * radius;
            float y = -sign * radius + Mathf.Sin(angleRad) * radius;

            children[i].anchorMin = new Vector2(0.5f, 0.5f);
            children[i].anchorMax = new Vector2(0.5f, 0.5f);
            children[i].pivot    = new Vector2(0.5f, 0.5f);
            children[i].anchoredPosition = new Vector2(x, y);
            children[i].localRotation = applyRotation
                ? Quaternion.Euler(0f, 0f, angleDeg - 90f)
                : Quaternion.identity;
        }
    }

    List<RectTransform> GetActiveChildren()
    {
        var list = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i) is RectTransform rt && rt.gameObject.activeSelf)
                list.Add(rt);
        }
        return list;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float sign = invertArch ? -1f : 1f;
        float centerAngleDeg = invertArch ? 270f : 90f;
        float halfSpan = arcSpanDegrees / 2f;

        Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
        const int segments = 48;
        Vector3 prev = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angleDeg = (centerAngleDeg - halfSpan) + (float)i / segments * arcSpanDegrees;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector3 local = new Vector3(
                Mathf.Cos(angleRad) * radius,
                -sign * radius + Mathf.Sin(angleRad) * radius,
                0f);
            Vector3 world = transform.TransformPoint(local);

            if (i > 0) Gizmos.DrawLine(prev, world);
            prev = world;
        }
    }
#endif
}
