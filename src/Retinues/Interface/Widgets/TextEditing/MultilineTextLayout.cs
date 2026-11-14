using System;
using System.Collections.Generic;
using System.Globalization;

namespace Retinues.Interface.Widgets.TextEditing
{
    /// <summary>Wraps without changing the report text; indices remain UTF-16 text boundaries.</summary>
    internal sealed class MultilineTextLayout
    {
        internal readonly struct Line(int start, int end)
        {
            internal int Start { get; } = start;
            internal int End { get; } = end;
        }

        internal List<Line> Lines { get; } = new();
        private readonly int[] _positions;
        private readonly float[] _widths;

        internal MultilineTextLayout(string text, float maxWidth, Func<string, float> measure)
        {
            maxWidth = Math.Max(1, maxWidth);
            var elements = StringInfo.ParseCombiningCharacters(text);
            _positions = new int[elements.Length + 1];
            Array.Copy(elements, _positions, elements.Length);
            _positions[elements.Length] = text.Length;
            _widths = new float[_positions.Length];
            for (int i = 0; i < elements.Length; i++)
                _widths[i + 1] = _widths[i] + (text[elements[i]] == '\n' ? 0 : Math.Max(0, measure(text.Substring(elements[i], _positions[i + 1] - elements[i]))));

            int start = 0, cursor = 0, lastBreak = -1;
            while (cursor < elements.Length)
            {
                if (text[_positions[cursor]] == '\n')
                {
                    Lines.Add(new Line(_positions[start], _positions[cursor]));
                    start = ++cursor; lastBreak = -1;
                    continue;
                }
                if (cursor > start && _widths[cursor + 1] - _widths[start] > maxWidth)
                {
                    int end = lastBreak > start ? lastBreak : cursor;
                    Lines.Add(new Line(_positions[start], _positions[end]));
                    start = cursor = end; lastBreak = -1;
                    continue;
                }
                if (char.IsWhiteSpace(text[_positions[cursor]])) lastBreak = cursor + 1;
                cursor++;
            }
            Lines.Add(new Line(_positions[start], text.Length));
        }

        private int Boundary(int index)
        {
            int found = Array.BinarySearch(_positions, index);
            return found >= 0 ? found : Math.Max(0, ~found - 1);
        }

        internal int FindLine(int caret, int hint = -1)
        {
            if (hint >= 0 && hint < Lines.Count && caret >= Lines[hint].Start && caret <= Lines[hint].End) return hint;
            for (int i = Lines.Count - 1; i >= 0; i--)
                if (Lines[i].Start <= caret) return i;
            return 0;
        }

        internal float X(int line, int caret) => _widths[Boundary(Math.Max(Lines[line].Start, Math.Min(Lines[line].End, caret)))] - _widths[Boundary(Lines[line].Start)];

        internal int HitTest(int line, float x)
        {
            int start = Boundary(Lines[line].Start), end = Boundary(Lines[line].End);
            float origin = _widths[start];
            for (int i = start; i < end; i++)
                if (x < (_widths[i] + _widths[i + 1]) / 2 - origin) return _positions[i];
            return Lines[line].End;
        }
    }
}
