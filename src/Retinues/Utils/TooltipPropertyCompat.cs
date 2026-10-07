using TaleWorlds.Core.ViewModelCollection.Information;

namespace Retinues.Utils
{
    internal static class TooltipPropertyCompat
    {
        // 1.5.4 replaced the five-argument text constructor with a six-argument overload.
        // Its new optional argument still changes the binary signature. The parameterless
        // constructor and these setters exist on every supported version and produce the
        // same text row without binding a binary to either overload.
        internal static TooltipProperty Create(string definition, string value, int textHeight,
            bool onlyShowWhenExtended, TooltipProperty.TooltipPropertyFlags modifier) => new TooltipProperty
            {
                DefinitionLabel = definition,
                ValueLabel = value,
                TextHeight = textHeight,
                OnlyShowWhenExtended = onlyShowWhenExtended,
                PropertyModifier = (int)modifier,
            };
    }
}
