using System;
using System.Collections.Generic;
using Retinues.Doctrines;
using Retinues.Doctrines.Model;
using Retinues.Utils;
using TaleWorlds.CampaignSystem;

namespace Retinues.Tests
{
    /// <summary>
    /// Test helper for forcing doctrine unlock state. Adds the doctrine key directly to the
    /// service's private unlocked set (bypassing feat/cost eligibility). Always pair with a
    /// TestSandbox so the unlock is restored on dispose.
    /// </summary>
    public static class TestDoctrines
    {
        public static void Unlock<T>()
            where T : Doctrine => Unlock(typeof(T));

        public static void Unlock(Type doctrineType)
        {
            if (doctrineType == null)
                return;

            var svc = Campaign.Current?.GetCampaignBehavior<DoctrineServiceBehavior>();
            if (svc == null)
                return;

            var unlocked = Reflector.GetFieldValue<HashSet<string>>(svc, "_unlocked");
            unlocked?.Add(doctrineType.FullName);
        }
    }
}
