using UnityEngine;

public class SlopeDebugUI : MonoBehaviour
{
    public Transform pointA;   // xe
    public Transform pointB;   // điểm phía trước

    public float slopeDeg;
    public float slopePercent;
    public float fakeResistance;

    void Update()
    {
        if (pointA == null || pointB == null) return;

        Vector3 a = pointA.position;
        Vector3 b = pointB.position;

        float dy = b.y - a.y;
        float dxz = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        if (dxz < 0.001f)
        {
            slopeDeg = 0f;
            slopePercent = 0f;
            fakeResistance = 0f;
            return;
        }

        float slopeRad = Mathf.Atan2(dy, dxz);
        slopeDeg = slopeRad * Mathf.Rad2Deg;
        slopePercent = (dy / dxz) * 100f;

        float uphillDeg = Mathf.Max(0f, slopeDeg);
        fakeResistance = Mathf.Lerp(0f, 1f, Mathf.Clamp01(uphillDeg / 12f));
    }

    void OnGUI()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 24;
        labelStyle.normal.textColor = Color.white;

        GUI.Box(new Rect(10, 10, 260, 110), "");

        GUI.Label(new Rect(20, 20, 240, 25), $"Slope: {slopeDeg:F2} deg", labelStyle);
        GUI.Label(new Rect(20, 50, 240, 25), $"Grade: {slopePercent:F1} %", labelStyle);
        GUI.Label(new Rect(20, 80, 240, 25), $"Resistance: {fakeResistance:F2}", labelStyle);
    }
}