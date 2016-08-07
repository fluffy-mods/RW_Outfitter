using RimWorld;
using Verse;

namespace AutoEquip
{
    public class Saveable_Pawn_StatDef : IExposable
    {
        public StatDef Stat;
        public StatAssignment Assignment;
        public float Weight;

        public void ExposeData()
        {
            Scribe_Defs.LookDef(ref Stat, "Stat");
            Scribe_Values.LookValue(ref Assignment, "Assignment");
            Scribe_Values.LookValue(ref Weight, "Weight");
        }
    }
}
