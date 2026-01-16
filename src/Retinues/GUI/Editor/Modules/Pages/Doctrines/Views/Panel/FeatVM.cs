using Retinues.Editor.Events;
using Retinues.Game.Doctrines.Definitions;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Doctrines
{
    /// <summary>
    /// Doctrine feat card shown in the doctrines panel.
    /// </summary>
    public sealed class FeatVM(Feat feat) : EventListenerVM
    {
        [DataSourceProperty]
        public bool IsCompleted => !feat.Repeatable && feat.IsCompleted;

        [DataSourceProperty]
        public bool IsAcquired => feat.Doctrine.IsAcquired;

        [DataSourceProperty]
        public string Name => feat.Name.ToString();

        [DataSourceProperty]
        public string Description => feat.Description.ToString();

        [DataSourceProperty]
        public string WorthText => $"+{feat.Worth}%";

        [DataSourceProperty]
        public string ProgressText
        {
            get
            {
                var t = feat.Target;

                if (IsCompleted)
                    return $"{t}/{t}";

                var p = feat.Progress;

                return $"{p}/{t}";
            }
        }

        [DataSourceProperty]
        public Icon FeatProgressIcon =>
            new(
                tooltipFactory: ResolveIconTooltip,
                spriteFactory: ResolveIconSprite,
                refresh: [UIEvent.Doctrine]
            );

        private string ResolveIconSprite()
        {
            if (feat.Repeatable)
                return "StdAssets\\switch_default";

            return IsCompleted ? "StdAssets\\checkbox_full" : "StdAssets\\checkbox_empty";
        }

        private Tooltip ResolveIconTooltip()
        {
            if (feat.Repeatable)
            {
                return new Tooltip(
                    L.T(
                        "doctrine_feat_progress_tooltip_repeatable",
                        "This feat is repeatable. You can complete it multiple times."
                    )
                );
            }

            if (IsCompleted)
            {
                return new Tooltip(
                    L.T("doctrine_feat_progress_tooltip_completed", "You have completed this feat.")
                );
            }

            return new Tooltip(
                L.T(
                    "doctrine_feat_progress_tooltip_incomplete",
                    "You have not yet completed this feat."
                )
            );
        }
    }
}
