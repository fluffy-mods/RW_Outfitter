using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace AutoEquip
{
    public class SaveablePawn : IExposable
    {
        // Exposed members
        public Pawn Pawn;
        public List<Saveable_Pawn_StatDef> Stats = new List<Saveable_Pawn_StatDef>();

        public int _lastStatUpdate;
        public int _lastWorkStatUpdate;
        public bool targetTemperaturesOverride;
        private int _lastTempUpdate;

        private float _temperatureWeight;

        public float TemperatureWeight
        {
            get
            {
                UpdateTemperatureIfNecessary();
                return _temperatureWeight;
            }
        }

        private FloatRange _targetTemperatures;
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

        public List<Saveable_Pawn_StatDef> _cache;

        public SaveablePawn(Pawn pawn)
          {
              Pawn = pawn;
              _cache = new List<Saveable_Pawn_StatDef>();
              _lastStatUpdate = -5000;
              _lastTempUpdate = -5000;
          }

        public void UpdateTemperatureIfNecessary(bool force = false)
        {
            if (Find.TickManager.TicksGame - _lastTempUpdate > 1900 || force)
            {
                // get desired temperatures
                if (!targetTemperaturesOverride)
                {
                    _targetTemperatures = new FloatRange(Math.Max(GenTemperature.SeasonalTemp - 15f, ApparelStatsHelper.MinMaxTemperatureRange.min),
                                                          Math.Min(GenTemperature.SeasonalTemp + 10f, ApparelStatsHelper.MinMaxTemperatureRange.max));
                }

                if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_HeatWave>().Any())
                {
                    _targetTemperatures.min += 20;
                    _targetTemperatures.max += 20;
                }

                if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_ColdSnap>().Any())
                {
                    _targetTemperatures.min -= 20;
                    _targetTemperatures.max -= 20;
                }

                _temperatureWeight = GenTemperature.SeasonAcceptableFor(Pawn.def) ? 1f : 5f;
            }
        }

        public List<Saveable_Pawn_StatDef> StatCache
        {
            get
            {
                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - _lastStatUpdate > 1900)
                {
                    // list of auto stats
                    Dictionary<StatDef, float> updateAutoPriorities = Pawn.GetWeightedApparelStats();

                    // clear auto priorities
                    _cache.RemoveAll(stat => stat.Assignment == StatAssignment.Automatic);

                    // loop over each (new) stat
                    foreach (KeyValuePair<StatDef, float> pair in updateAutoPriorities)
                    {
                        // find index of existing priority for this stat
                        int i = _cache.FindIndex(stat => stat.Stat == pair.Key);

                        // if index -1 it doesnt exist yet, add it
                        if (i < 0)
                        {
                            _cache.Add(new Saveable_Pawn_StatDef(pair));
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
        /*
        public class StatPriority
        {
            public StatDef Stat { get; }
            public StatAssignment Assignment { get; set; }
            public float Weight { get; set; }

            public StatPriority(StatDef stat, float priority, StatAssignment assignment = StatAssignment.Automatic)
            {
                Stat = stat;
                Weight = priority;
                Assignment = assignment;
            }

            public StatPriority(KeyValuePair<StatDef, float> statDefWeightPair,
                StatAssignment assignment = StatAssignment.Automatic)
            {
                Stat = statDefWeightPair.Key;
                Weight = statDefWeightPair.Value;
                Assignment = assignment;
            }

            public void Delete(Pawn pawn)
            {
                pawn.GetApparelStatCache()._cache.Remove(this);
            }

            public void Reset(Pawn pawn)
            {
                Dictionary<StatDef, float> stats = pawn.GetWeightedApparelStats();
                Weight = stats[Stat];
                Assignment = StatAssignment.Automatic;
            }
        }
        */
        public void ExposeData()
        {
            Scribe_References.LookReference(ref Pawn, "Pawn");
            Scribe_Collections.LookList(ref Stats, "Stats", LookMode.Deep);
        }
    }
}