using Retinues.Framework.Runtime;
#if BL13 || BL14
using System;
using System.IO;
using Retinues.Utilities;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TwoD = TaleWorlds.TwoDimension;
#endif

namespace Retinues.Editor.Integration.MapBar
{
#if BL13 || BL14
    /// <summary>
    /// A sprite backed directly by a runtime-loaded texture, with no sprite sheet or compiled
    /// asset package behind it. Lets us show a custom PNG through the normal brush pipeline
    /// without shipping a SpriteData category + .tpac.
    /// </summary>
    internal sealed class RuntimeTextureSprite : TwoD.Sprite
    {
        private readonly TwoD.Texture _texture;

        public RuntimeTextureSprite(string name, int width, int height, TwoD.Texture texture)
            : base(name, width, height, new TwoD.SpriteNinePatchParameters(0, 0, 0, 0))
        {
            _texture = texture;
        }

        public override TwoD.Texture Texture => _texture;

        public override Vec2 GetMinUvs() => new(0f, 0f);

        public override Vec2 GetMaxUvs() => new(1f, 1f);
    }
#endif

    /// <summary>
    /// Loads the custom Troops map-bar icon from an embedded PNG and assigns it to the map-bar
    /// brush layer at runtime, replacing the borrowed vanilla placeholder sprite.
    /// </summary>
    /// <remarks>
    /// BL13/BL14 only: BL12's 2D sprite API requires building draw meshes by hand
    /// (Sprite.GetArrays), so the BL12 button keeps the borrowed placeholder icon.
    /// </remarks>
    [SafeClass]
    internal static class TroopsIcon
    {
#if BL13 || BL14
        // LogicalName set on the EmbeddedResource in Retinues.csproj.
        private const string ResourceName = "Retinues.troops_icon.png";

        // Match the vanilla nav icon area (IconBrushWidget SuggestedWidth/Height in MapBar.xml).
        // The embedded PNG is the same 3:2 aspect so it is not distorted when drawn here.
        private const int IconWidth = 60;
        private const int IconHeight = 40;

        // BL13/BL14 share one icon brush keyed by navigation id; we add a "troops" layer.
        private const string BrushName = "MapBar.Left.Icons";
        private const string LayerName = "troops";

        private static TwoD.Sprite _sprite;
        private static bool _loadFailed;

        /// <summary>
        /// Loads (once) and returns the custom icon sprite, or null if it could not be loaded.
        /// </summary>
        private static TwoD.Sprite GetSprite()
        {
            if (_sprite != null || _loadFailed)
                return _sprite;

            try
            {
                var asm = typeof(TroopsIcon).Assembly;
                using var stream = asm.GetManifestResourceStream(ResourceName);
                if (stream == null)
                {
                    Log.Warning($"Troops icon resource '{ResourceName}' not found in assembly.");
                    _loadFailed = true;
                    return null;
                }

                using var ms = new MemoryStream();
                stream.CopyTo(ms);

                var engineTexture = TaleWorlds.Engine.Texture.CreateFromMemory(ms.ToArray());
                var texture = new TwoD.Texture(new EngineTexture(engineTexture));
                _sprite = new RuntimeTextureSprite(
                    "retinues_troops_icon",
                    IconWidth,
                    IconHeight,
                    texture
                );
                Log.Debug("Loaded custom troops map-bar icon.");
            }
            catch (Exception e)
            {
                Log.Exception(e, "Failed to load custom troops map-bar icon.");
                _loadFailed = true;
            }

            return _sprite;
        }

        /// <summary>
        /// Assigns the custom icon to the map-bar brush layer. Cheap and idempotent: safe to call
        /// every frame on the map. Re-applies automatically if a brush reload reverts the sprite.
        /// </summary>
        internal static void EnsureApplied()
        {
            if (_loadFailed)
                return;

            var sprite = GetSprite();
            if (sprite == null)
                return;

            var layer = UIResourceManager.BrushFactory?.GetBrush(BrushName)?.GetLayer(LayerName);
            if (layer == null)
                return; // Brushes not loaded yet (e.g. before first map screen); retry next frame.

            if (ReferenceEquals(layer.Sprite, sprite))
                return;

            layer.Sprite = sprite;
            // The placeholder mirrored a borrowed vanilla icon; our own art must not be flipped.
            layer.HorizontalFlip = false;
        }
#else
        /// <summary>No-op on BL12 (keeps the borrowed placeholder icon).</summary>
        internal static void EnsureApplied() { }
#endif
    }
}
