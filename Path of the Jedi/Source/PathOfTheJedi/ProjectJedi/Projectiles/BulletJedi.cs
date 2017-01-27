using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;


namespace PathOfTheJedi
{
    public class BulletJedi : Projectile
    {

        protected override void Impact(Thing hitThing)
        {
            Verse.Map map = base.Map;
            base.Impact(hitThing);
            if (hitThing == null)
            {
                SoundDefOf.BulletImpactGround.PlayOneShot(new TargetInfo(base.Position, map, false));
                MoteMaker.MakeStaticMote(this.ExactPosition, map, ThingDefOf.Mote_ShotHit_Dirt, 1f);
            }
            else
            {
                int num = this.def.projectile.damageAmountBase;
                ThingDef thingDef = this.equipmentDef;
                DamageDef damageDef = this.def.projectile.damageDef;
                Vector3 exactRotation = this.ExactRotation.eulerAngles;
                DamageInfo damageInfo = new DamageInfo(damageDef, num, exactRotation.y, this.launcher, null, thingDef);
                hitThing.TakeDamage(damageInfo);
            }
        }
    }
}