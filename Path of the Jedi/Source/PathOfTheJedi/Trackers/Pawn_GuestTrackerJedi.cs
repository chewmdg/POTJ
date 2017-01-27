using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace PathOfTheJedi
{
    public class Pawn_GuestTrackerJedi : IExposable
    {
        private const int DefaultWaitInsteadOfEscapingTicks = 12500;

        private const int CheckInitiatePrisonBreakIntervalTicks = 2500;

        private Pawn pawn;

        private bool getsFoodInt = true;

        public PrisonerInteractionMode interactionMode;

        private Faction hostFactionInt;

        public bool isPrisonerInt;

        public bool released;

        private int ticksWhenAllowedToEscapeAgain;

        public IntVec3 spotToWaitInsteadOfEscaping = IntVec3.Invalid;

        public int MinInteractionInterval = 7500;

        public bool GetsFood
        {
            get
            {
                if (this.HostFaction != null)
                {
                    return this.getsFoodInt;
                }
                Log.Error("GetsFood without host faction.");
                return true;
            }
            set
            {
                this.getsFoodInt = value;
            }
        }

        public Faction HostFaction
        {
            get
            {
                return this.hostFactionInt;
            }
        }

        public bool IsPrisoner
        {
            get
            {
                return this.isPrisonerInt;
            }
        }

        public bool PrisonerIsSecure
        {
            get
            {
                if (this.released)
                {
                    return false;
                }
                if (this.pawn.HostFaction == null)
                {
                    return false;
                }
                if (this.pawn.InMentalState)
                {
                    return false;
                }
                if (this.pawn.Spawned)
                {
                    if (this.pawn.jobs.curJob != null && this.pawn.jobs.curJob.exitMapOnArrival)
                    {
                        return false;
                    }
                    if (PrisonBreakUtility.IsPrisonBreaking(this.pawn))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        public bool ScheduledForInteraction
        {
            get
            {
                return this.pawn.mindState.lastAssignedInteractTime < Find.TickManager.TicksGame - this.MinInteractionInterval;
            }
        }

        public bool ShouldBeBroughtFood
        {
            get
            {
                bool flag;
                if (!this.GetsFood || this.interactionMode == PrisonerInteractionMode.Execution)
                {
                    flag = false;
                }
                else
                {
                    flag = (this.interactionMode != PrisonerInteractionMode.Release ? true : this.pawn.Downed);
                }
                return flag;
            }
        }

        public bool ShouldWaitInsteadOfEscaping
        {
            get
            {
                if (!this.IsPrisoner)
                {
                    return false;
                }
                Map mapHeld = this.pawn.MapHeld;
                if (mapHeld == null)
                {
                    return false;
                }
                if (mapHeld.mapPawns.FreeColonistsSpawnedCount == 0)
                {
                    return false;
                }
                return Find.TickManager.TicksGame < this.ticksWhenAllowedToEscapeAgain;
            }
        }

        public Pawn_GuestTrackerJedi()
        {
        }

        public Pawn_GuestTrackerJedi(Pawn pawn)
        {
            this.pawn = pawn;
        }

        public void ExposeData()
        {
            Scribe_References.LookReference<Faction>(ref this.hostFactionInt, "hostFaction", false);
            Scribe_Values.LookValue<bool>(ref this.isPrisonerInt, "prisoner", false, false);
            Scribe_Values.LookValue<bool>(ref this.getsFoodInt, "getsFood", false, false);
            Scribe_Values.LookValue<PrisonerInteractionMode>(ref this.interactionMode, "interactionMode", PrisonerInteractionMode.NoInteraction, false);
            Scribe_Values.LookValue<bool>(ref this.released, "released", false, false);
            Scribe_Values.LookValue<int>(ref this.ticksWhenAllowedToEscapeAgain, "ticksWhenAllowedToEscapeAgain", 0, false);
            IntVec3 intVec3 = new IntVec3();
            Scribe_Values.LookValue<IntVec3>(ref this.spotToWaitInsteadOfEscaping, "spotToWaitInsteadOfEscaping", intVec3, false);
        }

        public void GuestTrackerTick()
        {
            if (this.pawn.IsHashIntervalTick(2500))
            {
                float single = PrisonBreakUtility.InitiatePrisonBreakMtbDays(this.pawn);
                if (single >= 0f && Rand.MTBEventOccurs(single, 60000f, 2500f))
                {
                    PrisonBreakUtility.StartPrisonBreak(this.pawn);
                }
            }
        }

        internal void Notify_PawnUndowned()
        {
            float single;
            if (this.pawn.RaceProps.Humanlike && this.HostFaction == Faction.OfPlayer && (this.pawn.Faction == null || this.pawn.Faction.def.rescueesCanJoin) && !this.IsPrisoner)
            {
                single = (!this.pawn.SafeTemperatureRange().Includes(this.pawn.Map.mapTemperature.OutdoorTemp) || this.pawn.Map.mapConditionManager.ConditionIsActive(MapConditionDefOf.ToxicFallout) ? 1f : 0.5f);
                Rand.PushSeed();
                Rand.Seed = this.pawn.HashOffset();
                float value = Rand.Value;
                Rand.PopSeed();
                if (value < single)
                {
                    this.pawn.SetFaction(Faction.OfPlayer, null);
                    Messages.Message("MessageRescueeJoined".Translate(new object[] { this.pawn.LabelShort }).AdjustedFor(this.pawn), this.pawn, MessageSound.Benefit);
                }
            }
        }

        public void SetGuestStatus(Faction newHost, bool prisoner = false)
        {
            bool flag;
            if (newHost != null)
            {
                this.released = false;
            }
            if (newHost == this.HostFaction && prisoner == this.IsPrisoner)
            {
                return;
            }
            if (!prisoner && this.pawn.Faction.HostileTo(newHost))
            {
                Log.Error(string.Concat(new object[] { "Tried to make ", this.pawn, " a guest of ", newHost, " but their faction ", this.pawn.Faction, " is hostile to ", newHost }));
                return;
            }
            if (newHost == this.pawn.Faction && !prisoner)
            {
                Log.Error(string.Concat(new object[] { "Tried to make ", this.pawn, " a guest of their own faction ", this.pawn.Faction }));
                return;
            }
            if (!prisoner)
            {
                flag = false;
            }
            else
            {
                flag = (!this.IsPrisoner ? true : this.HostFaction != newHost);
            }
            bool flag1 = flag;
            this.isPrisonerInt = prisoner;
            this.hostFactionInt = newHost;
            this.pawn.ClearMind(false);
            this.pawn.ClearReservations();
            if (flag1)
            {
                this.pawn.DropAndForbidEverything(false);
                Lord lord = this.pawn.GetLord();
                if (lord != null)
                {
                    lord.Notify_PawnLost(this.pawn, PawnLostCondition.MadePrisoner);
                }
                if (this.pawn.Drafted)
                {
                    this.pawn.drafter.Drafted = false;
                }
            }
            PawnComponentsUtility.AddAndRemoveDynamicComponents(this.pawn, false);
            this.pawn.health.surgeryBills.Clear();
            if (this.pawn.ownership != null)
            {
                this.pawn.ownership.Notify_ChangedGuestStatus();
            }
            ReachabilityUtility.ClearCache();
            if (this.pawn.Spawned)
            {
                this.pawn.Map.mapPawns.UpdateRegistryForPawn(this.pawn);
                this.pawn.Map.attackTargetsCache.UpdateTarget(this.pawn);
            }
            AddictionUtility.CheckDrugAddictionTeachOpportunity(this.pawn);
            if (prisoner && this.pawn.playerSettings != null)
            {
                this.pawn.playerSettings.Notify_MadePrisoner();
            }
        }

        public void WaitInsteadOfEscapingFor(int ticks)
        {
            if (!this.IsPrisoner)
            {
                return;
            }
            this.ticksWhenAllowedToEscapeAgain = Find.TickManager.TicksGame + ticks;
            this.spotToWaitInsteadOfEscaping = IntVec3.Invalid;
        }

        public void WaitInsteadOfEscapingForDefaultTicks()
        {
            this.WaitInsteadOfEscapingFor(12500);
        }
    }
}