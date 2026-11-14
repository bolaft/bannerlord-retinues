using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Retinues.Interface.Widgets
{
    /// <summary>
    /// Keeps the equipment panel beside the left-anchored preview on ultrawide screens.
    /// Other pages and viewports no wider than 16:9 retain their original layout.
    /// </summary>
    public sealed class RetinuesEditorPanelWidget(UIContext context) : Widget(context)
    {
        [Editor(false)]
        public bool KeepWithPreview { get; set; }

        private float? _normalRightMargin;

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            // Capture the prefab margin once, before applying any extra inset. Deriving
            // each frame from it also restores the layout after a resize or page change.
            _normalRightMargin ??= MarginRight;
            float inset = 0f;
            var viewport = Context.TwoDimensionContext;
            if (KeepWithPreview && viewport != null)
            {
                float width = viewport.Width;
                float height = viewport.Height;
                float inverseScale = _inverseScaleToUse;
                if (width > 0f && height > 0f && inverseScale > 0f
                    && !float.IsInfinity(width) && !float.IsInfinity(height)
                    && !float.IsInfinity(inverseScale))
                {
                    // Use the full viewport, not the shorter editor body. Cross-multiply
                    // to leave exact 16:9 sizes unchanged, including with custom UI scale.
                    double extraWidth = (width * 9d - height * 16d) / 9d;
                    // Ignore pixel rounding in nominal 16:9 modes such as 1366x768.
                    if (extraWidth > 1d)
                        inset = (float)(extraWidth * inverseScale);
                }
            }

            // Move the parent with its controls so their input bounds move with them.
            MarginRight = _normalRightMargin.Value + inset;
        }
    }
}
