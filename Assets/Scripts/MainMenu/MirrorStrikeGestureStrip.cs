using UnityEngine;
using UnityEngine.UI;

namespace MyoGuess.MainMenu
{
    [AddComponentMenu("Myo Guess/Main Menu/Mirror Strike Gesture Strip")]
    public sealed class MirrorStrikeGestureStrip : MaskableGraphic
    {
        private static readonly Color[] GestureColors =
        {
            new Color(0f, 1f, 0.88f, 0.9f),
            new Color(1f, 0f, 0.88f, 0.9f),
            new Color(1f, 0.3f, 0f, 0.9f),
            new Color(1f, 0.72f, 0f, 0.9f)
        };

        private static readonly Vector2[][] GesturePaths =
        {
            new[]
            {
                new Vector2(-10, -12), new Vector2(-13, -4), new Vector2(-11, 5), new Vector2(-7, 5),
                new Vector2(-7, 17), new Vector2(-3, 18), new Vector2(-1, 7), new Vector2(4, 11),
                new Vector2(7, 8), new Vector2(4, 4), new Vector2(11, 4), new Vector2(12, -3),
                new Vector2(7, -11), new Vector2(-2, -15), new Vector2(-10, -12)
            },
            new[]
            {
                new Vector2(-10, -13), new Vector2(-10, 2), new Vector2(-5, 2), new Vector2(-4, 19),
                new Vector2(1, 19), new Vector2(2, 3), new Vector2(8, 3), new Vector2(10, -2),
                new Vector2(8, -13), new Vector2(-10, -13)
            },
            new[]
            {
                new Vector2(-13, -9), new Vector2(-14, 4), new Vector2(-9, 11), new Vector2(-4, 9),
                new Vector2(0, 12), new Vector2(5, 9), new Vector2(10, 11), new Vector2(14, 6),
                new Vector2(12, -7), new Vector2(5, -12), new Vector2(-6, -12), new Vector2(-13, -9)
            },
            new[]
            {
                new Vector2(-12, -13), new Vector2(-14, 2), new Vector2(-10, 5), new Vector2(-10, 17),
                new Vector2(-6, 19), new Vector2(-4, 6), new Vector2(-2, 21), new Vector2(2, 21),
                new Vector2(2, 6), new Vector2(5, 18), new Vector2(9, 16), new Vector2(7, 4),
                new Vector2(12, 11), new Vector2(15, 7), new Vector2(12, -7), new Vector2(5, -13),
                new Vector2(-12, -13)
            }
        };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float spacing = rect.width / 4f;

            for (int gesture = 0; gesture < GesturePaths.Length; gesture++)
            {
                float centerX = rect.xMin + spacing * (gesture + 0.5f);
                Vector2 origin = new Vector2(centerX, rect.center.y);
                Vector2[] path = GesturePaths[gesture];
                for (int i = 1; i < path.Length; i++)
                {
                    AddLine(vh, origin + path[i - 1], origin + path[i], 2f, GestureColors[gesture]);
                }

                if (gesture < GesturePaths.Length - 1)
                {
                    AddSquare(vh, new Vector2(centerX + spacing * 0.5f, rect.center.y - 8f), 3f,
                        new Color(0.05f, 0.55f, 1f, 0.75f));
                }
            }
        }

        private static void AddSquare(VertexHelper vh, Vector2 center, float size, Color color)
        {
            int start = vh.currentVertCount;
            Vector2 half = Vector2.one * size * 0.5f;
            vh.AddVert(center + new Vector2(-half.x, -half.y), color, Vector2.zero);
            vh.AddVert(center + new Vector2(-half.x, half.y), color, Vector2.up);
            vh.AddVert(center + new Vector2(half.x, half.y), color, Vector2.one);
            vh.AddVert(center + new Vector2(half.x, -half.y), color, Vector2.right);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddLine(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color color)
        {
            Vector2 direction = b - a;
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
