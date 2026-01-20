using Retinues.Behaviors.Doctrines.Definitions;
using Retinues.Editor.Events;
using Retinues.Interface.Components;
using Retinues.Interface.Services;
using TaleWorlds.Library;

namespace Retinues.Editor.MVC.Pages.Doctrines.Views.Panel
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

        /// <summary>
        /// Resolves the sprite for the feat progress icon.
        /// </summary>
        private string ResolveIconSprite()
        {
            if (feat.Repeatable)
                return "StdAssets\\switch_default";

            return IsCompleted ? "StdAssets\\checkbox_full" : "StdAssets\\checkbox_empty";
        }

        /// <summary>
        /// Resolves the tooltip for the feat progress icon.
        /// </summary>
        /// <returns></returns>
        private Tooltip ResolveIconTooltip()
        {
            if (feat.Repeatable)
            {
                return new Tooltip(
                    L.T(
                        "doctrine_feat_progress_tooltip_repeatable",
                        "Repeatable"
                    )
                );
            }

            if (IsCompleted)
            {
                return new Tooltip(
                    L.T("doctrine_feat_progress_tooltip_completed", "Completed")
                );
            }

            return new Tooltip(
                L.T(
                    "doctrine_feat_progress_tooltip_incomplete",
                    "In Progress"
                )
            );
        }
    }
}
