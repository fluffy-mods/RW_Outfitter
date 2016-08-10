// Outfitter/ApparelStatsHelper.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2015-12-31 14:34

using System;
using System.Collections.Generic;
using System.Linq;
using Infused;
using RimWorld;
using Verse;

namespace Outfitter
{
    public static class ApparelStatsHelper
    {
        private static readonly Dictionary<Pawn, ApparelStatCache> PawnApparelStatCaches = new Dictionary<Pawn, ApparelStatCache>();
        private static readonly List<string> IgnoredWorktypeDefs = new List<string>();

        public static FloatRange MinMaxTemperatureRange => new FloatRange(-100, 100);

        // New curve
        public static readonly SimpleCurve HitPointsPercentScoreFactorCurve = new SimpleCurve
        {
            new CurvePoint( 0.0f, 0.05f ),
            new CurvePoint( 0.4f, 0.3f ),
            new CurvePoint( 0.6f, 0.75f ),
            new CurvePoint( 1f, 1f )
        //  new CurvePoint( 0.0f, 0.0f ),
        //  new CurvePoint( 0.25f, 0.15f ),
        //  new CurvePoint( 0.5f, 0.7f ),
        //  new CurvePoint( 1f, 1f )
        };

        public static ApparelStatCache GetApparelStatCache(this Pawn pawn)
        {
            if (!PawnApparelStatCaches.ContainsKey(pawn))
            {
                PawnApparelStatCaches.Add(pawn, new ApparelStatCache(pawn));
            }
            return PawnApparelStatCaches[pawn];
        }

        public static Dictionary<StatDef, float> GetWeightedApparelStats(this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();
            //       dict.Add(StatDefOf.ArmorRating_Blunt, 0.25f);
            //       dict.Add(StatDefOf.ArmorRating_Sharp, 0.25f);

            if (Find.MapConditionManager.ActiveConditions.OfType<MapCondition_ToxicFallout>().Any())
            {
                dict.Add(StatDefOf.ImmunityGainSpeed, 1f);
            }

            if (Find.MapConditionManager.ConditionIsActive(MapConditionDef.Named("PsychicDrone")))
            {
                if (Find.MapConditionManager.GetActiveCondition<MapCondition_PsychicEmanation>().gender == pawn.gender)
                {
                    switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("PsychicSensitivity")))
                    {
                        case -1:
                            {
                                dict.Add(StatDefOf.PsychicSensitivity, -0.25f);
                                break;
                            }
                        case 0:
                            {
                                dict.Add(StatDefOf.PsychicSensitivity, -0.5f);
                                break;
                            }
                        case 1:
                            {
                                dict.Add(StatDefOf.PsychicSensitivity, -0.75f);
                                break;
                            }
                        case 2:
                            {
                                dict.Add(StatDefOf.PsychicSensitivity, -1f);
                                break;
                            }
                    }
                }
            }

            if (Find.MapConditionManager.ConditionIsActive(MapConditionDef.Named("PsychicSoothe")))
            {
                if (Find.MapConditionManager.GetActiveCondition<MapCondition_PsychicEmanation>().gender == pawn.gender)
                {
                    switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("PsychicSensitivity")))
                    {
                        case -1:
                            {
                                dict.Add(StatDefOf.PsychicSensitivity, 1f);
                                break;
                            }
                        case 0:
                            {
                                dict.Add(StatDefOf.PsychicSensitivity, 0.75f);
                                break;
                            }
                        case 1:
                            {
                                dict.Add(StatDefOf.PsychicSensitivity, 0.5f);
                                break;
                            }
                        case 2:
                            {
                                dict.Add(StatDefOf.PsychicSensitivity, 0.25f);
                                break;
                            }
                    }
                }
            }

            switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("Nerves")))
            {
                case -1:
                    dict.Add(StatDefOf.MentalBreakThreshold, -0.5f);
                    break;
                case -2:
                    dict.Add(StatDefOf.MentalBreakThreshold, -1f);
                    break;
            }

            switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("Neurotic")))
            {
                case 1:
                    if (dict.ContainsKey(StatDefOf.MentalBreakThreshold))
                    {
                        dict[StatDefOf.MentalBreakThreshold] += -0.5f;
                    }
                    else
                    {
                        dict.Add(StatDefOf.MentalBreakThreshold, -0.5f);
                    }
                    break;
                case 2:
                    if (dict.ContainsKey(StatDefOf.MentalBreakThreshold))
                    {
                        dict[StatDefOf.MentalBreakThreshold] += -1f;
                    }
                    else
                    {
                        dict.Add(StatDefOf.MentalBreakThreshold, -1f);
                    }
                    break;
            }
            // Adds manual prioritiy adjustments 


            // add weights for all worktypes, multiplied by job priority
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(def => pawn.workSettings.WorkIsActive(def)))
            {
                foreach (KeyValuePair<StatDef, float> stat in GetStatsOfWorkType(workType))
                {
                    int priority = pawn.workSettings.GetPriority(workType);

                    float priorityAdjust;
                    switch (priority)
                    {
                        case 1:
                            priorityAdjust = 1f;
                            break;
                        case 2:
                            priorityAdjust = 0.5f;
                            break;
                        case 3:
                            priorityAdjust = 0.33f;
                            break;
                        case 4:
                            priorityAdjust = 0.25f;
                            break;
                        default:
                            priorityAdjust = 0.1f;
                            break;
                    }

                    float weight = stat.Value * priorityAdjust;

                    if (dict.ContainsKey(stat.Key))
                    {
                        dict[stat.Key] += weight;
                    }
                    else
                    {
                        dict.Add(stat.Key, weight);
                    }
                }
            }

            foreach (StatDef key in new List<StatDef>(dict.Keys))
            {
                if (key == StatDef.Named("MoveSpeed"))
                {
                    switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("SpeedOffset")))
                    {
                        case -1:
                            dict[key] *= 1.5f;
                            break;
                        case 1:
                            dict[key] *= 0.5f;
                            break;
                        case 2:
                            dict[key] *= 0.25f;
                            break;
                    }
                }

                if (key == StatDef.Named("WorkSpeedGlobal"))
                {
                    switch (pawn.story.traits.DegreeOfTrait(TraitDef.Named("Industriousness")))
                    {
                        case -2:
                            dict[key] *= 2f;
                            break;
                        case -1:
                            dict[key] *= 1.5f;
                            break;
                        case 1:
                            dict[key] *= 0.5f;
                            break;
                        case 2:
                            dict[key] *= 0.25f;
                            break;
                    }
                }


            }

            // normalize weights
            float max = dict.Values.Select(Math.Abs).Max();
            foreach (StatDef key in new List<StatDef>(dict.Keys))
            {
                // normalize max of absolute weigths to be 0.75
                dict[key] /= max / 0.75f;
            }

            return dict;
        }

        public static float ApparelScoreGain(Pawn pawn, Apparel ap)
        {
            // only allow shields to be considered if a primary weapon is equipped and is melee
            if (ap.def == ThingDefOf.Apparel_PersonalShield &&
                 pawn.equipment.Primary != null &&
                 !pawn.equipment.Primary.def.Verbs[0].MeleeRange)
            {
                return -1000f;
            }
            ApparelStatCache conf = new ApparelStatCache(pawn);

            // get the score of the considered apparel
            float candidateScore = conf.ApparelScoreRaw(ap, pawn);
            //    float candidateScore = ApparelStatCache.ApparelScoreRaw(ap, pawn);

            // get the current list of worn apparel
            List<Apparel> wornApparel = pawn.apparel.WornApparel;

            // check if the candidate will replace existing gear
            bool willReplace = false;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                if (!ApparelUtility.CanWearTogether(wornApparel[i].def, ap.def))
                {
                    // can't drop forced gear
                    if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[i]))
                    {
                        return -1000f;
                    }

                    // if replaces, score is difference of the two pieces of gear
                    candidateScore -= conf.ApparelScoreRaw(wornApparel[i], pawn);
                    willReplace = true;
                }
            }

            // increase score if this piece can be worn without replacing existing gear.
            if (!willReplace)
            {
                candidateScore *= 10f;
            }

            return candidateScore;
        }

        private static List<StatDef> _allApparelStats;

        public static List<StatDef> AllStatDefsModifiedByAnyApparel
        {
            get
            {
                if (_allApparelStats == null)
                {
                    _allApparelStats = new List<StatDef>();

                    // add all stat modifiers from all apparels
                    foreach (ThingDef apparel in DefDatabase<ThingDef>.AllDefsListForReading.Where(td => td.IsApparel))
                    {
                        if (apparel.equippedStatOffsets != null &&
                             apparel.equippedStatOffsets.Count > 0)
                        {
                            foreach (StatModifier modifier in apparel.equippedStatOffsets)
                            {
                                if (!_allApparelStats.Contains(modifier.stat))
                                {
                                    _allApparelStats.Add(modifier.stat);
                                }
                            }
                        }
                    }

                    // add all stat modifiers from all infusions
                    foreach (InfusionDef infusion in DefDatabase<InfusionDef>.AllDefsListForReading)
                    {
                        foreach (KeyValuePair<StatDef, StatMod> mod in infusion.stats)
                        {
                            if (!_allApparelStats.Contains(mod.Key))
                            {
                                _allApparelStats.Add(mod.Key);
                            }
                        }
                    }
                }
                return _allApparelStats;
            }
        }

        public static List<StatDef> NotYetAssignedStatDefs(this Pawn pawn)
        {
            return
                AllStatDefsModifiedByAnyApparel
                    .Except(pawn.GetApparelStatCache().StatCache.Select(prio => prio.Stat))
                    .ToList();
        }

        public static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType(WorkTypeDef worktype)
        {
            switch (worktype.defName)
            {
                case "Research":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ResearchSpeed"), 1f);
                    yield break;
                case "Cleaning":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.5f);
                    yield break;
                case "Hauling":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 1f);
                    yield break;
                case "Crafting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.3f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("StonecuttingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmeltingSpeed"), 1f);
                    yield break;
                case "Art":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.3f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SculptingSpeed"), 1f);
                    yield break;
                case "Tailoring":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.3f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TailoringSpeed"), 1f);
                    yield break;
                case "Smithing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.3f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmithingSpeed"), 1f);
                    yield break;
                case "PlantCutting":
                    yield break;
                case "Growing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.3f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("PlantWorkSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HarvestFailChance"), -0.5f);
                    yield break;
                case "Mining":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MiningSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.3f);
                    yield break;
                case "Repair":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("FixBrokenDownBuildingFailChance"), -0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.3f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.1f);
                    yield break;
                case "Construction":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ConstructionSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmoothingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.9f);
                    yield break;
                case "Hunting":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingDelayFactor"), -1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ShootingAccuracy"), 1f);
                    //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingAccuracy"), 1f); // CR
                    //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ReloadSpeed"), 0.25f); // CR
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.25f);
                    yield break;
                case "Cooking":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.05f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("WorkSpeedGlobal"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CookSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("FoodPoisonChance"), -0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BrewingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshEfficiency"), 1f);
                    yield break;
                case "Handling":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MoveSpeed"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryingCapacity"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TameAnimalChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TrainAnimalChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeHitChance"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.25f);
                    //         yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryWeight"), 0.25f); // CR
                    //         yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryBulk"), 0.25f); // CR
                    yield break;
                case "Warden":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SocialImpact"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("RecruitPrisonerChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("GiftImpact"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TradePriceImprovement"), 0.2f);
                    yield break;
                case "Flicker":
                    yield break;
                case "PatientEmergency":
                    yield break;
                case "PatientBedRest":
                    yield break;
                case "Firefighter":
                    yield break;
                case "Doctor":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MedicalOperationSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SurgerySuccessChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BaseHealingQuality"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HealingSpeed"), 0.5f);
                    yield break;
                case "Managing":
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SocialImpact"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ManagingSpeed"), 0.5f);
                    yield break;
                default:
                    if (!IgnoredWorktypeDefs.Contains(worktype.defName))
                    {
                        Log.Warning("WorkTypeDef " + worktype.defName + " not handled.");
                        IgnoredWorktypeDefs.Add(worktype.defName);
                    }
                    yield break;
            }
        }
    }
}