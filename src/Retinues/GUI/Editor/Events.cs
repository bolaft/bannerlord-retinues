using System;
using Retinues.Utils;

namespace Retinues.GUI.Editor
{
    public class EditorEvent(string name, bool enabled = true)
    {
        public string Name { get; } = name;
        private readonly bool _enabled = enabled;
        private Action _handlers;

        public void Fire()
        {
            Log.Debug($"Event fired: {Name}");
            if (_enabled)
            {
                Log.Debug($"Event is enabled.");
                _handlers?.Invoke();
            }
            else
            {
                Log.Debug($"Event is disabled; handlers will not be invoked.");
            }
        }

        public void Register(Action action) => _handlers += action;

        public void Unregister(Action action) => _handlers -= action;

        public void Clear() => _handlers = null;

        public void RegisterProperties(BaseComponent vm, params string[] names)
        {
            var wr = new WeakReference<BaseComponent>(vm);
            void h()
            {
                if (!wr.TryGetTarget(out var target))
                {
                    Unregister(h);
                    return;
                }
                target.Raise(names);
            }
            Register(h);
        }
    }

    public static class EventManager
    {
        public static readonly EditorEvent FactionChange = new(nameof(FactionChange));
        public static readonly EditorEvent ScreenChange = new(nameof(ScreenChange));
        public static readonly EditorEvent TroopChange = new(nameof(TroopChange));
        public static readonly EditorEvent TroopListChange = new(nameof(TroopListChange));
        public static readonly EditorEvent SkillChange = new(nameof(SkillChange));
        public static readonly EditorEvent NameChange = new(nameof(NameChange));
        public static readonly EditorEvent GenderChange = new(nameof(GenderChange));
        public static readonly EditorEvent TierChange = new(nameof(TierChange));
        public static readonly EditorEvent ConversionChange = new(nameof(ConversionChange));
        public static readonly EditorEvent EquipmentChange = new(nameof(EquipmentChange));
        public static readonly EditorEvent EquipmentItemChange = new(nameof(EquipmentItemChange));
        public static readonly EditorEvent EquipmentSlotChange = new(nameof(EquipmentSlotChange));
    }
}
