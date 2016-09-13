using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Outfitter
{
    public class Saveable_Pawn_StatDef : IExposable
    {
        public StatDef Stat;
        public StatAssignment Assignment;
        public float Weight;
  /*      
        public Saveable_Pawn_StatDef(StatDef stat, float priority, StatAssignment assignment = StatAssignment.Automatic)
        {
            Stat = stat;
            Weight = priority;
            Assignment = assignment;
        }

        public Saveable_Pawn_StatDef(KeyValuePair<StatDef, float> statDefWeightPair, StatAssignment assignment = StatAssignment.Automatic)
        {
            Stat = statDefWeightPair.Key;
            Weight = statDefWeightPair.Value;
            Assignment = assignment;
        }
*/

        public void ExposeData()
        {
            Scribe_Defs.LookDef(ref Stat, "Stat");
            Scribe_Values.LookValue(ref Assignment, "Assignment");
            Scribe_Values.LookValue(ref Weight, "Weight");
        }
    }
}
