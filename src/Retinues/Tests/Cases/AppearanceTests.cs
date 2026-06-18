using Retinues.Game;
using Retinues.Game.Helpers;
using Retinues.Game.Wrappers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Retinues.Tests.Cases
{
    /// <summary>
    /// Tests for culture-driven appearance resets. Guard the bug where switching a troop's culture
    /// left the previous culture's tags (tattoos/hairstyle) in place, and where minor/ancestral
    /// cultures with no troops of their own (e.g. Nord, Vakken) reset nothing at all.
    /// </summary>
    public static class AppearanceTests
    {
        /// <summary>
        /// ApplyTagsFromCulture overwrites every tag category with the template's value — including
        /// clearing a stale category the new culture does not define — rather than leaving it.
        /// </summary>
        [GameTest(
            "TagsOverwriteStaleCategory",
            "appearance",
            "Switching culture overwrites stale tag categories instead of leaving the old ones"
        )]
        public static void TagsOverwriteStaleCategory(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var culture = Player.Clan?.Culture?.Base;
            Tests.AssertNotNull(culture, "Player culture is available.");

            var tmpl = culture.BasicTroop ?? culture.EliteBasicTroop;
            Tests.AssertNotNull(tmpl, "Player culture has a basic/elite template.");

            // The value the culture template actually defines (may legitimately be empty).
            var expectedTattoo = tmpl.BodyPropertyRange?.TattooTags ?? string.Empty;

            using var sandbox = new TestSandbox();

            var troop = sandbox.NewStub();
            troop.FillFrom(
                new WCharacter(tmpl),
                keepUpgrades: false,
                keepEquipment: false,
                keepSkills: false
            );
            troop.Culture = new WCulture(culture);
            troop.Body.EnsureOwnBodyRange();

            // Plant a stale tattoo tag as if carried over from a previously-selected culture.
            const string stale = "RetinuesTestStaleTattoo";
            troop.Base.BodyPropertyRange.TattooTags = stale;

            BodyHelper.ApplyTagsFromCulture(troop);

            Tests.AssertEqual(
                expectedTattoo,
                troop.Base.BodyPropertyRange.TattooTags ?? string.Empty,
                "Stale tattoo tag is overwritten with the culture template's value (cleared if empty)."
            );
        }

        /// <summary>
        /// GetCultureTemplate prefers the culture's BasicTroop, and for a troop-less culture (no
        /// BasicTroop/EliteBasicTroop) resolves to null or a same-culture fallback without throwing.
        /// </summary>
        [GameTest(
            "CultureTemplateFallback",
            "appearance",
            "GetCultureTemplate prefers BasicTroop and falls back safely for troop-less cultures"
        )]
        public static void CultureTemplateFallback(GameTestContext ctx)
        {
            ctx.EnsureCampaign();

            var culture = Player.Clan?.Culture?.Base;
            Tests.AssertNotNull(culture, "Player culture is available.");

            if (culture.BasicTroop != null)
                Tests.AssertTrue(
                    ReferenceEquals(culture.BasicTroop, BodyHelper.GetCultureTemplate(culture)),
                    "GetCultureTemplate prefers the culture's BasicTroop."
                );

            // Opportunistic: if a troop-less culture exists (e.g. Nord/Vakken), its template must
            // resolve to null or a troop of that same culture — never a throw or a foreign troop.
            foreach (var c in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
            {
                if (c == null || c.BasicTroop != null || c.EliteBasicTroop != null)
                    continue;

                var resolved = BodyHelper.GetCultureTemplate(c);
                if (resolved != null)
                    Tests.AssertTrue(
                        ReferenceEquals(resolved.Culture, c),
                        $"Fallback template for troop-less culture '{c.StringId}' belongs to it."
                    );
                break;
            }
        }
    }
}
