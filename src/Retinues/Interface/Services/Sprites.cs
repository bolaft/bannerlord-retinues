using System.Linq;
using Retinues.Framework.Runtime;
using Retinues.Utilities;
using TaleWorlds.Engine.GauntletUI;

namespace Retinues.Interface.Services
{
    /// <summary>
    /// Helpers for loading sprite resources.
    /// </summary>
    [SafeClass]
    public static class Sprites
    {
        /// <summary>
        /// Returns true if the game has a skill icon sprite for the given skill id. Uses the exact
        /// lookup the native SkillIconVisualWidget performs, so a skill added by another mod that
        /// ships no icon returns false and the UI can fall back to a text label instead of a blank.
        /// </summary>
        public static bool HasSkillIcon(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
                return false;

            try
            {
                var data = UIResourceManager.SpriteData;
                if (data == null)
                    return true; // Sprite data not ready: assume an icon exists (no worse than before).

                return data.GetSprite("SPGeneral\\Skills\\gui_skills_icon_" + skillId.ToLower())
                    != null;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Loads the specified sprite categories into memory.
        /// </summary>
        public static void Load(params string[] names)
        {
            Log.Debug($"Loading sprites {string.Join(", ", names)}...");

            var data = UIResourceManager.SpriteData;
            var context = UIResourceManager.ResourceContext;

#if BL13 || BL14
            var depot = UIResourceManager.ResourceDepot;
#else
            var depot = UIResourceManager.UIResourceDepot;
#endif

            // Load each requested category if not already loaded
            foreach (var name in names.Distinct())
                if (data.SpriteCategories.TryGetValue(name, out var category) && !category.IsLoaded)
                    category.Load(context, depot);

            Log.Debug("Sprites loaded.");
        }
    }
}
