using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace AutoEquip
{
    class MapComponent_AutoEquip : MapComponent
    {
        public List<SaveablePawn> PawnCache = new List<SaveablePawn>();

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

        public SaveablePawn GetCache(Pawn pawn)
        {
            foreach (SaveablePawn c in PawnCache)
                if (c.Pawn == pawn)
                    return c;
            SaveablePawn n = new SaveablePawn { Pawn = pawn };
            PawnCache.Add(n);
            return n;
        }

        public override void ExposeData()
        {
            Scribe_Collections.LookList(ref PawnCache, "Pawns", LookMode.Deep);
            if (PawnCache == null)
                PawnCache = new List<SaveablePawn>();
        }

    }
}
