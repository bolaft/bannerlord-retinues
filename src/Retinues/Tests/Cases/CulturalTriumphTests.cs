using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Retinues.Behaviors.Doctrines.Feats.Loot;
using Retinues.Domain.Characters.Wrappers;
using Retinues.Domain.Events.Models;
using Retinues.Domain.Factions.Wrappers;
using Retinues.Framework.Model;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    public static class CulturalTriumphTests
    {
        static MMapEvent.SideData Side(string leaderPartyId, params MMapEvent.PartyData[] parties) =>
            new(BattleSideEnum.Defender, false, true, leaderPartyId, 100, 100, 1, 100f, parties);

        static MMapEvent.PartyData Party(string partyId, string leaderId) =>
            new(partyId, leaderId, 100, 100, 0, 0f, 0f, 0, 0);

        static T Empty<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));

        [GameTest("CulturalTriumphRejectsMissingLeaderData", "community", RequiresCampaign = false)]
        public static void CulturalTriumphRejectsMissingLeaderData()
        {
            var culture = new WCulture(Empty<CultureObject>());
            bool Qualifies(MMapEvent.SideData side) =>
                Feat_AncestralHeritage_CulturalTriumph.HasDifferentLeaderCulture(side, culture);

            Tests.AssertFalse(Qualifies(null), "A missing enemy side cannot qualify.");
            Tests.AssertFalse(Qualifies(Side(null, Party(null, null))),
                "A missing party ID cannot accidentally match a null snapshot ID.");
            Tests.AssertFalse(Qualifies(Side("", Party("", null))),
                "An empty party ID cannot identify the army leader.");
            Tests.AssertFalse(Qualifies(Side("leader")), "An empty snapshot cannot qualify.");
            Tests.AssertFalse(Qualifies(Side("leader", null, Party("other", null))),
                "Null entries and an absent leader entry must not throw.");
            Tests.AssertFalse(Qualifies(Side("leader", Party("leader", null))),
                "An entry without a captured hero cannot qualify.");
            Tests.AssertFalse(Qualifies(Side("leader", Party("leader", ""))),
                "An empty hero ID cannot qualify.");
        }

        [GameTest("CulturalTriumphUsesCapturedHeroCulture", "community", RequiresCampaign = false)]
        public static void CulturalTriumphUsesCapturedHeroCulture()
        {
            // Use managed fixtures only; the captured party deliberately does not exist.
            // Preserve the resolver and remove only this test's unique cache entries afterward.
            RuntimeHelpers.RunClassConstructor(typeof(WHero).TypeHandle);
            var resolvers = (Dictionary<Type, Func<string, MBObjectBase>>)typeof(WBase<WHero, Hero>)
                .GetField("Resolvers", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var originalResolver = resolvers[typeof(Hero)];
            var cultureCache = (Dictionary<string, WCulture>)typeof(WBase<WCulture, CultureObject>)
                .GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var prefix = "retinues_cultural_triumph_test_" + Guid.NewGuid().ToString("N");
            var playerCulture = Empty<CultureObject>();
            playerCulture.StringId = prefix + "_player";
            var enemyCulture = Empty<CultureObject>();
            enemyCulture.StringId = prefix + "_enemy";
            var hero = Empty<Hero>(); // No StringId: the wrapper is not cached.
            var heroId = prefix + "_hero";
            var side = Side(prefix + "_party", null, Party(prefix + "_party", heroId));
            var player = new WCulture(playerCulture);
            try
            {
                WHero.RegisterResolver<Hero>(id => id == heroId ? hero : originalResolver(id) as Hero);
                hero.Culture = enemyCulture;
                Tests.AssertTrue(Feat_AncestralHeritage_CulturalTriumph.HasDifferentLeaderCulture(side, player),
                    "A captured hero of a different culture still qualifies after their party disappears.");
                hero.Culture = playerCulture;
                Tests.AssertFalse(Feat_AncestralHeritage_CulturalTriumph.HasDifferentLeaderCulture(side, player),
                    "A known leader of the same culture cannot qualify.");
                hero.Culture = null;
                Tests.AssertFalse(Feat_AncestralHeritage_CulturalTriumph.HasDifferentLeaderCulture(side, player),
                    "A hero with an unknown culture cannot qualify.");
                hero.Culture = enemyCulture;
                Tests.AssertFalse(Feat_AncestralHeritage_CulturalTriumph.HasDifferentLeaderCulture(side, null),
                    "An unknown player culture cannot qualify.");
                WHero.RegisterResolver<Hero>(id => id == heroId ? null : originalResolver(id) as Hero);
                Tests.AssertFalse(Feat_AncestralHeritage_CulturalTriumph.HasDifferentLeaderCulture(side, player),
                    "An unresolved captured hero must not throw or award progress.");
            }
            finally
            {
                resolvers[typeof(Hero)] = originalResolver;
                cultureCache.Remove(playerCulture.StringId);
                cultureCache.Remove(enemyCulture.StringId);
            }
        }
    }
}
