using Verse;


namespace PathOfTheJedi
{
    public class ProjectilePropertiesJedi : ProjectileProperties
    {
        public float armorPenetration = 0;
        public int pelletCount = 1;
        public float spreadMult = 1;
        public bool damageAdjacentTiles = false;
        public bool dropsCasings = false;
        public string casingMoteDefname = "Mote_EmptyCasing";
    }
}