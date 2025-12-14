using System.Linq;
using Retinues.Utilities;
using TaleWorlds.Engine.GauntletUI;

namespace Retinues.Helpers
{
    [SafeClass]
    public static class Sprites
    {
        /// <summary>
        /// Loads the specified sprite categories into memory.
        /// </summary>
        public static void Load(params string[] names)
        {
            Log.Info($"Loading sprites {string.Join(", ", names)}...");

            var data = UIResourceManager.SpriteData;
            var context = UIResourceManager.ResourceContext;

#if BL13
            var depot = UIResourceManager.ResourceDepot;
#else
            var depot = UIResourceManager.UIResourceDepot;
#endif

            // Load each requested category if not already loaded
            foreach (var name in names.Distinct())
                if (data.SpriteCategories.TryGetValue(name, out var category) && !category.IsLoaded)
                    category.Load(context, depot);

            Log.Info("Sprites loaded.");
        }
    }
}
