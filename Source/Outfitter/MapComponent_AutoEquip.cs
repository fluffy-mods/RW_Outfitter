using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace AutoEquip
{
     class MapComponent_AutoEquip : MapComponent
    {
        public List<SaveablePawn> PawnCache = new List<SaveablePawn>();

        public SaveablePawn GetApparelStatCache(Pawn pawn)
        {
            foreach (SaveablePawn c in PawnCache)
                if (c.Pawn == pawn)
                    return c;
            SaveablePawn n = new SaveablePawn(pawn);
            PawnCache.Add(n);
            return n;
            // if (!PawnApparelStatCaches.ContainsKey(pawn))
            // {
            //     PawnApparelStatCaches.Add(pawn, new ApparelStatCache(pawn));
            // }
            // return PawnApparelStatCaches[pawn];
        }

        public static MapComponent_AutoEquip Get
        {
            get
            {
                MapComponent_AutoEquip getComponent = Find.Map.components.OfType<MapComponent_AutoEquip>().FirstOrDefault();
                if (getComponent == null)
                {
                    getComponent = new MapComponent_AutoEquip();
                    Find.Map.components.Add(getComponent);
                }

                return getComponent;
            }
        }


        public override void ExposeData()
        {
            Scribe_Collections.LookList(ref PawnCache, "Pawns", LookMode.Deep);
            base.ExposeData();
            if (PawnCache == null)
                PawnCache = new List<SaveablePawn>();
        }
    }
}
