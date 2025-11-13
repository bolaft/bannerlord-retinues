using System.Collections.Generic;
using System.Xml.Serialization;

namespace Retinues.Safety.Legacy
{
    [XmlRoot("Troops")]
    public class LegacyTroopsContainer
    {
        [XmlElement("TroopSaveData")]
        public List<LegacyTroopSaveData> Troops { get; set; } = new List<LegacyTroopSaveData>();

        public LegacyTroopsContainer() { }
    }
}
