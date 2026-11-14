using System;
using System.Collections.Generic;
using Retinues.Interface.Widgets.Patches;
using Retinues.Interface.Widgets.TextEditing;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace Retinues.Interface.Widgets
{
    /// <summary>A plain-text viewport. Native text rows render glyphs; editing never uses RichText markup.</summary>
    public sealed class RetinuesMultilineTextWidget : BrushWidget
    {
        private sealed class Row
        {
            internal Widget Selection;
            internal TextWidget Label;
        }

        private readonly MultilineTextBuffer _buffer = new();
        private readonly List<Row> _rows = new();
        private MultilineTextLayout _layout;
        private Widget _caret;
        private Font _font;
        private float _fontSize, _width, _lineHeight = 28, _preferredX = -1;
        private int _topLine, _visibleLines = 1, _caretLineHint = -1;
        private bool _dirty = true, _showCaret = true, _dragging, _autofocused;
        private float _blink, _dragScrollDelay, _repeatDelay;
        private InputKey? _repeatKey;

        public RetinuesMultilineTextWidget(UIContext context) : base(context)
        {
            IsFocusable = true;
            ClipContents = true;
            DoNotPassEventsToChildren = true;
        }

        [Editor(false)]
        public string Text
        {
            get => _buffer.Text;
            set
            {
                if (string.Equals(value ?? "", _buffer.Text, StringComparison.Ordinal)) return;
                _buffer.SetText(value);
                Changed();
            }
        }

        [Editor(false)]
        public ScrollbarWidget VerticalScrollbar { get; set; }

        private static bool Shift => Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);
        // AltGr is used to type characters on many European keyboards, not a Ctrl shortcut.
        private static bool Control => !Input.IsKeyDown(InputKey.RightAlt)
            && (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl));

        private void FocusEditor()
        {
#if BL12
            EventManager.SetWidgetFocused(this);
#else
            EventManager.FocusedWidget = this;
#endif
        }

        private (float X, float Y) LocalMousePosition
        {
            get
            {
#if BL12
                var mouse = (EventManager.MousePosition - GlobalPosition) / _scaleToUse;
#else
                var mouse = AreaRect.TransformScreenPositionToLocal(EventManager.MousePosition) / _scaleToUse;
#endif
                return (mouse.X, mouse.Y);
            }
        }

        private void Changed()
        {
            _dirty = _showCaret = true;
            _caretLineHint = -1;
            _preferredX = -1;
            _blink = 0;
            OnPropertyChanged(_buffer.Text, nameof(Text));
        }

        private float Measure(string element)
        {
            float width = 0;
            foreach (char c in element)
            {
                var font = _font.Characters.ContainsKey(c) ? _font : Context.FontFactory.GetUsableFontForCharacter(c) ?? _font;
                width += font.GetCharacterWidth(c, 0.5f) * _fontSize / font.Size;
            }
            return width;
        }

        private void EnsureLayout()
        {
            if (Size.X <= 0 || Size.Y <= 0) return;
            var font = Context.FontFactory.GetMappedFontForLocalization((ReadOnlyBrush.Font ?? Context.FontFactory.DefaultFont).Name);
            float width = Math.Max(1, Size.X / _scaleToUse - 3);
            float fontSize = ReadOnlyBrush.FontSize;
            if (font != _font || Math.Abs(width - _width) > 0.1f || fontSize != _fontSize)
            {
                _font = font; _fontSize = fontSize; _width = width;
                _lineHeight = Math.Max(24, (_font.Base + 5) * fontSize / _font.Size + 4);
                _dirty = _showCaret = true;
            }
            _visibleLines = Math.Max(1, (int)(Size.Y / _scaleToUse / _lineHeight));
            if (_dirty)
            {
                _layout = new MultilineTextLayout(_buffer.Text, _width, Measure);
                _dirty = false;
            }
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            _blink += dt;
            _repeatDelay -= dt;
            _dragScrollDelay -= dt;
            EnsureLayout();
            if (_layout == null) return;
            if (!_autofocused)
            {
                _autofocused = true;
                FocusEditor();
            }
            if (_dragging)
            {
                if (!Input.IsKeyDown(InputKey.LeftMouseButton)) _dragging = false;
                else
                {
                    var mouse = LocalMousePosition;
                    if (_dragScrollDelay <= 0 && (mouse.Y < 0 || mouse.Y >= Size.Y / _scaleToUse))
                    { _topLine += mouse.Y < 0 ? -1 : 1; _dragScrollDelay = 0.07f; }
                    ClampScroll();
                    PlaceCaret(mouse.X, mouse.Y, true);
                }
            }
            if (VerticalScrollbar != null)
            {
                if (!_showCaret && !_dragging) _topLine = (int)Math.Round(VerticalScrollbar.ValueFloat);
                VerticalScrollbar.MaxValue = Math.Max(0, _layout.Lines.Count - _visibleLines);
                VerticalScrollbar.IsVisible = VerticalScrollbar.MaxValue > 0;
                VerticalScrollbar.DoNotUpdateHandleSize = true;
                if (VerticalScrollbar.Handle != null)
                    VerticalScrollbar.Handle.SuggestedHeight = Math.Max(24, VerticalScrollbar.Size.Y / _scaleToUse * Math.Min(1f, (float)_visibleLines / _layout.Lines.Count));
            }
            if (_showCaret)
            {
                int line = _layout.FindLine(_buffer.Caret, _caretLineHint);
                if (line < _topLine) _topLine = line;
                if (line >= _topLine + _visibleLines) _topLine = line - _visibleLines + 1;
                _showCaret = false;
            }
            ClampScroll();
            if (VerticalScrollbar != null) VerticalScrollbar.ValueFloat = _topLine;
            RenderRows();
        }

        private void ClampScroll() => _topLine = Math.Max(0, Math.Min(_topLine, Math.Max(0, _layout.Lines.Count - _visibleLines)));

        private Widget Rectangle(Color color)
        {
            return new Widget(Context)
            {
                ParentWidget = this, DoNotAcceptEvents = true,
                WidthSizePolicy = SizePolicy.Fixed, HeightSizePolicy = SizePolicy.Fixed,
                Sprite = Context.SpriteData.GetSprite("BlankWhiteSquare_9"), Color = color,
            };
        }

        private void RenderRows()
        {
            while (_rows.Count < _visibleLines)
            {
                var row = new Row { Selection = Rectangle(new Color(0.30f, 0.47f, 0.65f, 0.75f)) };
                row.Label = new TextWidget(Context)
                {
                    ParentWidget = this, DoNotAcceptEvents = true,
                    WidthSizePolicy = SizePolicy.StretchToParent, HeightSizePolicy = SizePolicy.Fixed,
                    Brush = ReadOnlyBrush, ClipContents = true,
                };
                _rows.Add(row);
            }
            if (_caret == null) _caret = Rectangle(new Color(0.95f, 0.84f, 0.65f));
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                bool visible = i < _visibleLines && i + _topLine < _layout.Lines.Count;
                row.Label.IsVisible = visible;
                row.Selection.IsVisible = false;
                if (!visible) continue;
                int lineNumber = i + _topLine;
                var line = _layout.Lines[lineNumber];
                row.Label.PositionYOffset = i * _lineHeight;
                row.Label.SuggestedHeight = _lineHeight;
                row.Label.Brush.FontSize = (int)_fontSize;
                row.Label.Brush.TextHorizontalAlignment = TextHorizontalAlignment.Left;
                row.Label.Brush.TextVerticalAlignment = TextVerticalAlignment.Top;
                row.Label.Text = _buffer.Text.Substring(line.Start, line.End - line.Start);
                int start = Math.Max(_buffer.SelectionStart, line.Start), end = Math.Min(_buffer.SelectionEnd, line.End);
                bool newline = _buffer.SelectionStart <= line.End && _buffer.SelectionEnd > line.End;
                if (end > start || (newline && line.Start == line.End))
                {
                    row.Selection.IsVisible = true;
                    row.Selection.PositionXOffset = _layout.X(lineNumber, start);
                    row.Selection.PositionYOffset = i * _lineHeight;
                    row.Selection.SuggestedWidth = Math.Max(3, _layout.X(lineNumber, end) - row.Selection.PositionXOffset + (newline ? 4 : 0));
                    row.Selection.SuggestedHeight = _lineHeight;
                }
            }
            int caretLine = _layout.FindLine(_buffer.Caret, _caretLineHint);
            _caret.IsVisible = IsFocused && caretLine >= _topLine && caretLine < _topLine + _visibleLines && _blink % 1f < 0.6f;
            _caret.PositionXOffset = _layout.X(caretLine, _buffer.Caret);
            _caret.PositionYOffset = (caretLine - _topLine) * _lineHeight;
            _caret.SuggestedWidth = 1.5f;
            _caret.SuggestedHeight = _lineHeight - 3;
        }

        private void Moved(bool keepColumn = false)
        {
            _showCaret = true; _blink = 0;
            if (!keepColumn) _preferredX = -1;
        }

        private void MoveVertical(int rows, bool select)
        {
            if (_layout == null) return;
            int line = _layout.FindLine(_buffer.Caret, _caretLineHint);
            if (_preferredX < 0) _preferredX = _layout.X(line, _buffer.Caret);
            _caretLineHint = Math.Max(0, Math.Min(_layout.Lines.Count - 1, line + rows));
            _buffer.MoveTo(_layout.HitTest(_caretLineHint, _preferredX), select);
            Moved(true);
        }

        private bool PressedOrRepeating(InputKey key)
        {
            if (Input.IsKeyPressed(key)) { _repeatKey = key; _repeatDelay = 0.4f; return true; }
            if (_repeatKey == key && Input.IsKeyDown(key) && _repeatDelay <= 0)
            { _repeatDelay = 0.045f; return true; }
            return false;
        }

        public override void HandleInput(IReadOnlyList<int> lastKeysPressed)
        {
            if (!IsFocused || IsDisabled) return;
            EnsureLayout();
            bool control = Control, shift = Shift;
            if (_repeatKey.HasValue && !Input.IsKeyDown(_repeatKey.Value)) _repeatKey = null;
            if (control)
            {
                if (Input.IsKeyPressed(InputKey.A)) { _buffer.SelectAll(); Moved(); return; }
                if (Input.IsKeyPressed(InputKey.C) || Input.IsKeyPressed(InputKey.X))
                {
                    if (_buffer.SelectionStart != _buffer.SelectionEnd)
                    {
                        Input.SetClipboardText(_buffer.SelectedText.Replace("\n", "\r\n"));
                        if (Input.IsKeyPressed(InputKey.X) && _buffer.Delete(true)) Changed();
                    }
                    return;
                }
                if (Input.IsKeyPressed(InputKey.V)) { if (_buffer.Insert(Input.GetClipboardText())) Changed(); return; }
                if (Input.IsKeyPressed(InputKey.Z)) { if (_buffer.Undo(shift)) Changed(); return; }
                if (Input.IsKeyPressed(InputKey.Y)) { if (_buffer.Undo(true)) Changed(); return; }
            }
            if (PressedOrRepeating(InputKey.Left) || PressedOrRepeating(InputKey.Right))
            {
                _buffer.MoveHorizontal(_repeatKey == InputKey.Left ? -1 : 1, shift, control);
                _caretLineHint = -1; Moved(); return;
            }
            if (PressedOrRepeating(InputKey.Up)) { MoveVertical(-1, shift); return; }
            if (PressedOrRepeating(InputKey.Down)) { MoveVertical(1, shift); return; }
            if (PressedOrRepeating(InputKey.PageUp)) { MoveVertical(-_visibleLines, shift); return; }
            if (PressedOrRepeating(InputKey.PageDown)) { MoveVertical(_visibleLines, shift); return; }
            if ((Input.IsKeyPressed(InputKey.Home) || Input.IsKeyPressed(InputKey.End)) && _layout != null)
            {
                bool end = Input.IsKeyPressed(InputKey.End);
                _caretLineHint = control ? (end ? _layout.Lines.Count - 1 : 0) : _layout.FindLine(_buffer.Caret, _caretLineHint);
                _buffer.MoveTo(end ? _layout.Lines[_caretLineHint].End : _layout.Lines[_caretLineHint].Start, shift);
                Moved(); return;
            }
            if (PressedOrRepeating(InputKey.BackSpace)) { if (_buffer.Delete(true, control)) Changed(); return; }
            if (PressedOrRepeating(InputKey.Delete)) { if (_buffer.Delete(false, control)) Changed(); return; }
            if (PressedOrRepeating(InputKey.Enter) || PressedOrRepeating(InputKey.NumpadEnter))
            { if (_buffer.Insert("\n")) Changed(); return; }
            if (control) return;
            var typed = new System.Text.StringBuilder();
            foreach (int key in lastKeysPressed)
                if (key >= 32 && key <= 0x10ffff && key != 127)
                {
                    if (key <= char.MaxValue) typed.Append((char)key);
                    else typed.Append(char.ConvertFromUtf32(key));
                }
            if (typed.Length > 0 && _buffer.Insert(typed.ToString())) Changed();
        }

        private void PlaceCaret(float x, float y, bool select)
        {
            if (_layout == null) return;
            _caretLineHint = Math.Max(0, Math.Min(_layout.Lines.Count - 1, _topLine + Math.Max(0, Math.Min(_visibleLines - 1, (int)Math.Floor(y / _lineHeight)))));
            _buffer.MoveTo(_layout.HitTest(_caretLineHint, x), select);
            Moved();
        }

        protected override void OnMousePressed()
        {
            base.OnMousePressed();
            FocusEditor();
            EnsureLayout();
            var mouse = LocalMousePosition;
            PlaceCaret(mouse.X, mouse.Y, Shift);
            _dragging = true;
        }

#if BL12 || BL13
        protected override void OnMouseReleased() { base.OnMouseReleased(); _dragging = false; }
#else
        protected override void OnMouseReleased(bool isFromInput) { base.OnMouseReleased(isFromInput); _dragging = false; }
#endif

        protected override void OnMouseScroll()
        {
            base.OnMouseScroll();
            if (_layout == null) return;
            _topLine -= Math.Sign(EventManager.DeltaMouseScroll) * 3;
            ClampScroll(); _showCaret = false;
            if (VerticalScrollbar != null) VerticalScrollbar.ValueFloat = _topLine;
        }

        protected override void OnGainFocus() { base.OnGainFocus(); HotkeyBlocker.SetBlocked(true); Moved(); }
        protected override void OnLoseFocus() { base.OnLoseFocus(); HotkeyBlocker.SetBlocked(false); _dragging = false; _repeatKey = null; }
        protected override void OnDisconnectedFromRoot()
        {
            if (EventManager.FocusedWidget == this) EventManager.ClearFocus();
            base.OnDisconnectedFromRoot();
        }
    }
}
