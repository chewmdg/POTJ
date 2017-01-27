using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;


namespace PathOfTheJedi
{ 
    public class Apparel : ThingWithComps
    {
        public Pawn wearer;

        private bool wornByCorpseInt;

        public bool WornByCorpse
        {
            get
            {
                return this.wornByCorpseInt;
            }
        }

        public Apparel()
        {
        }

        public virtual bool AllowVerbCast(IntVec3 root, TargetInfo targ)
        {
            return true;
        }

        public virtual bool CheckPreAbsorbDamage(DamageInfo dinfo)
        {
            return false;
        }

        public override void Destroy(DestroyMode mode = 0)
        {
            base.Destroy(mode);
            if (base.Destroyed && this.wearer != null)
            {
                this.wearer.apparel.Notify_WornApparelDestroyed(this);
            }
        }

        public virtual void DrawWornExtras()
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<bool>(ref this.wornByCorpseInt, "wornByCorpse", false, false);
        }

        public override string GetInspectString()
        {
            string inspectString = base.GetInspectString();
            if (this.WornByCorpse)
            {
                inspectString = string.Concat(inspectString, "WasWornByCorpse".Translate());
            }
            return inspectString;
        }

        public virtual float GetSpecialApparelScoreOffset()
        {
            return 0f;
        }


        public void Notify_Stripped(Pawn pawn)
        {
            if (pawn.Dead && this.def.apparel.careIfWornByCorpse)
            {
                this.wornByCorpseInt = true;
            }
        }
    }
}