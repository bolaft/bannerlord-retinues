using System;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Features.Doctrines
{
    /// <summary>Static helpers so other systems can just call Doctrines.* from anywhere.</summary>
    public static class DoctrineAPI
    {
        private static DoctrineServiceBehavior Svc
            => Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();

        public static bool IsDoctrineUnlocked(string id) => Svc?.IsDoctrineUnlocked(id) == true;

        public static DoctrineStatus GetDoctrineStatus(string id)
            => Svc != null ? Svc.GetDoctrineStatus(id) : DoctrineStatus.Locked;

        public static bool TryAcquireDoctrine(string id, out string reason)
        {
            if (Svc != null)
            {
                return Svc.TryAcquireDoctrine(id, out reason);
            }
            reason = "Doctrine service is unavailable.";
            return false;
        }

        public static bool IsFeatComplete(string featId) => Svc?.IsFeatComplete(featId) == true;

        public static int GetFeatProgress(string featId) => Svc?.GetFeatProgress(featId) ?? 0;

        public static int GetFeatTarget(string featId) => Svc?.GetFeatTarget(featId) ?? 0;

        public static int AdvanceFeat(string featId, int amount = 1) => Svc?.AdvanceFeat(featId, amount) ?? 0;

        public static void RegisterFeat(string featId, string description, int target, string doctrineId)
            => Svc?.RegisterFeat(featId, description, target, doctrineId);

        public static event Action<string> DoctrineUnlocked
        {
            add { if (Svc != null) Svc.DoctrineUnlocked += value; }
            remove { if (Svc != null) Svc.DoctrineUnlocked -= value; }
        }

        public static event Action<string> FeatCompleted
        {
            add { if (Svc != null) Svc.FeatCompleted += value; }
            remove { if (Svc != null) Svc.FeatCompleted -= value; }
        }
    }
}
