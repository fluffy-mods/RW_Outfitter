using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class SaveablePawn : IExposable
    {
        // Exposed members
        public Pawn Pawn;
        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();

        public int _lastStatUpdate;
        public int _lastWorkStatUpdate;

        public void ExposeData()
        {
            Scribe_References.LookReference(ref Pawn, "Pawn");
            Scribe_Collections.LookList(ref Stats, "Stats", LookMode.Deep);
        }

    }
}