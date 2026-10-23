using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrovsTestApp
{
    /// <summary>Stroked 24x24 icons, written as SVG path data.</summary>
    public static class TabIcons
    {
        public const string Home = "M3 11 L12 3 L21 11 V20 a1 1 0 0 1 -1 1 H15 V15 H9 V21 H4 a1 1 0 0 1 -1 -1 Z";
        public const string Links =
            "M10 14 a5 5 0 0 0 7 0 l3 -3 a5 5 0 0 0 -7 -7 l-1 1 M14 10 a5 5 0 0 0 -7 0 l-3 3 a5 5 0 0 0 7 7 l1 -1";
        public const string Events = "M4 19 V9 M10 19 V5 M16 19 V12 M22 19 H2";
        public const string InApp = "M4 5 H20 V16 H8 L4 20 Z";

        public const string Back = "M19 12 H5 M11 6 L5 12 L11 18";
        public const string ChevronDown = "M6 9 L12 15 L18 9";
        public const string ChevronUp = "M6 15 L12 9 L18 15";
        public const string Refresh = "M19 12 A7 7 0 1 1 16.95 7.05 M12.95 7.05 H16.95 V3.05";

        public static string For(Tab tab)
        {
            switch (tab)
            {
                case Tab.Home: return Home;
                case Tab.Links: return Links;
                case Tab.Events: return Events;
                default: return InApp;
            }
        }
    }

    /// <summary>
    /// Draws a stroked path in the element's text colour, so USS state classes decide the colour.
    /// </summary>
    public class StrokeIcon : VisualElement
    {
        const int ArcSegments = 12;

        readonly List<List<Vector2>> _paths;
        readonly List<bool> _closed = new List<bool>();
        readonly float _strokeWidth;
        Color _drawnColor;

        public StrokeIcon(string pathData, float strokeWidth = 1.6f)
        {
            AddToClassList("stroke-icon");
            pickingMode = PickingMode.Ignore;
            _strokeWidth = strokeWidth;
            _paths = Parse(pathData, _closed);
            generateVisualContent += Draw;
            // A theme or state change recolours the text but does not repaint custom content on its own.
            schedule.Execute(() =>
            {
                if (resolvedStyle.color != _drawnColor) MarkDirtyRepaint();
            }).Every(100);
        }

        void Draw(MeshGenerationContext context)
        {
            var size = Mathf.Min(layout.width, layout.height);
            if (float.IsNaN(size) || size <= 0) return;
            var unit = size / 24f;
            _drawnColor = resolvedStyle.color;

            var painter = context.painter2D;
            painter.strokeColor = _drawnColor;
            painter.lineWidth = _strokeWidth * unit;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            for (var i = 0; i < _paths.Count; i++)
            {
                var points = _paths[i];
                if (points.Count < 2) continue;
                painter.BeginPath();
                painter.MoveTo(points[0] * unit);
                for (var p = 1; p < points.Count; p++) painter.LineTo(points[p] * unit);
                if (_closed[i]) painter.ClosePath();
                painter.Stroke();
            }
        }

        static readonly Regex Token = new Regex(@"[A-Za-z]|-?[0-9]*\.?[0-9]+");

        /// <summary>Reads M, L, H, V, A and Z (absolute and relative) into polylines. Arcs are flattened.</summary>
        static List<List<Vector2>> Parse(string data, List<bool> closed)
        {
            var paths = new List<List<Vector2>>();
            var tokens = new List<string>();
            foreach (Match m in Token.Matches(data)) tokens.Add(m.Value);

            List<Vector2> current = null;
            var pen = Vector2.zero;
            var start = Vector2.zero;
            var command = 'M';
            var i = 0;

            float Next() => float.Parse(tokens[i++], CultureInfo.InvariantCulture);

            while (i < tokens.Count)
            {
                if (char.IsLetter(tokens[i][0])) command = tokens[i++][0];
                var relative = char.IsLower(command);
                var origin = relative ? pen : Vector2.zero;
                switch (char.ToUpperInvariant(command))
                {
                    case 'M':
                        pen = origin + new Vector2(Next(), Next());
                        start = pen;
                        current = new List<Vector2> { pen };
                        paths.Add(current);
                        closed.Add(false);
                        command = relative ? 'l' : 'L';
                        break;
                    case 'L':
                        pen = origin + new Vector2(Next(), Next());
                        current.Add(pen);
                        break;
                    case 'H':
                        pen = new Vector2((relative ? pen.x : 0) + Next(), pen.y);
                        current.Add(pen);
                        break;
                    case 'V':
                        pen = new Vector2(pen.x, (relative ? pen.y : 0) + Next());
                        current.Add(pen);
                        break;
                    case 'A':
                        var radius = Next();
                        Next(); // ry: these icons only use circular arcs
                        Next(); // x axis rotation
                        var large = Next() != 0;
                        var sweep = Next() != 0;
                        var end = origin + new Vector2(Next(), Next());
                        AddArc(current, pen, end, radius, large, sweep);
                        pen = end;
                        break;
                    case 'Z':
                        closed[closed.Count - 1] = true;
                        pen = start;
                        break;
                    default:
                        throw new FormatException("Unsupported path command " + command);
                }
            }
            return paths;
        }

        // The SVG endpoint to centre conversion, for a circle.
        static void AddArc(List<Vector2> points, Vector2 from, Vector2 to, float radius, bool large, bool sweep)
        {
            var half = (from - to) / 2f;
            var r = Mathf.Max(radius, half.magnitude);
            var factor = Mathf.Sqrt(Mathf.Max(0, (r * r - half.sqrMagnitude) / half.sqrMagnitude));
            if (large == sweep) factor = -factor;
            var centre = new Vector2(factor * half.y, -factor * half.x) + (from + to) / 2f;

            var startAngle = Mathf.Atan2(from.y - centre.y, from.x - centre.x);
            var endAngle = Mathf.Atan2(to.y - centre.y, to.x - centre.x);
            var delta = endAngle - startAngle;
            if (sweep && delta < 0) delta += 2 * Mathf.PI;
            if (!sweep && delta > 0) delta -= 2 * Mathf.PI;

            var steps = Mathf.Max(2, Mathf.CeilToInt(ArcSegments * Mathf.Abs(delta) / Mathf.PI));
            for (var s = 1; s <= steps; s++)
            {
                var angle = startAngle + delta * s / steps;
                points.Add(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r);
            }
        }
    }

    /// <summary>A 40x40 tap target holding a stroked icon. Taps do not reach the row around it.</summary>
    public class IconButton : VisualElement
    {
        public IconButton(string pathData, Action onClick, float strokeWidth = 2f)
        {
            AddToClassList("icon-button");
            Add(new StrokeIcon(pathData, strokeWidth));
            RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
            RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                onClick();
            });
            Ui.TrackPress(this);
        }
    }
}
