using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.InputSystem;

namespace Retinues.GUI.Editor
{
    /// <summary>
    /// Base class for all VMs.
    /// </summary>
    public abstract class BaseVM : ViewModel
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Visibility                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        private bool _isVisible = false;

        [DataSourceProperty]
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                if (_isVisible == value)
                    return;
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
                if (_isVisible)
                    FlushIfPending();
            }
        }

        public virtual void Show() => IsVisible = true;

        public virtual void Hide() => IsVisible = false;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                    Event Management                    //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected abstract Dictionary<UIEvent, string[]> EventMap { get; }

        private readonly HashSet<string> _pendingProps = [];
        private bool _queuedWhileHidden;
        private bool _inPulse;

        protected BaseVM() => EventManager.Register(this);

        ~BaseVM() => EventManager.Unregister(this);

        // Called by EventManager
        internal void __BeginPulse() => _inPulse = true;

        internal void __EndPulse()
        {
            _inPulse = false;
            if (IsVisible)
                FlushPending();
            else
                _queuedWhileHidden = true;
        }

        internal void __OnGlobalPulse(UIEvent e)
        {
            if (EventMap.TryGetValue(e, out var props) && props != null)
            {
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];
                    if (!string.IsNullOrEmpty(p))
                        _pendingProps.Add(p);
                }
            }

            // If hidden and not opted-in, don't execute heavy hooks—just queue.
            if (!IsVisible)
            {
                _queuedWhileHidden = true;
                return;
            }

            // Invoke specific hooks only when visible or explicitly opted-in
            if (e == UIEvent.Faction)
                OnFactionChange();
            else if (e == UIEvent.Troop)
                OnTroopChange();
            else if (e == UIEvent.Equipment)
                OnEquipmentChange();
            else if (e == UIEvent.Slot)
                OnSlotChange();

            if (_inPulse || !IsVisible)
            {
                return;
            }

            FlushPending();
        }

        private void FlushIfPending()
        {
            if (_queuedWhileHidden && !_inPulse && IsVisible)
                FlushPending();
        }

        protected void FlushPending()
        {
            if (_pendingProps.Count == 0)
            {
                _queuedWhileHidden = false;
                return;
            }

            foreach (var name in _pendingProps)
                OnPropertyChanged(name);

            _pendingProps.Clear();
            _queuedWhileHidden = false;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                      Event Hooks                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected virtual void OnFactionChange() { }

        protected virtual void OnTroopChange() { }

        protected virtual void OnEquipmentChange() { }

        protected virtual void OnSlotChange() { }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                         Input                          //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        protected static int BatchInput(bool capped = true)
        {
            if (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl))
                return capped ? 5 : 1000;
            if (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift))
                return 5;
            return 1;
        }
    }
}
