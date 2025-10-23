using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using Retinues.Utils;

namespace Retinues.GUI
{
    public static class SpriteLoader
    {
        public static void LoadAllCategories()
        {
            var spriteData = UIResourceManager.SpriteData;
            var ctx = UIResourceManager.ResourceContext;
#if BL13
            var depot = UIResourceManager.ResourceDepot;
#else
            var depot = UIResourceManager.UIResourceDepot;
#endif

            foreach (var kv in spriteData.SpriteCategories)
            {
                var cat = kv.Value;
                if (!cat.IsLoaded)
                {
                    Log.Info($"Loading sprite category '{cat.Name}'...");
                    cat.Load(ctx, depot);
                }
            }
        }

        // If you prefer a whitelist instead of “all”:
        public static void LoadCategories(params string[] names)
        {
            var spriteData = UIResourceManager.SpriteData;
            var ctx = UIResourceManager.ResourceContext;
#if BL13
            var depot = UIResourceManager.ResourceDepot;
#else
            var depot = UIResourceManager.UIResourceDepot;
#endif

            foreach (var name in names.Distinct())
            {
                if (spriteData.SpriteCategories.TryGetValue(name, out var cat) && !cat.IsLoaded)
                {
                    Log.Info($"Loading sprite category '{name}'...");
                    cat.Load(ctx, depot);
                }
            }
        }
    }
}
