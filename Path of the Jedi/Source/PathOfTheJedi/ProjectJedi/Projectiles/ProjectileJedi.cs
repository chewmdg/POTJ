using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PathOfTheJedi
{
    //Vanilla Projectile
    public abstract class ProjectileJedi : ThingWithComps
    {
        private const float BasePawnInterceptChance = 0.4f;
        private const float PawnInterceptChanceFactor_LayingDown = 0.1f;
        private const float PawnInterceptChanceFactor_NonWildNonEnemy = 0.4f;
        private const float InterceptChanceOnRandomObjectPerFillPercent = 0.07f;
        private const float InterceptDist_Possible = 4f;
        private const float InterceptDist_Short = 7f;
        private const float InterceptDist_Normal = 10f;
        private const float InterceptChanceFactor_VeryShort = 0.5f;
        private const float InterceptChanceFactor_Short = 0.75f;


        public bool canFreeIntercept;
        protected Vector3 origin;
        protected Vector3 destination;
        protected Thing assignedTarget;
        private bool interceptWallsInt = true;
        private bool freeInterceptInt = true;
        protected ThingDef equipmentDef;
        protected Thing launcher;
        private Thing neverInterceptTargetInt;
        protected bool landed;
        protected int ticksToImpact;
        private Sustainer ambientSustainer;
        private static List<IntVec3> checkedCells;
        private readonly static List<Thing> cellThingsFiltered;


        //New variables
        private const float treeCollisionChance = 0.5f; //Tree collision chance is multiplied by this factor
        public float shotAngle;
        public float shotHeight = 0f;
        public float shotSpeed = -1f;


        protected IntVec3 DestinationCell
        {
            get
            {
                return new IntVec3(this.destination);
            }
        }

        public override Vector3 DrawPos
        {
            get
            {
                return this.ExactPosition;
            }
        }

        public virtual Vector3 ExactPosition
        {
            get
            {
                Vector3 startingTicksToImpact = (this.destination - this.origin) * (1f - (float)this.ticksToImpact / (float)this.StartingTicksToImpact);
                return (this.origin + startingTicksToImpact) + (Vector3.up * this.def.Altitude);
            }
        }

        public virtual Quaternion ExactRotation
        {
            get
            {
                return Quaternion.LookRotation(this.destination - this.origin);
            }
        }

        public bool FreeIntercept
        {
            get
            {
                if (this.def.projectile.alwaysFreeIntercept)
                {
                    return true;
                }
                if (this.def.projectile.flyOverhead)
                {
                    return false;
                }
                return this.freeInterceptInt;
            }
            set
            {
                if (!value && this.def.projectile.alwaysFreeIntercept)
                {
                    Log.Error("Tried to set FreeIntercept to false on projectile with alwaysFreeIntercept=true");
                    return;
                }
                if (value && this.def.projectile.flyOverhead)
                {
                    Log.Error("Tried to set FreeIntercept to true on a projectile with flyOverhead=true");
                    return;
                }
                this.freeInterceptInt = value;
            }
        }

        public bool InterceptWalls
        {
            get
            {
                if (this.def.projectile.alwaysFreeIntercept)
                {
                    return true;
                }
                if (this.def.projectile.flyOverhead)
                {
                    return false;
                }
                return this.interceptWallsInt;
            }
            set
            {
                if (!value && this.def.projectile.alwaysFreeIntercept)
                {
                    Log.Error("Tried to set interceptWalls to false on projectile with alwaysFreeIntercept=true");
                    return;
                }
                if (value && this.def.projectile.flyOverhead)
                {
                    Log.Error("Tried to set interceptWalls to true on a projectile with flyOverhead=true");
                    return;
                }
                this.interceptWallsInt = value;
                if (!interceptWallsInt && this is Projectile_Explosive)
                {
                    Log.Message("Non interceptWallsInt explosive.");
                }
            }
        }

        protected int StartingTicksToImpact
        {
            get
            {
                Vector3 vector3 = this.origin - this.destination;
                int num = Mathf.RoundToInt(vector3.magnitude / (this.def.projectile.speed / 100f));
                if (num < 1)
                {
                    num = 1;
                }
                return num;
            }
        }

        public Thing ThingToNeverIntercept
        {
            get
            {
                return this.neverInterceptTargetInt;
            }
            set
            {
                if (value.def.Fillage == FillCategory.Full)
                {
                    return;
                }
                this.neverInterceptTargetInt = value;
            }
        }

        static ProjectileJedi()
        {
            ProjectileJedi.checkedCells = new List<IntVec3>();
            ProjectileJedi.cellThingsFiltered = new List<Thing>();
        }

        protected ProjectileJedi()
        {
        }

        private bool CheckForFreeIntercept(IntVec3 c)
        {
            float single = (c.ToVector3Shifted() - this.origin).MagnitudeHorizontalSquared();
            if (single < 16f)
            {
                return false;
            }
            List<Thing> things = base.Map.thingGrid.ThingsListAt(c);
            for (int i = 0; i < things.Count; i++)
            {
                Thing item = things[i];
                if (item != this.ThingToNeverIntercept)
                {
                    if (item != this.launcher)
                    {
                        if (item.def.Fillage == FillCategory.Full && this.InterceptWalls)
                        {
                            this.Impact(item);
                            return true;
                        }
                        if (this.FreeIntercept)
                        {
                            float single1 = 0f;
                            Pawn pawn = item as Pawn;
                            if (pawn != null)
                            {
                                single1 = 0.4f;
                                if (pawn.GetPosture() != PawnPosture.Standing)
                                {
                                    single1 = single1 * 0.1f;
                                }
                                if (this.launcher != null && pawn.Faction != null && this.launcher.Faction != null && !pawn.Faction.HostileTo(this.launcher.Faction))
                                {
                                    single1 = single1 * 0.4f;
                                }
                                single1 = single1 * Mathf.Clamp(pawn.BodySize, 0.1f, 2f);
                            }
                            else if (item.def.fillPercent > 0.2f)
                            {
                                single1 = item.def.fillPercent * 0.07f;
                            }
                            if (single1 > 1E-05f)
                            {
                                if (single < 49f)
                                {
                                    single1 = single1 * 0.5f;
                                }
                                else if (single < 100f)
                                {
                                    single1 = single1 * 0.75f;
                                }
                                if (DebugViewSettings.drawShooting)
                                {
                                    MoteMaker.ThrowText(this.ExactPosition, base.Map, single1.ToStringPercent(), -1f);
                                }
                                if (Rand.Value < single1)
                                {
                                    this.Impact(item);
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private bool CheckForFreeInterceptBetween(Vector3 lastExactPos, Vector3 newExactPos)
        {
            IntVec3 intVec3 = lastExactPos.ToIntVec3();
            IntVec3 intVec31 = newExactPos.ToIntVec3();
            if (intVec31 == intVec3)
            {
                return false;
            }
            if (!intVec3.InBounds(base.Map) || !intVec31.InBounds(base.Map))
            {
                return false;
            }
            if (intVec31.AdjacentToCardinal(intVec3))
            {
                bool flag = this.CheckForFreeIntercept(intVec31);
                if (DebugViewSettings.drawInterceptChecks)
                {
                    if (!flag)
                    {
                        MoteMaker.ThrowText(intVec31.ToVector3Shifted(), base.Map, "o", -1f);
                    }
                    else
                    {
                        MoteMaker.ThrowText(intVec31.ToVector3Shifted(), base.Map, "x", -1f);
                    }
                }
                return flag;
            }
            if (this.origin.ToIntVec3().DistanceToSquared(intVec31) <= 16f)
            {
                return false;
            }
            Vector3 vector3 = lastExactPos;
            Vector3 vector31 = newExactPos - lastExactPos;
            Vector3 vector32 = vector31.normalized * 0.2f;
            int num = (int)(vector31.MagnitudeHorizontal() / 0.2f);
            ProjectileJedi.checkedCells.Clear();
            int num1 = 0;
            while (true)
            {
                vector3 = vector3 + vector32;
                IntVec3 intVec32 = vector3.ToIntVec3();
                if (!ProjectileJedi.checkedCells.Contains(intVec32))
                {
                    if (this.CheckForFreeIntercept(intVec32))
                    {
                        if (DebugViewSettings.drawInterceptChecks)
                        {
                            MoteMaker.ThrowText(vector3, base.Map, "x", -1f);
                        }
                        return true;
                    }
                    ProjectileJedi.checkedCells.Add(intVec32);
                }
                if (DebugViewSettings.drawInterceptChecks)
                {
                    MoteMaker.ThrowText(vector3, base.Map, "o", -1f);
                }
                num1++;
                if (num1 > num)
                {
                    return false;
                }
                if (intVec32 == intVec31)
                {
                    break;
                }
            }
            return false;
        }

        public override void Draw()
        {
            Graphics.DrawMesh(MeshPool.plane10, this.DrawPos, this.ExactRotation, this.def.DrawMatSingle, 0);
            base.Comps_PostDraw();
        }

        //Add new variables
        public override void ExposeData()
        {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving && launcher != null && launcher.Destroyed)
            {
                launcher = null;
            }
            Vector3 vector3 = new Vector3();
            Scribe_Values.LookValue<Vector3>(ref this.origin, "origin", vector3, false);
            Vector3 vector31 = new Vector3();
            Scribe_Values.LookValue<Vector3>(ref this.destination, "destination", vector31, false);
            Scribe_Values.LookValue<int>(ref this.ticksToImpact, "ticksToImpact", 0, false);
            Scribe_References.LookReference<Thing>(ref this.assignedTarget, "assignedTarget", false);
            Scribe_References.LookReference<Thing>(ref this.launcher, "launcher", false);
            Scribe_Defs.LookDef<ThingDef>(ref this.equipmentDef, "equipmentDef");
            Scribe_Values.LookValue<bool>(ref this.interceptWallsInt, "interceptWalls", true, false);
            Scribe_Values.LookValue<bool>(ref this.freeInterceptInt, "interceptRandomTargets", true, false);
            Scribe_Values.LookValue<bool>(ref this.landed, "landed", false, false);
            Scribe_References.LookReference<Thing>(ref this.neverInterceptTargetInt, "neverInterceptTarget", false);

            //Here is where to add new variables
            Scribe_Values.LookValue(ref canFreeIntercept, "canFreeIntercept", false, false);
            Scribe_Values.LookValue(ref shotAngle, "shotAngle", 0f, true);
            Scribe_Values.LookValue(ref shotAngle, "shotHeight", 0f, true);
            Scribe_Values.LookValue(ref shotSpeed, "shotSpeed", 0f, true);
        }

        public void ForceInstantImpact()
        {
            if (!this.DestinationCell.InBounds(base.Map))
            {
                this.Destroy(DestroyMode.Vanish);
                return;
            }
            this.ticksToImpact = 0;
            base.Position = this.DestinationCell;
            this.ImpactSomething();
        }

        protected virtual void Impact(Thing hitThing)
        {
            this.Destroy(DestroyMode.Vanish);
        }

        private void ImpactSomething()
        {
            if (this.def.projectile.flyOverhead)
            {
                RoofDef roofDef = base.Map.roofGrid.RoofAt(base.Position);
                if (roofDef != null)
                {
                    if (roofDef.isThickRoof)
                    {
                        this.def.projectile.soundHitThickRoof.PlayOneShot(new TargetInfo(base.Position, base.Map, false));
                        this.Destroy(DestroyMode.Vanish);
                        return;
                    }
                    if (base.Position.GetEdifice(base.Map) == null || base.Position.GetEdifice(base.Map).def.Fillage != FillCategory.Full)
                    {
                        RoofCollapserImmediate.DropRoofInCells(base.Position, base.Map);
                    }
                }
            }
            if (this.assignedTarget != null)
            {
                Pawn pawn = this.assignedTarget as Pawn;
                if (pawn != null && pawn.GetPosture() != PawnPosture.Standing && (this.origin - this.destination).MagnitudeHorizontalSquared() >= 20.25f && Rand.Value > 0.2f)
                {
                    this.Impact(null);
                    return;
                }
                this.Impact(this.assignedTarget);
                return;
            }
            ProjectileJedi.cellThingsFiltered.Clear();
            List<Thing> thingList = base.Position.GetThingList(base.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                Pawn item = thingList[i] as Pawn;
                if (item != null)
                {
                    ProjectileJedi.cellThingsFiltered.Add(item);
                }
            }
            if (ProjectileJedi.cellThingsFiltered.Count > 0)
            {
                this.Impact(ProjectileJedi.cellThingsFiltered.RandomElement<Thing>());
                return;
            }
            ProjectileJedi.cellThingsFiltered.Clear();
            for (int j = 0; j < thingList.Count; j++)
            {
                Thing thing = thingList[j];
                if (thing.def.fillPercent > 0f || thing.def.passability != Traversability.Standable)
                {
                    ProjectileJedi.cellThingsFiltered.Add(thing);
                }
            }
            if (ProjectileJedi.cellThingsFiltered.Count <= 0)
            {
                this.Impact(null);
                return;
            }
            this.Impact(ProjectileJedi.cellThingsFiltered.RandomElement<Thing>());
        }

        //Added new method, takes Vector3 destination as argument
        public void LaunchVector3(Thing launcher, Vector3 origin, LocalTargetInfo targ, Vector3 target, Thing equipment = null)
        {
            destination = target;
            Launch(launcher, origin, targ, equipment);
        }

        public void Launch(Thing launcher, LocalTargetInfo targ, Thing equipment = null)
        {
            IntVec3 position = base.Position;
            this.Launch(launcher, position.ToVector3Shifted(), targ, equipment);
        }

        public void Launch(Thing launcher, Vector3 origin, LocalTargetInfo targ, Thing equipment = null)
        {
            this.launcher = launcher;
            this.origin = origin;
            if (equipment == null)
            {
                this.equipmentDef = null;
            }
            else
            {
                this.equipmentDef = equipment.def;
            }
            if (targ.Thing != null)
            {
                this.assignedTarget = targ.Thing;
            }
            IntVec3 cell = targ.Cell;
            this.destination = cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f));
            this.ticksToImpact = this.StartingTicksToImpact;
            if (!this.def.projectile.soundAmbient.NullOrUndefined())
            {
                SoundInfo soundInfo = SoundInfo.InMap(this, MaintenanceType.PerTick);
                this.ambientSustainer = this.def.projectile.soundAmbient.TrySpawnSustainer(soundInfo);
            }
        }


        public override void Tick()
        {
            base.Tick();
            if (landed)
            {
                return;
            }
            Vector3 exactPosition = ExactPosition;
            ticksToImpact--;
            if (!ExactPosition.InBounds(base.Map))
            {
                ticksToImpact++;
                Position = ExactPosition.ToIntVec3();
                Destroy(DestroyMode.Vanish);
                return;
            }
            Vector3 exactPosition2 = ExactPosition;
            if (!def.projectile.flyOverhead && canFreeIntercept &&
                CheckForFreeInterceptBetween(exactPosition, exactPosition2))
            {
                return;
            }
            Position = ExactPosition.ToIntVec3();
            if (ticksToImpact == 60f && Find.TickManager.CurTimeSpeed == TimeSpeed.Normal &&
                def.projectile.soundImpactAnticipate != null)
            {
                def.projectile.soundImpactAnticipate.PlayOneShot(this);
            }
            if (ticksToImpact <= 0)
            {
                if (DestinationCell.InBounds(base.Map))
                {
                    Position = DestinationCell;
                }
                ImpactSomething();
                return;
            }
            if (ambientSustainer != null)
            {
                ambientSustainer.Maintain();
            }

        }
    }
}
