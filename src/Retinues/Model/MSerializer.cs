namespace Retinues.Model
{
    /// <summary>
    /// Base serializer for persistent attributes.
    /// Use MSerializer<T> in attributes.
    /// </summary>
    public abstract class MSerializer { }

    /// <summary>
    /// Serializer for a specific attribute type.
    /// </summary>
    public abstract class MSerializer<T> : MSerializer
    {
        public abstract string Serialize(MAttribute<T> attribute);

        public abstract T Deserialize(string serialized);
    }
}
