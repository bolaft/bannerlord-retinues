using TaleWorlds.SaveSystem;

namespace Retinues.Compatibility.Legacy.Save
{
    /// <summary>
    /// Legacy body customization schema.
    /// </summary>
    public sealed class TroopBodySaveData
    {
        [SaveableField(1)]
        public float AgeMin;

        [SaveableField(2)]
        public float AgeMax;

        [SaveableField(3)]
        public float WeightMin;

        [SaveableField(4)]
        public float WeightMax;

        [SaveableField(5)]
        public float BuildMin;

        [SaveableField(6)]
        public float BuildMax;

        [SaveableField(7)]
        public float HeightMin;

        [SaveableField(8)]
        public float HeightMax;

        public TroopBodySaveData() { }
    }
}
