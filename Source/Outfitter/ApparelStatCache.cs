// Outfitter/ApparelStatCache.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2016-01-02 13:58

using System;
using System.Collections.Generic;
using System.Linq;
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

        private Pawn _pawn;
        public int _lastStatUpdate;
        private int _lastTempUpdate;
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

        private FloatRange _targetTemperatures;

        public FloatRange TargetTemperatures
        {
            get
            {
                var pawnSave = MapComponent_Outfitter.Get.GetCache(_pawn);
                if (pawnSave.targetTemperaturesOverride)
                {
                    targetTemperaturesOverride = pawnSave.targetTemperaturesOverride;
                    _targetTemperatures = pawnSave.TargetTemperatures;
                }

                //   if (!optimized)
                //   {
                //       targetTemperaturesOverride = pawnSave.targetTemperaturesOverride;
                //       _targetTemperatures = pawnSave.TargetTemperatures;
                //       optimized = true;
                //   }

                //   if (!targetTemperaturesOverride)
                //   {
                //       pawnSave.targetTemperaturesOverride = false;
                //   }

                UpdateTemperatureIfNecessary();
                return _targetTemperatures;
            }
            set
            {
                _targetTemperatures = value;
                targetTemperaturesOverride = true;

                var pawnSave = MapComponent_Outfitter.Get.GetCache(_pawn);
                pawnSave.TargetTemperatures = value;
                pawnSave.targetTemperaturesOverride = true;
            }

        }

        public List<StatPriority> StatCache
        {
            get
            {
                var pawnSave = MapComponent_Outfitter.Get.GetCache(_pawn);

                // update auto stat priorities roughly between every vanilla gear check cycle
                if (Find.TickManager.TicksGame - _lastStatUpdate > 1900)
                {
                    // list of auto stats

                    if (_cache.Count < 1 && pawnSave.Stats.Count > 0)
                        foreach (var vari in pawnSave.Stats)
                        {
                            _cache.Add(new StatPriority(vari.Stat, vari.Weight, vari.Assignment));
                        }
                    pawnSave.Stats.Clear();

                    Dictionary<StatDef, float> updateAutoPriorities = _pawn.GetWeightedApparelStats();
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
                            _cache.Add(new StatPriority(pair));
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


                foreach (var statPriority in _cache)
                {
                    if (statPriority.Assignment != StatAssignment.Automatic)
                    {
                        if (statPriority.Assignment != StatAssignment.Override)
                            statPriority.Assignment = StatAssignment.Manual;

                        bool exists = false;
                        foreach (var stat in pawnSave.Stats)
                        {
                            if (!stat.Stat.Equals(statPriority.Stat)) continue;
                            stat.Weight = statPriority.Weight;
                            stat.Assignment = statPriority.Assignment;
                            exists = true;
                        }
                        if (!exists)
                        {
                            Saveable_Pawn_StatDef stats = new Saveable_Pawn_StatDef
                            {
                                Stat = statPriority.Stat,
                                Assignment = statPriority.Assignment,
                                Weight = statPriority.Weight
                            };
                            pawnSave.Stats.Add(stats);
                        }
                    }
                }

                return _cache;
            }
        }

        public static HashSet<StatDef> infusedOffsets;


        public delegate void ApparelScoreRawStatsHandler(Pawn pawn, Apparel apparel, StatDef statDef, ref float num);
        public delegate void ApparelScoreRawInfusionHandlers(Pawn pawn, Apparel apparel, StatDef statDef);
        public delegate void ApparelScoreRawIgnored_WTHandlers(ref List<StatDef> statDef);

        public static event ApparelScoreRawStatsHandler ApparelScoreRaw_PawnStatsHandlers;
        public static event ApparelScoreRawInfusionHandlers ApparelScoreRaw_InfusionHandlers;
        public static event ApparelScoreRawIgnored_WTHandlers Ignored_WTHandlers;

        public static void DoApparelScoreRaw_PawnStatsHandlers(Pawn pawn, Apparel apparel, StatDef statDef, ref float num)
        {
            if (ApparelScoreRaw_PawnStatsHandlers != null)
                ApparelScoreRaw_PawnStatsHandlers(pawn, apparel, statDef, ref num);
        }

        public static void FillIgnoredInfused_PawnStatsHandlers(ref List<StatDef> _allApparelStats)
        {
            if (Ignored_WTHandlers != null)
                Ignored_WTHandlers(ref _allApparelStats);
        }

        public static void FillInfusionHashset_PawnStatsHandlers(Pawn pawn, Apparel apparel, StatDef statDef)
        {
            if (ApparelScoreRaw_InfusionHandlers != null)
                ApparelScoreRaw_InfusionHandlers(pawn, apparel, statDef);
        }

        public ApparelStatCache(Pawn pawn)
        {
            _pawn = pawn;
            _cache = new List<StatPriority>();
            _lastStatUpdate = -5000;
            _lastTempUpdate = -5000;
        }


        public float ApparelScoreRaw(Apparel apparel, Pawn pawn)
        {
            // relevant apparel stats
            HashSet<StatDef> equippedOffsets = new HashSet<StatDef>();
            if (apparel.def.equippedStatOffsets != null)
            {
                foreach (StatModifier equippedStatOffset in apparel.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }

            HashSet<StatDef> statBases = new HashSet<StatDef>();
            if (apparel.def.statBases != null)
            {
                foreach (StatModifier statBase in apparel.def.statBases)
                {
                    statBases.Add(statBase.stat);
                }
            }

            infusedOffsets = new HashSet<StatDef>();
            foreach (StatPriority statPriority in _pawn.GetApparelStatCache().StatCache)
                FillInfusionHashset_PawnStatsHandlers(_pawn, apparel, statPriority.Stat);

            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel

            foreach (StatPriority statPriority in pawn.GetApparelStatCache().StatCache)
            {
                
                // statbases, e.g. armor
                if (statBases.Contains(statPriority.Stat))
                {
                    float statValue = apparel.GetStatValue(statPriority.Stat);
            //        statValue += ApparelStatCache.StatInfused(infusionSet, statPriority, ref baseInfused);
            //        DoApparelScoreRaw_PawnStatsHandlers(_pawn, apparel, statPriority.Stat, ref statValue);

                    // add stat to base score before offsets are handled ( the pawn's apparel stat cache always has armors first as it is initialized with it).

                    score += statValue * statPriority.Weight;
                }

                // equipped offsets, e.g. movement speeds
                if (equippedOffsets.Contains(statPriority.Stat))
                {
                    float statValue = GetEquippedStatValue(apparel, statPriority.Stat) - 1;
                    //  statValue += ApparelStatCache.StatInfused(infusionSet, statPriority, ref equippedInfused);
                    //DoApparelScoreRaw_PawnStatsHandlers(_pawn, apparel, statPriority.Stat, ref statValue);

                    score += statValue * statPriority.Weight;

                    // base value
                    float norm = apparel.GetStatValue(statPriority.Stat);
                    float adjusted = norm;

                    // add offset
                    adjusted += apparel.def.equippedStatOffsets.GetStatOffsetFromList(statPriority.Stat) *
                                statPriority.Weight;

                    // normalize
                    if (norm != 0)
                    {
                        adjusted /= norm;
                    }

                    // multiply score to favour items with multiple offsets
                    //     score *= adjusted;

                    //debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                // infusions
                if (infusedOffsets.Contains(statPriority.Stat))
                {
                  //  float statInfused = StatInfused(infusionSet, statPriority, ref dontcare);
                    float statInfused = 0f;
                    DoApparelScoreRaw_PawnStatsHandlers(_pawn, apparel, statPriority.Stat, ref statInfused);

                    score += statInfused * statPriority.Weight;
                }
                //        Debug.LogWarning(statPriority.Stat.LabelCap + " infusion: " + score);
            
            }
            score += ApparelScoreRaw_Temperature(apparel, pawn) / 10f;

            score += 0.125f * ApparelScoreRaw_ProtectionBaseStat(apparel);
          
            // offset for apparel hitpoints 
            if (apparel.def.useHitPoints)
            {
                float x = apparel.HitPoints / (float)apparel.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }


            return score;
        }

        public static float GetEquippedStatValue(Apparel apparel, StatDef stat)
        {

            float baseStat = apparel.GetStatValue(stat, true);
            float currentStat = baseStat + apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat);
            //            currentStat += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef);

            //   if (stat.StatDef.defName.Equals("PsychicSensitivity"))
            //   {
            //       return apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.StatDef) - baseStat;
            //   }

            if (baseStat != 0)
            {
                currentStat = currentStat / baseStat;
            }

            return currentStat;
        }



        /*
        public float ApparelScoreRaw_InsulationColdAdjust(Apparel ap)
        {
            switch (_neededWarmth)
            {
                case NeededWarmth.Warm:
                    {
                        float statValueAbstract = ap.def.GetStatValueAbstract(StatDefOf.Insulation_Cold, null);
                        float final = InsulationColdScoreFactorCurve_NeedWarm.Evaluate(statValueAbstract);
                        return final;
                    }

                case NeededWarmth.Cool:
                    {
                        float statValueAbstract = ap.def.GetStatValueAbstract(StatDefOf.Insulation_Heat, null);
                        float final = InsulationWarmScoreFactorCurve_NeedCold.Evaluate(statValueAbstract);
                        return final;
                    }
                    
                default:
                    return 1;
            }
        }
*/
        public float ApparelScoreRaw_Temperature(Apparel apparel, Pawn pawn)
        {
            // temperature

            FloatRange targetTemperatures = pawn.GetApparelStatCache().TargetTemperatures;


            float minComfyTemperature = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin);
            float maxComfyTemperature = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);


            if (_pawn.story.traits.DegreeOfTrait(TraitDef.Named("TemperaturePreference")) != 0)
            {
                //calculating trait offset because there's no way to get comfytemperaturemin without clothes
                List<Trait> traitList = (
                    from tr in _pawn.story.traits.allTraits
                    where tr.CurrentData.statOffsets != null && tr.CurrentData.statOffsets.Any(se => se.stat == StatDefOf.ComfyTemperatureMin)
                    select tr
                    ).ToList();

                foreach (Trait t in traitList)
                {
                    minComfyTemperature += t.CurrentData.statOffsets.First(se => se.stat == StatDefOf.ComfyTemperatureMin).value;
                    maxComfyTemperature += t.CurrentData.statOffsets.First(se => se.stat == StatDefOf.ComfyTemperatureMax).value;
                }
            }

            // offsets on apparel
            float insulationCold = apparel.GetStatValue(StatDefOf.Insulation_Cold);
            float insulationHeat = apparel.GetStatValue(StatDefOf.Insulation_Heat);

            // offsets on apparel infusions
            DoApparelScoreRaw_PawnStatsHandlers(_pawn,apparel, StatDefOf.ComfyTemperatureMin,ref insulationCold);
            DoApparelScoreRaw_PawnStatsHandlers(_pawn,apparel, StatDefOf.ComfyTemperatureMax, ref insulationHeat);

            // if this gear is currently worn, we need to make sure the contribution to the pawn's comfy temps is removed so the gear is properly scored
            if (pawn.apparel.WornApparel.Contains(apparel))
            {
                minComfyTemperature -= insulationCold;
                maxComfyTemperature -= insulationHeat;
            }

            // now for the interesting bit.
            float temperatureScoreOffset = 0f;
            float tempWeight = pawn.GetApparelStatCache().TemperatureWeight;
            float neededInsulation_Cold = targetTemperatures.TrueMin - minComfyTemperature;
            // isolation_cold is given as negative numbers < 0 means we're underdressed
            float neededInsulation_Warmth = targetTemperatures.TrueMax - maxComfyTemperature;
            // isolation_warm is given as positive numbers.

            // currently too cold
            if (neededInsulation_Cold < 0)
            {
                temperatureScoreOffset += -insulationCold * tempWeight;
            }
            // currently warm enough
            else
            {
                // this gear would make us too cold
                if (insulationCold > neededInsulation_Cold)
                {
                    temperatureScoreOffset += (neededInsulation_Cold - insulationCold) * tempWeight;
                }
            }

            // currently too warm
            if (neededInsulation_Warmth > 0)
            {
                temperatureScoreOffset += insulationHeat * tempWeight;
            }
            // currently cool enough
            else
            {
                // this gear would make us too warm
                if (insulationHeat < neededInsulation_Warmth)
                {
                    temperatureScoreOffset += -(neededInsulation_Warmth - insulationHeat) * tempWeight;
                }
            }
            return temperatureScoreOffset;
        }

        public static float ApparelScoreRaw_ProtectionBaseStat(Apparel ap)
        {
            float num = 1f;
            float num2 = ap.GetStatValue(StatDefOf.ArmorRating_Sharp, true) + ap.GetStatValue(StatDefOf.ArmorRating_Blunt, true) * 0.75f;
            return num + num2 * 1.25f;
        }

        public void UpdateTemperatureIfNecessary(bool force = false)
        {
            if (Find.TickManager.TicksGame - _lastTempUpdate > 1900 || force)
            {
                // get desired temperatures
                if (!targetTemperaturesOverride)
                {

                    var temp = GenTemperature.AverageTemperatureAtWorldCoordsForMonth(Find.Map.WorldCoords, GenDate.CurrentMonth);

                    _targetTemperatures = new FloatRange(Math.Max(temp - 12f, ApparelStatsHelper.MinMaxTemperatureRange.min),
                                                          Math.Min(temp + 12f, ApparelStatsHelper.MinMaxTemperatureRange.max));

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

                    if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_VolcanicWinter>().Any())
                    {
                        _targetTemperatures.min -= 7;
                        _targetTemperatures.max -= 7;
                    }
                    var pawnSave = MapComponent_Outfitter.Get.GetCache(_pawn);
                    pawnSave.targetTemperaturesOverride = false;
                }

                _temperatureWeight = GenTemperature.OutdoorTemperatureAcceptableFor(_pawn.def) ? 0.25f : 1f;
            }
        }

        public static void DrawStatRow(ref Vector2 cur, float width, StatPriority stat, Pawn pawn, out bool stop_ui)
        {
            // sent a signal if the statlist has changed
            stop_ui = false;

            // set up rects
            Rect labelRect = new Rect(cur.x, cur.y, (width - 24) / 2f - 12f, 30f);
            Rect sliderRect = new Rect(labelRect.xMax + 4f, cur.y + 5f, labelRect.width, 25f);
            Rect buttonRect = new Rect(sliderRect.xMax + 4f, cur.y + 3f, 16f, 16f);

            // draw label
            Text.Font = Text.CalcHeight(stat.Stat.LabelCap, labelRect.width) > labelRect.height
                ? GameFont.Tiny
                : GameFont.Small;
            Widgets.Label(labelRect, stat.Stat.LabelCap);
            Text.Font = GameFont.Small;

            // draw button
            // if manually added, delete the priority
            string buttonTooltip = String.Empty;
            if (stat.Assignment == StatAssignment.Manual)
            {
                buttonTooltip = "StatPriorityDelete".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.deleteButton))
                {
                    stat.Delete(pawn);
                    stop_ui = true;
                }
            }
            // if overridden auto assignment, reset to auto
            if (stat.Assignment == StatAssignment.Override)
            {
                buttonTooltip = "StatPriorityReset".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, OutfitterTextures.resetButton))
                {
                    stat.Reset(pawn);
                    stop_ui = true;
                }
            }

            // draw line behind slider
            GUI.color = new Color(.3f, .3f, .3f);
            for (int y = (int)cur.y; y < cur.y + 30; y += 5)
            {
                Widgets.DrawLineVertical((sliderRect.xMin + sliderRect.xMax) / 2f, y, 3f);
            }

            // draw slider 
            GUI.color = stat.Assignment == StatAssignment.Automatic ? Color.grey : Color.white;
            float weight = GUI.HorizontalSlider(sliderRect, stat.Weight, -1f, 1f);
            if (Mathf.Abs(weight - stat.Weight) > 1e-4)
            {
                stat.Weight = weight;
                if (stat.Assignment == StatAssignment.Automatic)
                {
                    stat.Assignment = StatAssignment.Override;
                }
            }
            GUI.color = Color.white;

            // tooltips
            TooltipHandler.TipRegion(labelRect, stat.Stat.LabelCap + "\n\n" + stat.Stat.description);
            if (buttonTooltip != String.Empty)
                TooltipHandler.TipRegion(buttonRect, buttonTooltip);
            TooltipHandler.TipRegion(sliderRect, stat.Weight.ToStringByStyle(ToStringStyle.FloatTwo));

            // advance row
            cur.y += 30f;
        }

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

                var pawnSave = MapComponent_Outfitter.Get.GetCache(pawn);
                pawnSave.Stats.RemoveAll(i => i.Stat == Stat);
            }

            public void Reset(Pawn pawn)
            {
                Dictionary<StatDef, float> stats = pawn.GetWeightedApparelStats();
                Weight = stats[Stat];
                Assignment = StatAssignment.Automatic;

                var pawnSave = MapComponent_Outfitter.Get.GetCache(pawn);
                pawnSave.Stats.RemoveAll(i => i.Stat == Stat);
            }
        }
    }
}