using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Verse
{
    public struct ShotReport
    {
        public const float LayingDownHitChanceFactorMinDistance = 4.5f;
        public const float HitChanceFactorIfLayingDown = 0.2f;
        private const float NonPawnShooterHitFactorPerDistance = 0.96f;
        private const float ExecutionMaxDistance = 3.9f;
        private const float ExecutionFactor = 7.5f;
        private TargetInfo target;
        private float distance;
        private List<CoverInfo> covers;
        private float coversOverallBlockChance;
        private float factorFromShooterAndDist;
        private float factorFromEquipment;
        private float factorFromTargetSize;
        private float factorFromWeather;
        private float forcedMissRadius;

        public float ChanceToNotGoWild_IgnoringPosture
        {
            get
            {
                float factorFromExecution = this.factorFromShooterAndDist * this.factorFromEquipment * this.factorFromWeather * this.factorFromTargetSize * this.FactorFromExecution;
                return factorFromExecution;
            }
        }

        public float ChanceToNotHitCover
        {
            get
            {
                return 1f - this.coversOverallBlockChance;
            }
        }

        private float FactorFromExecution
        {
            get
            {
                if (this.target.HasThing)
                {
                    Pawn thing = this.target.Thing as Pawn;
                    if (thing != null && this.distance <= 3.9f && thing.GetPosture() != PawnPosture.Standing)
                    {
                        return 7.5f;
                    }
                }
                return 1f;
            }
        }

        private float FactorFromPosture
        {
            get
            {
                if (this.target.HasThing)
                {
                    Pawn thing = this.target.Thing as Pawn;
                    if (thing != null && this.distance >= 4.5f && thing.GetPosture() != PawnPosture.Standing)
                    {
                        return 0.2f;
                    }
                }
                return 1f;
            }
        }

        public float TotalEstimatedHitChance
        {
            get
            {
                float chanceToNotGoWildIgnoringPosture = this.ChanceToNotGoWild_IgnoringPosture * this.FactorFromPosture * this.ChanceToNotHitCover;
                return Mathf.Clamp01(chanceToNotGoWildIgnoringPosture);
            }
        }

        public Thing GetRandomCoverToMissInto()
        {
            CoverInfo coverInfo = this.covers.RandomElementByWeight<CoverInfo>((CoverInfo c) => c.BlockChance);
            return coverInfo.Thing;
        }

        public string GetTextReadout()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (this.forcedMissRadius <= 0.5f)
            {
                stringBuilder.AppendLine(string.Concat(" ", this.TotalEstimatedHitChance.ToStringPercent()));
                stringBuilder.AppendLine(string.Concat("   ", "ShootReportShooterAbility".Translate(), "  ", this.factorFromShooterAndDist.ToStringPercent()));
                if (this.factorFromEquipment < 0.99f)
                {
                    stringBuilder.AppendLine(string.Concat("   ", "ShootReportWeapon".Translate(), "        ", this.factorFromEquipment.ToStringPercent()));
                }
                if (this.target.HasThing && this.factorFromTargetSize != 1f)
                {
                    stringBuilder.AppendLine(string.Concat("   ", "TargetSize".Translate(), "       ", this.factorFromTargetSize.ToStringPercent()));
                }
                if (this.factorFromWeather < 0.99f)
                {
                    stringBuilder.AppendLine(string.Concat("   ", "Weather".Translate(), "         ", this.factorFromWeather.ToStringPercent()));
                }
                if (this.FactorFromPosture < 0.9999f)
                {
                    stringBuilder.AppendLine(string.Concat("   ", "TargetProne".Translate(), "  ", this.FactorFromPosture.ToStringPercent()));
                }
                if (this.FactorFromExecution != 1f)
                {
                    stringBuilder.AppendLine(string.Concat("   ", "Execution".Translate(), "   ", this.FactorFromExecution.ToStringPercent()));
                }
                if (this.ChanceToNotHitCover >= 1f)
                {
                    stringBuilder.AppendLine(string.Concat("   (", "NoCoverLower".Translate(), ")"));
                }
                else
                {
                    stringBuilder.AppendLine(string.Concat("   ", "ShootingCover".Translate(), "        ", this.ChanceToNotHitCover.ToStringPercent()));
                    for (int i = 0; i < this.covers.Count; i++)
                    {
                        CoverInfo item = this.covers[i];
                        stringBuilder.AppendLine(string.Concat("     ", "CoverThingBlocksPercentOfShots".Translate(new object[] { item.Thing.LabelCap, item.BlockChance.ToStringPercent() })));
                    }
                }
            }
            else
            {
                stringBuilder.AppendLine();
                stringBuilder.AppendLine(string.Concat("WeaponMissRadius".Translate(), "   ", this.forcedMissRadius.ToString("F1")));
            }
            return stringBuilder.ToString();
        }

        public static ShotReport HitReportFor(Thing caster, Verb verb, LocalTargetInfo target)
        {
            ShotReport lengthHorizontal = new ShotReport();
            float single;
            Pawn pawn = caster as Pawn;
            IntVec3 cell = target.Cell;
            lengthHorizontal.distance = (cell - caster.Position).LengthHorizontal;
            lengthHorizontal.target = target.ToTargetInfo(caster.Map);
            single = (pawn == null ? 0.96f : pawn.GetStatValue(StatDefOf.ShootingAccuracy, true));
            lengthHorizontal.factorFromShooterAndDist = Mathf.Pow(single, lengthHorizontal.distance);
            if (lengthHorizontal.factorFromShooterAndDist < 0.0201f)
            {
                lengthHorizontal.factorFromShooterAndDist = 0.0201f;
            }
            lengthHorizontal.factorFromEquipment = verb.verbProps.GetHitChanceFactor(verb.ownerEquipment, lengthHorizontal.distance);
            lengthHorizontal.covers = CoverUtility.CalculateCoverGiverSet(cell, caster.Position, caster.Map);
            lengthHorizontal.coversOverallBlockChance = CoverUtility.CalculateOverallBlockChance(cell, caster.Position, caster.Map);
            if (caster.Position.Roofed(caster.Map) || target.Cell.Roofed(caster.Map))
            {
                lengthHorizontal.factorFromWeather = 1f;
            }
            else
            {
                lengthHorizontal.factorFromWeather = caster.Map.weatherManager.CurWeatherAccuracyMultiplier;
            }
            lengthHorizontal.factorFromTargetSize = 1f;
            if (target.HasThing)
            {
                Pawn thing = target.Thing as Pawn;
                if (thing == null)
                {
                    lengthHorizontal.factorFromTargetSize = target.Thing.def.fillPercent * 1.7f;
                }
                else
                {
                    lengthHorizontal.factorFromTargetSize = thing.BodySize;
                }
                lengthHorizontal.factorFromTargetSize = Mathf.Clamp(lengthHorizontal.factorFromTargetSize, 0.5f, 2f);
            }
            lengthHorizontal.forcedMissRadius = verb.verbProps.forcedMissRadius;
            return lengthHorizontal;
        }
    }
}