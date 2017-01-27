using Verse;

namespace PathOfTheJedi
{
    public class VerbPropertiesJedi : VerbProperties
    {
        public RecoilPattern recoilPattern = RecoilPattern.None;
        public float recoilAmount = 0;
        public float indirectFirePenalty = 0;
        public float meleeArmorPenetration = 0;
        public bool ejectsCasings = true;
    }
}