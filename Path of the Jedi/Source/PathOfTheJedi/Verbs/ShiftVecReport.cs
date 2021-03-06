﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace PathOfTheJedi
{
    public class ShiftVecReport
    {
        public LocalTargetInfo target = null;
        public Pawn targetPawn
        {
            get
            {
                return target.Thing as Pawn;
            }
        }
        public float aimingAccuracy = 1f;
        public float aimEfficiency = 1f;

        private float accuracyFactorInt = -1f;
        public float accuracyFactor
        {
            get
            {
                if (accuracyFactorInt < 0)
                {
                    accuracyFactorInt = (1.5f - aimingAccuracy) / aimEfficiency;
                }
                return accuracyFactorInt;
            }
        }

        public float circularMissRadius = 0f;
        public float indirectFireShift = 0f;

        // Visibility variables
        public float lightingShift = 0f;
        public float weatherShift = 0f;
        private float visibilityShiftInt = -1f;
        public float visibilityShift
        {
            get
            {
                if (visibilityShiftInt < 0)
                {
                    visibilityShiftInt = (lightingShift + weatherShift) * (shotDist / 50) * (2 - aimingAccuracy);
                }
                return visibilityShiftInt;
            }
        }

        // Leading variables
        public float shotSpeed = 0f;
        private bool targetIsMoving
        {
            get
            {
                return targetPawn != null && targetPawn.pather != null && targetPawn.pather.Moving;
            }
        }
        private float leadDistInt = -1f;
        public float leadDist
        {
            get
            {
                if (leadDistInt < 0)
                {
                    if (targetIsMoving)
                    {
                        float targetSpeed = Jedi_Utility.GetMoveSpeed(targetPawn);
                        float timeToTarget = shotDist / shotSpeed;
                        leadDistInt = targetSpeed * timeToTarget;
                    }
                    else
                    {
                        leadDistInt = 0f;
                    }
                }
                return leadDistInt;
            }
        }
        public float leadShift
        {
            get
            {
                return leadDist * Mathf.Min(accuracyFactor, 3);
            }
        }

        // Range variables
        public float shotDist = 0f;
        public float distShift
        {
            get
            {
                return shotDist * Mathf.Min(accuracyFactor * 0.25f, 0.8f);
            }
        }

        public bool isAiming = false;
        public float swayDegrees = 0f;
        public float spreadDegrees = 0f;
        public Thing cover = null;

        // Copy-constructor
        public ShiftVecReport(ShiftVecReport report)
        {
            target = report.target;
            aimEfficiency = report.aimEfficiency;
            aimingAccuracy = report.aimingAccuracy;
            circularMissRadius = report.circularMissRadius;
            indirectFireShift = report.indirectFireShift;
            lightingShift = report.lightingShift;
            shotSpeed = report.shotSpeed;
            shotDist = report.shotDist;
            isAiming = report.isAiming;
            swayDegrees = report.swayDegrees;
            spreadDegrees = report.spreadDegrees;
            cover = report.cover;
        }

        public ShiftVecReport()
        {
        }

        public Vector2 GetRandCircularVec()
        {
            Vector2 vec = Jedi_Utility.GenRandInCircle(visibilityShift + circularMissRadius + indirectFireShift);
            return vec;
        }

        public float GetRandDist()
        {
            float dist = shotDist + UnityEngine.Random.Range(-distShift, distShift);
            return dist;
        }

        public Vector3 GetRandLeadVec()
        {
            Vector3 leadVec = new Vector3();
            if (targetIsMoving)
            {
                Vector3 moveVec = (targetPawn.pather.nextCell - targetPawn.Position).ToVector3();
                leadVec = moveVec * (leadDist + UnityEngine.Random.Range(-leadShift, leadShift));
            }
            return leadVec;
        }

        public Vector2 GetRandSpreadVec()
        {
            Vector2 vec = UnityEngine.Random.insideUnitCircle * spreadDegrees;
            return vec;
        }

        public static string AsPercent(float pct)
        {
            return Mathf.RoundToInt(100f * pct) + "%";
        }

        public string GetTextReadout()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (visibilityShift > 0)
            {
                stringBuilder.AppendLine("   " + "CR_VisibilityError".Translate() + "\t" + GenText.ToStringByStyle(visibilityShift, ToStringStyle.FloatTwo) + " c");

                if (lightingShift > 0)
                {
                    stringBuilder.AppendLine("      " + "Darkness".Translate() + "\t" + AsPercent(lightingShift));
                }
                if (weatherShift > 0)
                {
                    stringBuilder.AppendLine("      " + "Weather".Translate() + "\t" + AsPercent(weatherShift));
                }
            }
            if (leadShift > 0)
            {
                stringBuilder.AppendLine("   " + "CR_LeadError".Translate() + "\t" + GenText.ToStringByStyle(leadShift, ToStringStyle.FloatTwo) + " c");
            }
            if (distShift > 0)
            {
                stringBuilder.AppendLine("   " + "CR_RangeError".Translate() + "\t" + GenText.ToStringByStyle(distShift, ToStringStyle.FloatTwo) + " c");
            }
            if (swayDegrees > 0)
            {
                stringBuilder.AppendLine("   " + "CR_Sway".Translate() + "\t\t" + GenText.ToStringByStyle(swayDegrees, ToStringStyle.FloatTwo) + "°");
            }
            if (spreadDegrees > 0)
            {
                stringBuilder.AppendLine("   " + "CR_Spread".Translate() + "\t\t" + GenText.ToStringByStyle(spreadDegrees, ToStringStyle.FloatTwo) + "°");
            }
            // Don't display cover and target size if our weapon has a CEP
            if (circularMissRadius > 0)
            {
                stringBuilder.AppendLine("   " + "CR_MissRadius".Translate() + "\t" + GenText.ToStringByStyle(circularMissRadius, ToStringStyle.FloatTwo) + " c");
                if (indirectFireShift > 0)
                {
                    stringBuilder.AppendLine("   " + "CR_IndirectFire".Translate() + "\t" + GenText.ToStringByStyle(indirectFireShift, ToStringStyle.FloatTwo) + " c");
                }
            }
            else
            {
                if (cover != null)
                {
                    stringBuilder.AppendLine("   " + "CR_CoverHeight".Translate() + "\t" + GenText.ToStringByStyle(Jedi_Utility.GetCollisionHeight(cover), ToStringStyle.FloatTwo) + " c");
                }
                if (target.Thing != null)
                {
                    stringBuilder.AppendLine("   " + "CR_TargetHeight".Translate() + "\t" + GenText.ToStringByStyle(Jedi_Utility.GetCollisionHeight(target.Thing), ToStringStyle.FloatTwo) + " c");
                    stringBuilder.AppendLine("   " + "CR_TargetWidth".Translate() + "\t" + GenText.ToStringByStyle(Jedi_Utility.GetCollisionWidth(target.Thing) * 2, ToStringStyle.FloatTwo) + " c");
                }
            }
            return stringBuilder.ToString();
        }
    }
}
