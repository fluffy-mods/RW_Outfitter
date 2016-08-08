using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class MainTabWindow_AutoEquip : MainTabWindow
    {
        public override void PostOpen()
        {
            base.PostOpen();

            // body part groups are rediculously unoptimized, have to get them throuh iterating parts themselves.
            List<BodyPartGroupDef> groups = BodyDefOf.Human.AllParts.SelectMany( part => part.groups ).Distinct().ToList();

            // log it, because we can!
            Log.Message( String.Join( "\n", groups.Select( group => group.LabelCap ).ToArray() ) );

        //  foreach ( Pawn pawn in Find.ListerPawns.FreeColonistsSpawned )
        //  {
        //      Log.Message( pawn.NameStringShort + " stat priorities:\n" +
        //                   String.Join( "\n",
        //                                pawn.NormalizeCalculedStatDef()
        //                                    .Select( stat => stat.Key.LabelCap + ": " + stat.Value )
        //                                    .ToArray() ) );
        //
        //      Log.Message( String.Join( "\n",
        //                                Find.ListerThings.ThingsInGroup( ThingRequestGroup.Apparel )
        //                                    .Select(
        //                                        app =>
        //                                            app.LabelCap + ": " +
        //                                            ApparelStatsHelper.ApparelScoreRaw( app as Apparel, pawn ) )
        //                                    .ToArray() ) );
        //  }
        }
    }
}
