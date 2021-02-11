using System.Collections.Generic;
using UnityEngine;

public static class DebugExtension
{
    /// <summary>
    ///           p6 ----- p7  (max)
    ///          /  |     / |
    ///         /  p4 -- /  p5
    ///        p2 ---- p3  /
    ///        |  /    |  /
    ///  (min) p0 ---- p1
    /// </summary>
    public static void DrawBounds(Bounds bounds, Color color, float duration)
    {
        var p0 = bounds.min;
        var p1 = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        var p2 = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        var p3 = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        
        var p4 = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        var p5 = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        var p6 = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        var p7 = bounds.max;

        Debug.DrawLine(p0, p1, color, duration);
        Debug.DrawLine(p1, p3, color, duration);
        Debug.DrawLine(p3, p2, color, duration);
        Debug.DrawLine(p2, p0, color, duration);
        
        Debug.DrawLine(p0, p4, color, duration);
        Debug.DrawLine(p1, p5, color, duration);
        Debug.DrawLine(p2, p6, color, duration);
        Debug.DrawLine(p3, p7, color, duration);
        
        Debug.DrawLine(p4, p5, color, duration);
        Debug.DrawLine(p5, p7, color, duration);
        Debug.DrawLine(p7, p6, color, duration);
        Debug.DrawLine(p6, p4, color, duration);
    }
    
    
    public static void DrawRectOnPlaneY(Rect rect, Color color, float duration)
    {
        var p0 = new Vector3(rect.xMin, 0, rect.yMin);
        var p1 = new Vector3(rect.xMax, 0, rect.yMin);
        var p2 = new Vector3(rect.xMax, 0, rect.yMax);
        var p3 = new Vector3(rect.xMin, 0, rect.yMax);

        DrawShape(new[] {p0, p1, p2, p3}, color, duration);
    }

    
    public static void DrawCircle(Vector3 center, Vector3 normal, float radius, Color color, float duration)
    {
        var rotation = Quaternion.FromToRotation(Vector3.forward, normal.normalized);
        var p0 = Vector3.right;
        const float step = Mathf.PI / 8.0f;
        for (var i = 1; i <= 16; i++)
        {
            var angle = i * step;
            var p = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            Debug.DrawLine(center +  rotation * p0 * radius, center + rotation * p * radius, color, duration);
            p0 = p;
        }
    }
    
    
    public static void DrawSphere(Vector3 center, float radius, Color color, float duration)
    {
        var rY = Quaternion.FromToRotation(Vector3.forward, Vector3.up);
        var rX = Quaternion.FromToRotation(Vector3.forward, Vector3.right);
        var p0 = Vector3.right;
        const float step = Mathf.PI / 8.0f;
        for (var i = 1; i <= 16; i++)
        {
            var angle = i * step;
            var p = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            Debug.DrawLine(center +  p0 * radius, center + p * radius, color, duration);
            Debug.DrawLine(center +  rY * p0 * radius, center + rY * p * radius, color, duration);
            Debug.DrawLine(center +  rX * p0 * radius, center + rX * p * radius, color, duration);
            p0 = p;
        }
    }
    
    
    public static void DrawShape(IReadOnlyList<Vector3> points, Color color, float duration)
    {
        for (var i = 0; i < points.Count; i++)
        {
            Debug.DrawLine(points[i], points[(i + 1) % points.Count], color, duration);
        }
    }

    
    public static void DrawPoint(Vector3 position, float scale, Color color, float duration)
    {
        Debug.DrawLine(position - Vector3.left * scale, position - Vector3.right * scale, color, duration);
        Debug.DrawLine(position - Vector3.up * scale, position - Vector3.down * scale, color, duration);
        Debug.DrawLine(position - Vector3.forward * scale, position - Vector3.back * scale, color, duration);
    }


    public static void DrawTriangle(Geometry.Triangle t, Color color, float duration)
    {
        Debug.DrawLine(t.a, t.b, color, duration);
        Debug.DrawLine(t.b, t.c, color, duration);
        Debug.DrawLine(t.c, t.a, color, duration);
    }

}