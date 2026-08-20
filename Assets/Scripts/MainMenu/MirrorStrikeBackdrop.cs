using UnityEngine;
using UnityEngine.UI;

namespace MyoGuess.MainMenu
{
    [AddComponentMenu("Myo Guess/Main Menu/Mirror Strike Backdrop")]
    public sealed class MirrorStrikeBackdrop : MaskableGraphic
    {
        private static readonly Color BackgroundTop = new Color(0.004f, 0.002f, 0.014f, 1f);
        private static readonly Color BackgroundBottom = new Color(0.002f, 0.001f, 0.009f, 1f);
        private static readonly Color Cyan = new Color(0f, 1f, 0.88f, 1f);
        private static readonly Color Magenta = new Color(1f, 0f, 0.88f, 1f);

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            AddGradientQuad(vh, rect, BackgroundBottom, BackgroundTop);

            Vector2 center = new Vector2(rect.center.x, rect.center.y + 2f);
            AddTopDivider(vh, rect);
            AddRings(vh, center, rect.width);
            AddPerspectiveGrid(vh, rect);
            AddParticles(vh, rect);
            AddCornerBrackets(vh, rect);
        }

        private static void AddTopDivider(VertexHelper vh, Rect rect)
        {
            float y = rect.yMax - rect.height * 0.145f;
            AddLine(vh, new Vector2(rect.xMin, y), new Vector2(rect.xMax, y), 1.2f,
                new Color(Cyan.r, Cyan.g, Cyan.b, 0.2f));
        }

        private static void AddRings(VertexHelper vh, Vector2 center, float width)
        {
            const int segments = 128;
            for (int ring = 0; ring < 8; ring++)
            {
                float t = ring / 7f;
                float radiusX = Mathf.Lerp(width * 0.17f, width * 0.47f, t);
                float radiusY = Mathf.Lerp(43f, 250f, t);
                Color cyan = new Color(Cyan.r, Cyan.g, Cyan.b, Mathf.Lerp(0.09f, 0.045f, t));
                Color magenta = new Color(Magenta.r, Magenta.g, Magenta.b, Mathf.Lerp(0.08f, 0.035f, t));

                Vector2 previous = center + new Vector2(radiusX, 0f);
                for (int i = 1; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    Vector2 current = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
                    float blend = (Mathf.Sin(angle) + 1f) * 0.5f;
                    AddLine(vh, previous, current, 1.05f, Color.Lerp(magenta, cyan, blend));
                    previous = current;
                }
            }
        }

        private static void AddPerspectiveGrid(VertexHelper vh, Rect rect)
        {
            float horizon = rect.yMin + rect.height * 0.23f;
            Vector2 vanishingPoint = new Vector2(rect.center.x, horizon + 95f);
            Color gridColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.055f);
            float bottomY = rect.yMin;

            for (int i = -11; i <= 11; i++)
            {
                float bottomX = rect.center.x + i * rect.width / 18f;
                AddLine(vh, vanishingPoint, new Vector2(bottomX, bottomY), 1f, gridColor);
            }

            for (int row = 0; row < 7; row++)
            {
                float t = row / 6f;
                float eased = t * t;
                float y = Mathf.Lerp(horizon, bottomY, eased);
                float halfWidth = Mathf.Lerp(rect.width * 0.16f, rect.width * 0.49f, eased);
                AddLine(vh, new Vector2(rect.center.x - halfWidth, y), new Vector2(rect.center.x + halfWidth, y),
                    1f, gridColor);
            }
        }

        private static void AddParticles(VertexHelper vh, Rect rect)
        {
            uint state = 0x51A7E123;
            for (int i = 0; i < 25; i++)
            {
                state = state * 1664525u + 1013904223u;
                float x = rect.xMin + ((state & 0xFFFF) / 65535f) * rect.width;
                state = state * 1664525u + 1013904223u;
                float y = rect.yMin + ((state & 0xFFFF) / 65535f) * rect.height;
                state = state * 1664525u + 1013904223u;
                float size = 1.5f + ((state & 0xFF) / 255f) * 3f;
                Color particleColor = i % 3 == 0
                    ? new Color(Magenta.r, Magenta.g, Magenta.b, 0.75f)
                    : new Color(Cyan.r, Cyan.g, Cyan.b, 0.67f);
                AddSolidQuad(vh, new Rect(x, y, size, size), particleColor);
            }
        }

        private static void AddCornerBrackets(VertexHelper vh, Rect rect)
        {
            const float inset = 12f;
            const float length = 18f;
            const float thickness = 1.7f;
            Color c = new Color(Cyan.r, Cyan.g, Cyan.b, 0.65f);

            Vector2 topLeft = new Vector2(rect.xMin + inset, rect.yMax - inset);
            Vector2 topRight = new Vector2(rect.xMax - inset, rect.yMax - inset);
            Vector2 bottomLeft = new Vector2(rect.xMin + inset, rect.yMin + inset);
            Vector2 bottomRight = new Vector2(rect.xMax - inset, rect.yMin + inset);

            AddLine(vh, topLeft, topLeft + Vector2.right * length, thickness, c);
            AddLine(vh, topLeft, topLeft + Vector2.down * length, thickness, c);
            AddLine(vh, topRight, topRight + Vector2.left * length, thickness, c);
            AddLine(vh, topRight, topRight + Vector2.down * length, thickness, c);
            AddLine(vh, bottomLeft, bottomLeft + Vector2.right * length, thickness, c);
            AddLine(vh, bottomLeft, bottomLeft + Vector2.up * length, thickness, c);
            AddLine(vh, bottomRight, bottomRight + Vector2.left * length, thickness, c);
            AddLine(vh, bottomRight, bottomRight + Vector2.up * length, thickness, c);
        }

        private static void AddGradientQuad(VertexHelper vh, Rect rect, Color bottom, Color top)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), bottom, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), top, Vector2.up);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), top, Vector2.one);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), bottom, Vector2.right);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddSolidQuad(VertexHelper vh, Rect rect, Color color)
        {
            AddGradientQuad(vh, rect, color, color);
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 direction = b - a;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Vector2 normal = new Vector2(-direction.y, direction.x).normalized * thickness * 0.5f;
            int start = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero);
            vh.AddVert(a + normal, color, Vector2.up);
            vh.AddVert(b + normal, color, Vector2.one);
            vh.AddVert(b - normal, color, Vector2.right);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
