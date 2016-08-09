using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace Outfitter
{
     class MapComponent_Outfitter : MapComponent
    {
        public List<SaveablePawn> PawnCache = new List<SaveablePawn>();

        public SaveablePawn GetCache(Pawn pawn)
        {
            foreach (SaveablePawn c in PawnCache)
                if (c.Pawn == pawn)
                    return c;
            SaveablePawn n = new SaveablePawn { Pawn = pawn };
            PawnCache.Add(n);
            return n;

            // if (!PawnApparelStatCaches.ContainsKey(pawn))
            // {
            //     PawnApparelStatCaches.Add(pawn, new ApparelStatCache(pawn));
            // }
            // return PawnApparelStatCaches[pawn];
        }

        public static MapComponent_Outfitter Get
        {
            get
            {
                MapComponent_Outfitter getComponent = Find.Map.components.OfType<MapComponent_Outfitter>().FirstOrDefault();
                if (getComponent == null)
                {
                    getComponent = new MapComponent_Outfitter();
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
