using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using Verse;

namespace Outfitter
{
  public class InjectDetour : SpecialInjector 
  {
     public override bool Inject()
     {
         // detour apparel selection methods
         MethodInfo source = typeof (JobGiver_OptimizeApparel).GetMethod( "ApparelScoreGain",
                                                                          BindingFlags.Static | BindingFlags.Public );
         MethodInfo destination = typeof (ApparelStatsHelper).GetMethod( "ApparelScoreGain",
                                                                         BindingFlags.Static | BindingFlags.Public );
     
         Detours.TryDetourFromTo( source, destination );
            return true;

        }
    }

    public class InjectTab : SpecialInjector
    {
        public override bool Inject()
        {
            // inject ITab into all humanlikes
            foreach ( ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading.Where( td => td.category == ThingCategory.Pawn && td.race.Humanlike ) )
            {
                if( def.inspectorTabs == null || def.inspectorTabs.Count == 0 )
                {
                    def.inspectorTabs = new List<Type>();
                    def.inspectorTabsResolved = new List<ITab>();
                }
                if( def.inspectorTabs.Contains( typeof( ITab_Pawn_Outfitter ) ) )
                {
                    return false;
                }

                def.inspectorTabs.Add( typeof( ITab_Pawn_Outfitter ) );
                def.inspectorTabsResolved.Add( ITabManager.GetSharedInstance( typeof( ITab_Pawn_Outfitter ) ) );
            }


            return true;
        }
    }
}
