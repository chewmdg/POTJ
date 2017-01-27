using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;


namespace PathOfTheJedi
{
    public class Verb_LaunchProjectile : Verse.Verb
    {
//Mod to add VerbpropsCR?
        public VerbPropertiesJedi verbPropsCR
        {
            get
            {
                return this.verbProps as VerbPropertiesJedi;
            }
        }

        //Vanilla
        public Verb_LaunchProjectile()
        {
        }

        public override float HighlightFieldRadiusAroundTarget()
        {
            return this.verbProps.projectileDef.projectile.explosionRadius;
        }

        protected override bool TryCastShot()
        {
            ShootLine shootLine;
            float single;
            if (this.currentTarget.HasThing && this.currentTarget.Thing.Map != this.caster.Map)
            {
                return false;
            }
            bool flag = base.TryFindShootLineFromTo(this.caster.Position, this.currentTarget, out shootLine);
            if (this.verbProps.stopBurstWithoutLos && !flag)
            {
                return false;
            }
            Vector3 drawPos = this.caster.DrawPos;
            Projectile thing = (Projectile)GenSpawn.Spawn(this.verbProps.projectileDef, shootLine.Source, this.caster.Map);
            thing.FreeIntercept = (!this.canFreeInterceptNow ? false : !thing.def.projectile.flyOverhead);
            if (this.verbProps.forcedMissRadius > 0.5f)
            {
                IntVec3 cell = this.currentTarget.Cell - this.caster.Position;
                float lengthHorizontalSquared = cell.LengthHorizontalSquared;
                if (lengthHorizontalSquared < 9f)
                {
                    single = 0f;
                }
                else if (lengthHorizontalSquared >= 25f)
                {
                    single = (lengthHorizontalSquared >= 49f ? this.verbProps.forcedMissRadius * 1f : this.verbProps.forcedMissRadius * 0.8f);
                }
                else
                {
                    single = this.verbProps.forcedMissRadius * 0.5f;
                }
                if (single > 0.5f)
                {
                    int num = GenRadial.NumCellsInRadius(this.verbProps.forcedMissRadius);
                    int num1 = Rand.Range(0, num);
                    if (num1 > 0)
                    {
                        if (DebugViewSettings.drawShooting)
                        {
                            MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToForRad", -1f);
                        }
                        IntVec3 intVec3 = this.currentTarget.Cell + GenRadial.RadialPattern[num1];
                        if (this.currentTarget.HasThing)
                        {
                            thing.ThingToNeverIntercept = this.currentTarget.Thing;
                        }
                        if (!thing.def.projectile.flyOverhead)
                        {
                            thing.InterceptWalls = true;
                        }
                        thing.Launch(this.caster, drawPos, intVec3, this.ownerEquipment);
                        return true;
                    }
                }
            }
            ShotReport shotReport = ShotReport.HitReportFor(this.caster, this, this.currentTarget);
            if (Rand.Value > shotReport.ChanceToNotGoWild_IgnoringPosture)
            {
                if (DebugViewSettings.drawShooting)
                {
                    MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToWild", -1f);
                }
                shootLine.ChangeDestToMissWild();
                if (this.currentTarget.HasThing)
                {
                    thing.ThingToNeverIntercept = this.currentTarget.Thing;
                }
                if (!thing.def.projectile.flyOverhead)
                {
                    thing.InterceptWalls = true;
                }
                thing.Launch(this.caster, drawPos, shootLine.Dest, this.ownerEquipment);
                return true;
            }
            if (Rand.Value > shotReport.ChanceToNotHitCover)
            {
                if (DebugViewSettings.drawShooting)
                {
                    MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToCover", -1f);
                }
                if (this.currentTarget.Thing != null && this.currentTarget.Thing.def.category == ThingCategory.Pawn)
                {
                    Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
                    if (!thing.def.projectile.flyOverhead)
                    {
                        thing.InterceptWalls = true;
                    }
                    thing.Launch(this.caster, drawPos, randomCoverToMissInto, this.ownerEquipment);
                    return true;
                }
            }
            if (DebugViewSettings.drawShooting)
            {
                MoteMaker.ThrowText(this.caster.DrawPos, this.caster.Map, "ToHit", -1f);
            }
            if (!thing.def.projectile.flyOverhead)
            {
                thing.InterceptWalls = (!this.currentTarget.HasThing ? true : this.currentTarget.Thing.def.Fillage == FillCategory.Full);
            }
            if (this.currentTarget.Thing == null)
            {
                thing.Launch(this.caster, drawPos, shootLine.Dest, this.ownerEquipment);
            }
            else
            {
                thing.Launch(this.caster, drawPos, this.currentTarget, this.ownerEquipment);
            }
            return true;
        }
    }
}