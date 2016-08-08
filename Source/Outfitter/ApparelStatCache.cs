// AutoEquip/ApparelStatCache.cs
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

namespace AutoEquip
{
    public enum StatAssignment
    {
        Manual,
        Automatic,
        Override
    }

    public class ApparelStatCache
    {

        private Pawn _pawn;
        private SaveablePawn _saveablePawn;
        private Outfit _outfit;
        private Saveable_Pawn_StatDef[] _stats;

        public ApparelStatCache(Pawn pawn)
            : this(MapComponent_AutoEquip.Get.GetCache(pawn))
        {
        }

        public ApparelStatCache(SaveablePawn saveablePawn)
        {
            _saveablePawn = saveablePawn;
            _pawn = saveablePawn.Pawn;
            _outfit = _pawn.outfits.CurrentOutfit;

            _stats = saveablePawn.NormalizeCalculedStatDef().ToArray();

        }

        public IEnumerable<Saveable_Pawn_StatDef> Stats => _stats;


        //  private int _lastStatUpdate;
        //  private int _lastTempUpdate;
        //  private Pawn _pawn;
        //  public List<ApparelStatCache.StatPriority> Stats;

        //  public ApparelStatCache(Pawn pawn)
        //  {
        //      _pawn = pawn;
        //      Stats = new List<StatPriority>();
        //      _lastStatUpdate = -5000;
        //      _lastTempUpdate = -5000;
        //  }

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

            // start score at 1
            float score = 1;

            //// make infusions ready
            //InfusionSet infusions;
            //bool infused = false;
            //StatMod mod;
            //InfusionDef prefix = null;
            //InfusionDef suffix = null;
            //if ( apparel.TryGetInfusions( out infusions ) )
            //{
            //    infused = true;
            //    prefix = infusions.Prefix.ToInfusionDef();
            //    suffix = infusions.Suffix.ToInfusionDef();
            //}

            // add values for each statdef modified by the apparel
            SaveablePawn conf = MapComponent_AutoEquip.Get.GetCache(pawn);

            foreach (Saveable_Pawn_StatDef stat in Stats)
                //foreach (ApparelStatCache.StatPriority statPriority in pawn.GetCache().StatCache)
            {
                // statbases, e.g. armor
                if (statBases.Contains(stat.Stat))
                {
                    // add stat to base score before offsets are handled ( the pawn's apparel stat cache always has armors first as it is initialized with it).
                    score += apparel.GetStatValue(stat.Stat) * stat.Weight;
                }

                // equipped offsets, e.g. movement speeds
                if (equippedOffsets.Contains(stat.Stat))
                {

                    float statValue = DialogPawnApparelDetail.GetEquippedStatValue(apparel, stat.Stat);

                    var statStrength = stat.Weight;

                    if (statValue < 1)
                    {
                        statValue *= -1;
                        statValue += 1;
                        statStrength *= -1;
                    }

                    score += statValue * statStrength;




                    // base value
                    float norm = apparel.GetStatValue(stat.Stat);
                    float adjusted = norm;

                    // add offset
                    adjusted += apparel.def.equippedStatOffsets.GetStatOffsetFromList(stat.Stat) *
                                stat.Weight;

                    // normalize
                    if (norm != 0)
                    {
                        adjusted /= norm;
                    }

                    // multiply score to favour items with multiple offsets
                    //     score *= adjusted;

                    //debug.AppendLine( statWeightPair.Key.LabelCap + ": " + score );
                }

                //// infusions
                //if( infused ) { 
                //    // prefix
                //    if ( !infusions.PassPre &&
                //         prefix.GetStatValue( statPriority.Stat, out mod ) )
                //    {
                //        score += mod.offset * statPriority.Weight;
                //        score += score * ( mod.multiplier - 1 ) * statPriority.Weight;

                //        //debug.AppendLine( statWeightPair.Key.LabelCap + " infusion: " + score );
                //    }
                //    if ( !infusions.PassSuf &&
                //         suffix.GetStatValue( statPriority.Stat, out mod ) )
                //    {
                //        score += mod.offset * statPriority.Weight;
                //        score += score * ( mod.multiplier - 1 ) * statPriority.Weight;

                //        //debug.AppendLine( statWeightPair.Key.LabelCap + " infusion: " + score );
                //    }
                //}
            }
            score += 0.125f * ApparelScoreRaw_ProtectionBaseStat(apparel);
            // offset for apparel hitpoints 
            if (apparel.def.useHitPoints)
            {
                float x = (float)apparel.HitPoints / (float)apparel.MaxHitPoints;
                score *= ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);
            }
            score += ApparelScoreRaw_Temperature(apparel, pawn);

            var temperatureScoreOffset = ApparelScoreRaw_Temperature(apparel, pawn);

            // adjust for temperatures
            score += temperatureScoreOffset / 10f;

            return score;
        }

        public static float ApparelScoreRaw_Temperature(Apparel apparel, Pawn pawn)
        {
            // temperature
            SaveablePawn newPawnSaveable = MapComponent_AutoEquip.Get.GetCache(pawn);

            FloatRange targetTemperatures = newPawnSaveable.TargetTemperatures;
            float minComfyTemperature = pawn.GetStatValue(StatDefOf.ComfyTemperatureMin);
            float maxComfyTemperature = pawn.GetStatValue(StatDefOf.ComfyTemperatureMax);

            // offsets on apparel
            float insulationCold = apparel.GetStatValue(StatDefOf.Insulation_Cold);
            float insulationHeat = apparel.GetStatValue(StatDefOf.Insulation_Heat);

            // offsets on apparel infusions
            //if( infused )
            //{
            //    // prefix
            //    if( !infusions.PassPre &&
            //         prefix.GetStatValue( StatDefOf.ComfyTemperatureMin, out mod ) )
            //    {
            //        insulationCold += mod.offset;
            //    }
            //    if( !infusions.PassPre &&
            //         prefix.GetStatValue( StatDefOf.ComfyTemperatureMax, out mod ) )
            //    {
            //        insulationHeat += mod.offset;
            //    }

            //    // suffix
            //    if( !infusions.PassSuf &&
            //         suffix.GetStatValue( StatDefOf.ComfyTemperatureMin, out mod ) )
            //    {
            //        insulationCold += mod.offset;
            //    }
            //    if( !infusions.PassSuf &&
            //         suffix.GetStatValue( StatDefOf.ComfyTemperatureMax, out mod ) )
            //    {
            //        insulationHeat += mod.offset;
            //    }
            //}

            // if this gear is currently worn, we need to make sure the contribution to the pawn's comfy temps is removed so the gear is properly scored
            if (pawn.apparel.WornApparel.Contains(apparel))
            {
                minComfyTemperature -= insulationCold;
                maxComfyTemperature -= insulationHeat;
            }

            // now for the interesting bit.
            float temperatureScoreOffset = 0f;
            float tempWeight = newPawnSaveable.TemperatureWeight;
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

        public static void DrawStatRow(ref Vector2 cur, float width, Saveable_Pawn_StatDef stat, Pawn pawn, out bool stop_ui)
        {
            // sent a signal if the statlist has changed
            stop_ui = false;

            // set up rects
            Rect labelRect = new Rect(cur.x, cur.y, (width - 24) / 2f -12f, 30f);
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
                if (Widgets.ButtonImage(buttonRect, TexButton.deleteButton))
                {
                    stat.Delete(pawn);
                    stop_ui = true;
                }
            }
            // if overridden auto assignment, reset to auto
            if (stat.Assignment == StatAssignment.Override)
            {
                buttonTooltip = "StatPriorityReset".Translate(stat.Stat.LabelCap);
                if (Widgets.ButtonImage(buttonRect, TexButton.resetButton))
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
    }
}