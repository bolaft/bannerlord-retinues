namespace OldRetinues.GUI.Editor
{
    /// <summary>
    /// ViewModel for a single filterable list element.
    /// </summary>
    public abstract class BaseListElementVM : BaseButtonVM
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //
        //                       Public API                       //
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ //

        /// <summary>
        /// Apply the given filter to update element visibility.
        /// </summary>
        public void ApplyFilter(string filter)
        {
            if (string.IsNullOrWhiteSpace(filter))
                IsVisible = true;
            else
                IsVisible = FilterMatch(filter);
        }

        /// <summary>
        /// Allow subclasses to opt out of global event registration.
        /// </summary>
        protected BaseListElementVM(bool autoRegister = true)
            : base(autoRegister) { }

        /// <summary>
        /// Determine whether this element matches the provided filter.
        /// </summary>
        public abstract bool FilterMatch(string filter);
    }
}
