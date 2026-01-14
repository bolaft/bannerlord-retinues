using Retinues.Editor.Events;
using Retinues.Game.Doctrines;
using Retinues.UI.Services;
using Retinues.UI.VM;
using TaleWorlds.Library;

namespace Retinues.Editor.VM.Panel.Doctrines
{
    /// <summary>
    /// Doctrine feat card shown in the doctrines panel.
    /// </summary>
    public sealed class FeatVM(DoctrineFeatLink link, FeatDefinition def) : EventListenerVM
    {
        [DataSourceProperty]
        public bool IsCompleted => !def.Repeatable && FeatsAPI.IsCompleted(def.Id);

        [DataSourceProperty]
        public bool IsAcquired => DoctrinesAPI.IsAcquired(link.DoctrineId);

        [DataSourceProperty]
        public string Name => def.Name?.ToString() ?? string.Empty;

        [DataSourceProperty]
        public string Description => def.Description?.ToString() ?? string.Empty;

        [DataSourceProperty]
        public string WorthText => $"+{link.Worth}%";

        [DataSourceProperty]
        public string ProgressText
        {
            get
            {
                if (def == null)
                    return string.Empty;

                var t = def.Target;

                if (IsCompleted)
                    return $"{t}/{t}";

                var p = FeatsAPI.GetProgress(def.Id);

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
            if (def == null)
                return string.Empty;

            if (def.Repeatable)
                return "StdAssets\\switch_default";

            return IsCompleted ? "StdAssets\\checkbox_full" : "StdAssets\\checkbox_empty";
        }

        private Tooltip ResolveIconTooltip()
        {
            if (def == null)
                return null;

            if (def.Repeatable)
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
