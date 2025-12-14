using System;
using System.Collections.Generic;
using System.Linq;
using Retinues.Doctrines.Model;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace OldRetinues.Doctrines
{
    /// <summary>
    /// Static API for doctrine and feat management. Provides helpers for querying, unlocking, progressing, and listening to doctrine/feat events.
    /// </summary>
    [SafeClass]
    public static class DoctrineAPI
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Behavior Access                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private static DoctrineServiceBehavior Svc =>
            Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();

        private static bool EnsureSvc(out DoctrineServiceBehavior svc)
        {
            svc = Svc;
            return svc != null;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                   Discovery / Catalog                  //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Get all doctrine definitions, ordered by column and row.
        /// </summary>
        public static IReadOnlyList<DoctrineDefinition> AllDoctrines()
        {
            return EnsureSvc(out var svc)
                ? svc.AllDoctrines().ToList()
                : Array.Empty<DoctrineDefinition>();
        }

        /// <summary>
        /// Get a doctrine definition by type.
        /// </summary>
        public static DoctrineDefinition GetDoctrine<TDoctrine>()
            where TDoctrine : Doctrine => GetDoctrine(typeof(TDoctrine));

        /// <summary>
        /// Get a doctrine definition by type.
        /// </summary>
        public static DoctrineDefinition GetDoctrine(Type doctrineType)
        {
            if (doctrineType == null)
                return null;
            return GetDoctrine(doctrineType.FullName);
        }

        /// <summary>
        /// Get a doctrine definition by key (Type.FullName).
        /// </summary>
        public static DoctrineDefinition GetDoctrine(string doctrineKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(doctrineKey))
                return null;
            return svc.GetDoctrine(doctrineKey);
        }

        /// <summary>
        /// Get doctrine status by type.
        /// </summary>
        public static DoctrineStatus GetDoctrineStatus<TDoctrine>()
            where TDoctrine : Doctrine => GetDoctrineStatus(typeof(TDoctrine));

        /// <summary>
        /// Get doctrine status by type.
        /// </summary>
        public static DoctrineStatus GetDoctrineStatus(Type doctrineType)
        {
            if (!EnsureSvc(out var svc) || doctrineType == null)
                return DoctrineStatus.Locked;
            return svc.GetDoctrineStatus(doctrineType.FullName);
        }

        /// <summary>
        /// Get doctrine status by key.
        /// </summary>
        public static DoctrineStatus GetDoctrineStatus(string doctrineKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(doctrineKey))
                return DoctrineStatus.Locked;
            return svc.GetDoctrineStatus(doctrineKey);
        }

        /// <summary>
        /// Returns true if the doctrine is unlocked.
        /// </summary>
        public static bool IsDoctrineUnlocked<TDoctrine>()
            where TDoctrine : Doctrine => IsDoctrineUnlocked(typeof(TDoctrine));

        /// <summary>
        /// Returns true if the doctrine is unlocked.
        /// </summary>
        public static bool IsDoctrineUnlocked(Type doctrineType)
        {
            if (!EnsureSvc(out var svc) || doctrineType == null)
                return false;
            return svc.IsDoctrineUnlocked(doctrineType.FullName);
        }

        /// <summary>
        /// Returns true if the doctrine is unlocked.
        /// </summary>
        public static bool IsDoctrineUnlocked(string doctrineKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(doctrineKey))
                return false;
            return svc.IsDoctrineUnlocked(doctrineKey);
        }

        /// <summary>
        /// Try to acquire a doctrine (pays costs if eligible).
        /// </summary>
        public static bool TryAcquireDoctrine<TDoctrine>(out string reason)
            where TDoctrine : Doctrine => TryAcquireDoctrine(typeof(TDoctrine), out reason);

        /// <summary>
        /// Try to acquire a doctrine (pays costs if eligible).
        /// </summary>
        public static bool TryAcquireDoctrine(Type doctrineType, out string reason)
        {
            if (!EnsureSvc(out var svc) || doctrineType == null)
            {
                reason = L.S("doctrine_service_unavailable", "Doctrine service is unavailable.");
                return false;
            }
            return svc.TryAcquireDoctrine(doctrineType.FullName, out reason);
        }

        /// <summary>
        /// Try to acquire a doctrine (pays costs if eligible).
        /// </summary>
        public static bool TryAcquireDoctrine(string doctrineKey, out string reason)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(doctrineKey))
            {
                reason = L.S("doctrine_service_unavailable", "Doctrine service is unavailable.");
                return false;
            }
            return svc.TryAcquireDoctrine(doctrineKey, out reason);
        }

        /// <summary>
        /// Get feat progress by type.
        /// </summary>
        public static int GetFeatProgress<TFeat>()
            where TFeat : Feat => GetFeatProgress(typeof(TFeat));

        /// <summary>
        /// Get feat progress by type.
        /// </summary>
        public static int GetFeatProgress(Type featType)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return 0;
            return svc.GetFeatProgress(featType.FullName);
        }

        /// <summary>
        /// Get feat target by type.
        /// </summary>
        public static int GetFeatTarget<TFeat>()
            where TFeat : Feat => GetFeatTarget(typeof(TFeat));

        /// <summary>
        /// Get feat target by type.
        /// </summary>
        public static int GetFeatTarget(Type featType)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return 0;
            return svc.GetFeatTarget(featType.FullName);
        }

        /// <summary>
        /// Returns true if the feat is complete.
        /// </summary>
        public static bool IsFeatComplete<TFeat>()
            where TFeat : Feat => IsFeatComplete(typeof(TFeat));

        /// <summary>
        /// Returns true if the feat is complete.
        /// </summary>
        public static bool IsFeatComplete(Type featType)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return false;
            return svc.IsFeatComplete(featType.FullName);
        }

        /// <summary>
        /// Set feat progress by type.
        /// </summary>
        public static void SetFeatProgress<TFeat>(int amount)
            where TFeat : Feat => SetFeatProgress(typeof(TFeat), amount);

        /// <summary>
        /// Set feat progress by type.
        /// </summary>
        public static void SetFeatProgress(Type featType, int amount)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return;
            svc.SetFeatProgress(featType.FullName, amount);
        }

        /// <summary>
        /// Advance feat progress by type.
        /// </summary>
        public static int AdvanceFeat<TFeat>(int amount = 1)
            where TFeat : Feat => AdvanceFeat(typeof(TFeat), amount);

        /// <summary>
        /// Advance feat progress by type.
        /// </summary>
        public static int AdvanceFeat(Type featType, int amount = 1)
        {
            if (!EnsureSvc(out var svc) || featType == null)
                return 0;
            return svc.AdvanceFeat(featType.FullName, amount);
        }

        /// <summary>
        /// Get feat progress by key.
        /// </summary>
        public static int GetFeatProgress(string featKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return 0;
            return svc.GetFeatProgress(featKey);
        }

        /// <summary>
        /// Get feat target by key.
        /// </summary>
        public static int GetFeatTarget(string featKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return 0;
            return svc.GetFeatTarget(featKey);
        }

        /// <summary>
        /// Returns true if the feat is complete.
        /// </summary>
        public static bool IsFeatComplete(string featKey)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return false;
            return svc.IsFeatComplete(featKey);
        }

        /// <summary>
        /// Set feat progress by key.
        /// </summary>
        public static void SetFeatProgress(string featKey, int amount)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return;
            svc.SetFeatProgress(featKey, amount);
        }

        /// <summary>
        /// Advance feat progress by key.
        /// </summary>
        public static int AdvanceFeat(string featKey, int amount = 1)
        {
            if (!EnsureSvc(out var svc) || string.IsNullOrEmpty(featKey))
                return 0;
            return svc.AdvanceFeat(featKey, amount);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                     Events Helpers                     //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Add a listener for doctrine unlocked (key = Doctrine type full name).
        /// </summary>
        public static void AddDoctrineUnlockedListener(Action<string> listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.DoctrineUnlocked += listener;
        }

        /// <summary>
        /// Remove a listener for doctrine unlocked.
        /// </summary>
        public static void RemoveDoctrineUnlockedListener(Action<string> listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.DoctrineUnlocked -= listener;
        }

        /// <summary>
        /// Add a listener for catalog built.
        /// </summary>
        public static void AddCatalogBuiltListener(Action listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.CatalogBuilt += listener;
        }

        /// <summary>
        /// Add a listener for feat completed (key = Feat type full name).
        /// </summary>
        public static void AddFeatCompletedListener(Action<string> listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.FeatCompleted += listener;
        }

        /// <summary>
        /// Remove a listener for feat completed.
        /// </summary>
        public static void RemoveFeatCompletedListener(Action<string> listener)
        {
            if (!EnsureSvc(out var svc) || listener == null)
                return;
            svc.FeatCompleted -= listener;
        }
    }
}
