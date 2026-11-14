using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Retinues.Interface.Widgets.TextEditing
{
    /// <summary>Plain-text editing independent of the renderer and the campaign.</summary>
    internal sealed class MultilineTextBuffer
    {
        internal const int CharacterLimit = 8000;
        private readonly List<(string Text, int Caret, int Anchor)> _undo = new();
        private readonly List<(string Text, int Caret, int Anchor)> _redo = new();
        private int[] _boundaries = { 0 };
        internal string Text { get; private set; } = "";
        internal int Caret { get; private set; }
        internal int Anchor { get; private set; }
        internal int SelectionStart => Math.Min(Caret, Anchor);
        internal int SelectionEnd => Math.Max(Caret, Anchor);
        internal string SelectedText => Text.Substring(SelectionStart, SelectionEnd - SelectionStart);

        internal static string Normalize(string text)
        {
            var result = new StringBuilder();
            text = (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n');
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '\t') result.Append("    ");
                else if (c == '\u2028' || c == '\u2029') result.Append('\n');
                else if (char.IsHighSurrogate(c))
                {
                    if (i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                    { result.Append(c); result.Append(text[++i]); }
                }
                else if (!char.IsLowSurrogate(c) && (c == '\n' || !char.IsControl(c))) result.Append(c);
            }
            return result.ToString();
        }

        private static string Fit(string text, int limit)
        {
            if (text.Length <= limit) return text;
            var boundaries = StringInfo.ParseCombiningCharacters(text);
            int end = 0;
            foreach (int boundary in boundaries)
            {
                if (boundary > limit) break;
                end = boundary;
            }
            return text.Substring(0, end);
        }

        internal void SetText(string text)
        {
            Text = Fit(Normalize(text), CharacterLimit);
            RefreshBoundaries();
            Caret = Anchor = Text.Length;
            _undo.Clear();
            _redo.Clear();
        }

        private void RefreshBoundaries()
        {
            var elements = StringInfo.ParseCombiningCharacters(Text);
            _boundaries = new int[elements.Length + 1];
            Array.Copy(elements, _boundaries, elements.Length);
            _boundaries[elements.Length] = Text.Length;
        }

        private int Snap(int position)
        {
            position = Math.Max(0, Math.Min(Text.Length, position));
            int index = Array.BinarySearch(_boundaries, position);
            return index >= 0 ? position : _boundaries[Math.Max(0, ~index - 1)];
        }

        internal int Previous(int position) => _boundaries[Math.Max(0, Array.BinarySearch(_boundaries, Snap(position)) - 1)];
        internal int Next(int position) => _boundaries[Math.Min(_boundaries.Length - 1, Array.BinarySearch(_boundaries, Snap(position)) + 1)];

        internal void MoveTo(int position, bool select = false)
        {
            Caret = Snap(position);
            if (!select) Anchor = Caret;
        }

        internal void SelectAll() { Anchor = 0; Caret = Text.Length; }

        internal void MoveHorizontal(int direction, bool select, bool word)
        {
            if (!select && Caret != Anchor && !word)
            { MoveTo(direction < 0 ? SelectionStart : SelectionEnd); return; }
            int target = direction < 0 ? Previous(Caret) : Next(Caret);
            if (word)
            {
                target = Caret;
                if (direction < 0)
                {
                    while (target > 0 && char.IsWhiteSpace(Text[Previous(target)])) target = Previous(target);
                    while (target > 0 && !char.IsWhiteSpace(Text[Previous(target)])) target = Previous(target);
                }
                else
                {
                    while (target < Text.Length && !char.IsWhiteSpace(Text[target])) target = Next(target);
                    while (target < Text.Length && char.IsWhiteSpace(Text[target])) target = Next(target);
                }
            }
            MoveTo(target, select);
        }

        private void Remember()
        {
            Push(_undo, (Text, Caret, Anchor));
            _redo.Clear();
        }

        private static void Push(List<(string, int, int)> history, (string, int, int) entry)
        {
            if (history.Count == 64) history.RemoveAt(0);
            history.Add(entry);
        }

        internal bool Insert(string value)
        {
            value = Normalize(value);
            if (value.Length == 0) return false;
            value = Fit(value, CharacterLimit - Text.Length + SelectionEnd - SelectionStart);
            if (value.Length == 0) return false;
            Remember();
            int start = SelectionStart;
            Text = Text.Substring(0, start) + value + Text.Substring(SelectionEnd);
            RefreshBoundaries();
            // Inserting before an isolated combining mark can merge it into the new
            // character. Keep the caret after that complete character, never inside it.
            int position = start + value.Length;
            int boundary = Array.BinarySearch(_boundaries, position);
            Caret = Anchor = boundary >= 0 ? position : _boundaries[~boundary];
            return true;
        }

        internal bool Delete(bool backwards, bool word = false)
        {
            int start = SelectionStart, end = SelectionEnd;
            if (start == end)
            {
                int old = Caret;
                MoveHorizontal(backwards ? -1 : 1, true, word);
                start = SelectionStart; end = SelectionEnd;
                Caret = Anchor = old;
            }
            if (start == end) return false;
            Remember();
            Text = Text.Remove(start, end - start);
            RefreshBoundaries();
            Caret = Anchor = Snap(start);
            return true;
        }

        internal bool Undo(bool redo = false)
        {
            var source = redo ? _redo : _undo;
            if (source.Count == 0) return false;
            Push(redo ? _undo : _redo, (Text, Caret, Anchor));
            var snapshot = source[source.Count - 1];
            source.RemoveAt(source.Count - 1);
            Text = snapshot.Text;
            RefreshBoundaries();
            Caret = snapshot.Caret; Anchor = snapshot.Anchor;
            return true;
        }
    }
}
