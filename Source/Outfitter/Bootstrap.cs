using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommunityCoreLibrary;
using EdB.Interface;
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
                    return;
                }

                def.inspectorTabs.Add( typeof( ITab_Pawn_Outfitter ) );
                def.inspectorTabsResolved.Add( ITabManager.GetSharedInstance( typeof( ITab_Pawn_Outfitter ) ) );
            }

            // have EdB reload the tabs
            // There appears to be a race condition between EdB tab replacements and our extra tab. If EdB goes first, ours does not get shown until EdB refreshes tabs.
            // TODO: Figure out how to get to the ComponentTabReplacement to call the reset there
            foreach ( IPreference preference in Preferences.Instance.Groups.SelectMany( group => group.Preferences ) )
            {
                if ( preference is PreferenceTabArt )
                {
                    // insanely silly on off toggle just to get EdB to set the dirty toggle and reload ITabs so our custom one should always get loaded.
                    ( (PreferenceTabArt)preference ).Value = !( (PreferenceTabArt)preference ).Value;
                    ( (PreferenceTabArt)preference ).Value = !( (PreferenceTabArt)preference ).Value;
                    break;
                }
            }
        }
    }
}
