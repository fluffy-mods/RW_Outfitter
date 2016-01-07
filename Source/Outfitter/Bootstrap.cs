using System;
using System.Collections.Generic;
using System.Reflection;
using CommunityCoreLibrary;
using RimWorld;
using Verse;

namespace Outfitter
{
    public class InjectDetour : SpecialInjector 
    {
        public override void Inject()
        {
            // detour apparel selection methods
            MethodInfo source = typeof (JobGiver_OptimizeApparel).GetMethod( "ApparelScoreGain",
                                                                             BindingFlags.Static | BindingFlags.Public );
            MethodInfo destination = typeof (ApparelStatsHelper).GetMethod( "ApparelScoreGain",
                                                                            BindingFlags.Static | BindingFlags.Public );

            Detours.TryDetourFromTo( source, destination );
        }
    }

    public class InjectTab : SpecialInjector
    {
        public override void Inject()
        {
            // inject ITab
            // TODO: Inject into other humanlike pawns.
            ThingDef def = ThingDef.Named( "Human" );
            if( def.inspectorTabs == null || def.inspectorTabs.Count == 0 )
            {
                def.inspectorTabs = new List<Type>();
                def.inspectorTabsResolved = new List<ITab>();
            }
            if( def.inspectorTabs.Contains( typeof( ITab_Pawn_Outfitter ) ) )
            {
                return;
            }

            def.inspectorTabs.Add( typeof( ITab_Pawn_Outfitter ) );
            def.inspectorTabsResolved.Add( ITabManager.GetSharedInstance( typeof( ITab_Pawn_Outfitter ) ) );
        }
    }
}
