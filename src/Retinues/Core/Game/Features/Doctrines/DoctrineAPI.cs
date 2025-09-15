using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace Retinues.Core.Game.Features.Doctrines
{
    public static class DoctrineAPI
    {
        // ---------------------------------------------------------------------
        // Behavior access
        // ---------------------------------------------------------------------
        private static DoctrineServiceBehavior Svc =>
            Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();

        private static bool EnsureSvc(out DoctrineServiceBehavior svc)
        {
            svc = Svc;
            return svc != null;
        }

        // ---------------------------------------------------------------------
        // Discovery / catalog
        // ---------------------------------------------------------------------

        /// All doctrine definitions (ordered by Column, then Row).
        public static IReadOnlyList<DoctrineDef> AllDoctrines()
        {
            return EnsureSvc(out var svc)
                ? svc.AllDoctrines().ToList()
                : Array.Empty<DoctrineDef>();
        }

        /// Get a doctrine definition by type.
        public static DoctrineDef GetDoctrine<TDoctrine>()
            where TDoctrine : Doctrine => GetDoctrine(typeof(TDoctrine));

        /// Get a doctrine definition by type.
        public static DoctrineDef GetDoctrine(Type doctrineType)
        {
            if (doctrineType == null)
                return null;
            return GetDoctrine(doctrineType.FullName);
        }

        /// Get a doctrine definition by key (Type.FullName).
        public static DoctrineDef GetDoctrine(string doctrineKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(doctrineKey))
                return null;
            return svc.GetDoctrine(doctrineKey);
        }

        // ---------------------------------------------------------------------
        // Doctrine status / unlocks
        // ---------------------------------------------------------------------

        public static DoctrineStatus GetDoctrineStatus<TDoctrine>()
            where TDoctrine : Doctrine => GetDoctrineStatus(typeof(TDoctrine));

        public static DoctrineStatus GetDoctrineStatus(Type doctrineType)
        {
            if (!EnsureSvc(out var svc) || doctrineType == null)
                return DoctrineStatus.Locked;
            return svc.GetDoctrineStatus(doctrineType.FullName);
        }

        public static DoctrineStatus GetDoctrineStatus(string doctrineKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(doctrineKey))
                return DoctrineStatus.Locked;
            return svc.GetDoctrineStatus(doctrineKey);
        }

        public static bool IsDoctrineUnlocked<TDoctrine>()
            where TDoctrine : Doctrine => IsDoctrineUnlocked(typeof(TDoctrine));

        public static bool IsDoctrineUnlocked(Type doctrineType)
        {
            if (!EnsureSvc(out var svc) || doctrineType == null)
                return false;
            return svc.IsDoctrineUnlocked(doctrineType.FullName);
        }

        public static bool IsDoctrineUnlocked(string doctrineKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(doctrineKey))
                return false;
            return svc.IsDoctrineUnlocked(doctrineKey);
        }

        /// Try to acquire a doctrine (pays costs if eligible).
        public static bool TryAcquireDoctrine<TDoctrine>(out string reason)
            where TDoctrine : Doctrine => TryAcquireDoctrine(typeof(TDoctrine), out reason);

        public static bool TryAcquireDoctrine(Type doctrineType, out string reason)
        {
            if (!EnsureSvc(out var svc) || doctrineType == null)
            {
                reason = L.S("doctrine_service_unavailable", "Doctrine service is unavailable.");
                return false;
            }
            return svc.TryAcquireDoctrine(doctrineType.FullName, out reason);
        }

        public static bool TryAcquireDoctrine(string doctrineKey, out string reason)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(doctrineKey))
            {
                reason = L.S("doctrine_service_unavailable", "Doctrine service is unavailable.");
                return false;
            }
            return svc.TryAcquireDoctrine(doctrineKey, out reason);
        }

        // ---------------------------------------------------------------------
        // Feat progress (type-based)
        // ---------------------------------------------------------------------

        public static int GetFeatProgress<TFeat>()
            where TFeat : Feat => GetFeatProgress(typeof(TFeat));

        public static int GetFeatProgress(Type featType)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return 0;
            return svc.GetFeatProgress(featType.FullName);
        }

        public static int GetFeatTarget<TFeat>()
            where TFeat : Feat => GetFeatTarget(typeof(TFeat));

        public static int GetFeatTarget(Type featType)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return 0;
            return svc.GetFeatTarget(featType.FullName);
        }

        public static bool IsFeatComplete<TFeat>()
            where TFeat : Feat => IsFeatComplete(typeof(TFeat));

        public static bool IsFeatComplete(Type featType)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return false;
            return svc.IsFeatComplete(featType.FullName);
        }

        public static void SetFeatProgress<TFeat>(int amount)
            where TFeat : Feat => SetFeatProgress(typeof(TFeat), amount);

        public static void SetFeatProgress(Type featType, int amount)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return;
            svc.SetFeatProgress(featType.FullName, amount);
        }

        public static int AdvanceFeat<TFeat>(int amount = 1)
            where TFeat : Feat => AdvanceFeat(typeof(TFeat), amount);

        public static int AdvanceFeat(Type featType, int amount = 1)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return 0;
            return svc.AdvanceFeat(featType.FullName, amount);
        }

        // ---------------------------------------------------------------------
        // Feat progress (string-key overloads for VMs, etc.)
        // ---------------------------------------------------------------------

        public static int GetFeatProgress(string featKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return 0;
            return svc.GetFeatProgress(featKey);
        }

        public static int GetFeatTarget(string featKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return 0;
            return svc.GetFeatTarget(featKey);
        }

        public static bool IsFeatComplete(string featKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return false;
            return svc.IsFeatComplete(featKey);
        }

        public static void SetFeatProgress(string featKey, int amount)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return;
            svc.SetFeatProgress(featKey, amount);
        }

        public static int AdvanceFeat(string featKey, int amount = 1)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return 0;
            return svc.AdvanceFeat(featKey, amount);
        }

        // ---------------------------------------------------------------------
        // Events passthrough (optional helpers)
        // ---------------------------------------------------------------------

        /// Add a listener for doctrine unlocked (key = Doctrine type full name).
        public static void AddDoctrineUnlockedListener(Action<string> listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.DoctrineUnlocked += listener;
        }

        public static void RemoveDoctrineUnlockedListener(Action<string> listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.DoctrineUnlocked -= listener;
        }

        public static void AddCatalogBuiltListener(Action listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.CatalogBuilt += listener;
        }

        /// Add a listener for feat completed (key = Feat type full name).
        public static void AddFeatCompletedListener(Action<string> listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.FeatCompleted += listener;
        }

        public static void RemoveFeatCompletedListener(Action<string> listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.FeatCompleted -= listener;
        }
    }
}
