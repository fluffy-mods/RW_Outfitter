// Outfitter/ApparelStatCache.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2016-01-02 13:58

using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    public enum StatAssignment
    {
        Manual,
        Automatic,
        Override
    }

    public class ApparelStatCache
    {
        private List<StatPriority> _cache;
        private int _lastStatUpdate;
        private int _lastTempUpdate;
        private readonly Pawn _pawn;
        private FloatRange _targetTemperatures;
        public bool targetTemperaturesOverride;
        private float _temperatureWeight;

        public float TemperatureWeight
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _temperatureWeight;
            }
        }

        public List<StatPriority> StatCache
        {
            get
            {
                // update auto stat priorities roughly between every vanilla gear check cycle
                if ( Find.TickManager.TicksGame - _lastStatUpdate > 1900 )
                {
                    // list of auto stats
                    Dictionary<StatDef, float> updateAutoPriorities = _pawn.GetWeightedApparelStats();

                    // clear auto priorities
                    _cache.RemoveAll( stat => stat.Assignment == StatAssignment.Automatic );

                    // loop over each (new) stat
                    foreach ( KeyValuePair<StatDef, float> pair in updateAutoPriorities )
                    {
                        // find index of existing priority for this stat
                        int i = _cache.FindIndex( stat => stat.Stat == pair.Key );

                        // if index -1 it doesnt exist yet, add it
                        if ( i < 0 )
                        {
                            _cache.Add( new StatPriority( pair ) );
                        }
                        else
                        {
                            // it exists, make sure existing is (now) of type override.
                            _cache[i].Assignment = StatAssignment.Override;
                        }
                    }

                    // update our time check.
                    _lastStatUpdate = Find.TickManager.TicksGame;
                }
                return _cache;
            }
        }

        public FloatRange TargetTemperatures
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _targetTemperatures;
            }
            set
            {
                _targetTemperatures = value;
                targetTemperaturesOverride = true;
            }

        }

        public ApparelStatCache( Pawn pawn )
        {
            _pawn = pawn;
            _cache = new List<StatPriority>();
            _lastStatUpdate = - 5000;
            _lastTempUpdate = - 5000;
        }

        public void UpdateTemperatureIfNecessary( bool force = false )
        {
            if ( Find.TickManager.TicksGame - _lastTempUpdate > 1900 || force )
            {
                // get desired temperatures
                if ( !targetTemperaturesOverride )
                {
                    _targetTemperatures = new FloatRange( Math.Max( GenTemperature.SeasonalTemp - 10f, ApparelStatsHelper.MinMaxTemperatureRange.min),
                                                          Math.Min( GenTemperature.SeasonalTemp + 10f, ApparelStatsHelper.MinMaxTemperatureRange.max) );
                }
                _temperatureWeight = GenTemperature.SeasonAcceptableFor( _pawn.def ) ? 1f : 5f;
            }
        }

        public static void DrawStatRow( ref Vector2 cur, float width, StatPriority stat, Pawn pawn, out bool stop_ui )
        {
            // sent a signal if the statlist has changed
            stop_ui = false;

            // set up rects
            Rect labelRect = new Rect( cur.x, cur.y, (width - 24) / 2f, 30f );
            Rect sliderRect = new Rect( labelRect.xMax + 4f, cur.y + 5f, labelRect.width, 25f );
            Rect buttonRect = new Rect( sliderRect.xMax + 4f, cur.y + 3f, 16f, 16f );

            // draw label
            Text.Font = Text.CalcHeight( stat.Stat.LabelCap, labelRect.width ) > labelRect.height
                ? GameFont.Tiny
                : GameFont.Small;
            Widgets.Label( labelRect, stat.Stat.LabelCap );
            Text.Font = GameFont.Small;

            // draw button
            // if manually added, delete the priority
            string buttonTooltip = String.Empty;
            if ( stat.Assignment == StatAssignment.Manual )
            {
                buttonTooltip = "StatPriorityDelete".Translate( stat.Stat.LabelCap );
                if( Widgets.ImageButton( buttonRect, ITab_Pawn_Outfitter.deleteButton ) )
                {
                    stat.Delete( pawn );
                    stop_ui = true;
                }
            }
            // if overridden auto assignment, reset to auto
            if ( stat.Assignment == StatAssignment.Override )
            {
                buttonTooltip = "StatPriorityReset".Translate( stat.Stat.LabelCap );
                if ( Widgets.ImageButton( buttonRect, ITab_Pawn_Outfitter.resetButton ) )
                {
                    stat.Reset( pawn );
                    stop_ui = true;
                }
            }

            // draw line behind slider
            GUI.color = new Color( .3f, .3f, .3f );
            for ( int y = (int)cur.y; y < cur.y + 30; y += 5 )
            {
                Widgets.DrawLineVertical( ( sliderRect.xMin + sliderRect.xMax ) / 2f, y, 3f );
            }

            // draw slider 
            GUI.color = stat.Assignment == StatAssignment.Automatic ? Color.grey : Color.white;
            float weight = GUI.HorizontalSlider( sliderRect, stat.Weight, - 10f, 10f );
            if ( Mathf.Abs( weight - stat.Weight ) > 1e-4 )
            {
                stat.Weight = weight;
                if ( stat.Assignment == StatAssignment.Automatic )
                {
                    stat.Assignment = StatAssignment.Override;
                }
            }
            GUI.color = Color.white;

            // tooltips
            TooltipHandler.TipRegion( labelRect, stat.Stat.LabelCap + "\n\n" + stat.Stat.description );
            if (buttonTooltip != String.Empty)
                TooltipHandler.TipRegion( buttonRect, buttonTooltip );
            TooltipHandler.TipRegion( sliderRect, stat.Weight.ToStringByStyle( ToStringStyle.FloatTwo ) );

            // advance row
            cur.y += 30f;
        }

        public class StatPriority
        {
            public StatDef Stat { get; }
            public StatAssignment Assignment { get; set; }
            public float Weight { get; set; }

            public StatPriority( StatDef stat, float priority, StatAssignment assignment = StatAssignment.Automatic )
            {
                Stat = stat;
                Weight = priority;
                Assignment = assignment;
            }

            public StatPriority( KeyValuePair<StatDef, float> statDefWeightPair,
                                 StatAssignment assignment = StatAssignment.Automatic )
            {
                Stat = statDefWeightPair.Key;
                Weight = statDefWeightPair.Value;
                Assignment = assignment;
            }

            public void Delete( Pawn pawn )
            {
                pawn.GetApparelStatCache()._cache.Remove( this );
            }

            public void Reset( Pawn pawn )
            {
                Dictionary<StatDef, float> stats = pawn.GetWeightedApparelStats();
                Weight = stats[Stat];
                Assignment = StatAssignment.Automatic;
            }
        }
    }
}