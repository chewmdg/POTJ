﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace PathOfTheJedi
{
    public class CompProperties_Charges : CompProperties
    {
        // Charges are paired as velocity / range
        public List<Vector2> charges = new List<Vector2>();

        public CompProperties_Charges()
        {
            compClass = typeof(CompCharges);
        }
    }
}
