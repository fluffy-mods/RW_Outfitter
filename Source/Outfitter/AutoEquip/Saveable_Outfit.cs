using System.Collections.Generic;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class Saveable_Outfit : IExposable
    {
        public Outfit Outfit;
        public bool AddWorkStats;
        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();
        public bool AppendIndividualPawnStatus;


        public void ExposeData()
        {
            Scribe_References.LookReference(ref Outfit, "Outfit");
            Scribe_Values.LookValue(ref AddWorkStats, "AddWorkStats", false, false);
            Scribe_Values.LookValue(ref AppendIndividualPawnStatus, "AppendIndividualPawnStatus", true);
            Scribe_Collections.LookList(ref Stats, "Stats", LookMode.Deep);
            //       Scribe_Collections.LookList(ref WorkStats, "WorkStats", LookMode.Deep);
        }
    }
}
