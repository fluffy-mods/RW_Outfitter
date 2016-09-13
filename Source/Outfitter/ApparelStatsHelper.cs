// Outfitter/ApparelStatsHelper.cs
// 
// Copyright Karel Kroeze, 2016.
// 
// Created 2015-12-31 14:34

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;
using static Outfitter.SaveablePawn.MainJob;

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
            var pawnSave = MapComponent_Outfitter.Get.GetCache(pawn);

            //       dict.Add(StatDefOf.ArmorRating_Blunt, 0.25f);
            //       dict.Add(StatDefOf.ArmorRating_Sharp, 0.25f);

            // Adds manual prioritiy adjustments 
            if (pawnSave.AddWorkStats)
            {
                // add weights for all worktypes, multiplied by job priority
                foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(def => pawn.workSettings.WorkIsActive(def)))
                {
                    foreach (KeyValuePair<StatDef, float> stat in GetStatsOfWorkType(pawn, workType))
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
                                priorityAdjust = 0.25f;
                                break;
                            case 4:
                                priorityAdjust = 0.125f;
                                break;
                            default:
                                priorityAdjust = 0.125f;
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
                    if (key == StatDefOf.MoveSpeed)
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

                    if (key == StatDefOf.WorkSpeedGlobal)
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
            }

            if (dict.Count > 0)
            {
                // normalize weights
                float max = dict.Values.Select(Math.Abs).Max();
                foreach (StatDef key in new List<StatDef>(dict.Keys))
                {
                    // normalize max of absolute weigths to be 1
                    dict[key] /= max / 1f;
                }
            }

            return dict;
        }

        public static Dictionary<StatDef, float> GetWeightedApparelIndividualStats(this Pawn pawn)
        {
            Dictionary<StatDef, float> dict = new Dictionary<StatDef, float>();
            var pawnSave = MapComponent_Outfitter.Get.GetCache(pawn);

            //       dict.Add(StatDefOf.ArmorRating_Blunt, 0.25f);
            //       dict.Add(StatDefOf.ArmorRating_Sharp, 0.25f);

            if (pawnSave.AddIndividualStats)
            {
                #region MapConditions

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
                #endregion


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
            }

            if (dict.Count > 0)
            {
                // normalize weights
                float max = dict.Values.Select(Math.Abs).Max();
                foreach (StatDef key in new List<StatDef>(dict.Keys))
                {
                    // normalize max of absolute weigths to be 0.5
                    dict[key] /= max / 0.5f;
                }
            }

            return dict;
        }

        [Detour(typeof(JobGiver_OptimizeApparel), bindingFlags = (BindingFlags.Static | BindingFlags.Public))]
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

                    ApparelStatCache.FillIgnoredInfused_PawnStatsHandlers(ref _allApparelStats);

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

        public static IEnumerable<KeyValuePair<StatDef, float>> GetStatsOfWorkType(Pawn pawn, WorkTypeDef worktype)
        {
            var pawnSave = MapComponent_Outfitter.Get.GetCache(pawn);

            if (pawnSave.mainJob == Soldier00Close_Combat)
            {
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -3f);
                yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 2.4f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_Cooldown, -2.4f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageAmount, 1.2f);
                yield break;
            }

            if (pawnSave.mainJob == Soldier00Ranged_Combat)
            {
                yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1.5f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 3f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 1.5f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 1.5f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 1.8f);
                yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -3f);
                yield break;
            }

            switch (worktype.defName)
            {
                case "Firefighter":
                    yield break;
                case "PatientEmergency":
                    yield break;
                case "Doctor":
                    if (pawnSave.mainJob == Doctor)
                    {
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MedicalOperationSpeed"), 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SurgerySuccessChance"), 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BaseHealingQuality"), 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HealingSpeed"), 1.5f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MedicalOperationSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SurgerySuccessChance"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BaseHealingQuality"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("HealingSpeed"), 0.5f);
                    yield break;

                case "PatientBedRest":
                    yield break;
                case "Flicker":
                    yield break;
                case "Warden":
                    if (pawnSave.mainJob == Warden)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 1.5f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.RecruitPrisonerChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.GiftImpact, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.TradePriceImprovement, 0.6f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.RecruitPrisonerChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.GiftImpact, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TradePriceImprovement, 0.2f);
                    yield break;
                case "Handling":
                    if (pawnSave.mainJob == Handler)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.TameAnimalChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.TrainAnimalChance, 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 1.25f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 1.25f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_Cooldown, -0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageAmount, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.6f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TameAnimalChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.TrainAnimalChance, 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyTouch, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_Cooldown, -0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeWeapon_DamageAmount, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.2f);
                    //         yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryWeight"), 0.25f); // CR
                    //         yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CarryBulk"), 0.25f); // CR
                    yield break;
                case "Cooking":
                    if (pawnSave.mainJob == Cook)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CookSpeed"), 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.FoodPoisonChance, -1.5f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BrewingSpeed"), 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshSpeed"), 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshEfficiency"), 3f);
                        yield break;
                    }
                    //    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.05f);
                    //     yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("CookSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FoodPoisonChance, -0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("BrewingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryFleshEfficiency"), 1f);
                    yield break;
                case "Hunting":
                    if (pawnSave.mainJob == Hunter)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1.5f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.75f);
                        //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingAccuracy"), 1f); // CR
                        //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ReloadSpeed"), 0.25f); // CR
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.75f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 1.2f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 1.2f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 1.2f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -2.4f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AimingDelayFactor, -1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ShootingAccuracy, 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("MeleeDPS"), 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MeleeHitChance, 0.25f);
                    //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("AimingAccuracy"), 1f); // CR
                    //   yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ReloadSpeed"), 0.25f); // CR
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Blunt, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ArmorRating_Sharp, 0.25f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyShort, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyMedium, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.AccuracyLong, 0.4f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.RangedWeapon_Cooldown, -0.8f);
                    yield break;
                case "Construction":
                    if (pawnSave.mainJob == Constructor)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructionSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.SmoothingSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.75f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ConstructionSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SmoothingSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield break;
                case "Repair":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.FixBrokenDownBuildingFailChance, -1f);
                    yield break;
                case "Growing":
                    if (pawnSave.mainJob == Grower)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.HarvestFailChance, -1.5f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.HarvestFailChance, -0.5f);
                    yield break;
                case "Mining":
                    if (pawnSave.mainJob == Miner)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.75f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MiningSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield break;
                case "PlantCutting":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.PlantWorkSpeed, 0.5f);
                    yield break;
                case "Smithing":
                    if (pawnSave.mainJob == Smith)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmithingSpeed"), 3f);
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmithingSpeed"), 1f);
                    yield break;
                case "Tailoring":
                    if (pawnSave.mainJob == Tailor)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TailoringSpeed"), 3f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("TailoringSpeed"), 1f);
                    yield break;
                case "Art":
                    if (pawnSave.mainJob == Artist)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SculptingSpeed"), 3f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SculptingSpeed"), 1f);
                    yield break;
                case "Crafting":
                    if (pawnSave.mainJob == Crafter)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("StonecuttingSpeed"), 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmeltingSpeed"), 3f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryMechanoidSpeed"), 1.5f);
                        yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryMechanoidEfficiency"), 1.5f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("StonecuttingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("SmeltingSpeed"), 1f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryMechanoidSpeed"), 0.5f);
                    yield return new KeyValuePair<StatDef, float>(DefDatabase<StatDef>.GetNamed("ButcheryMechanoidEfficiency"), 0.5f);
                    yield break;
                case "Hauling":
                    if (pawnSave.mainJob == Hauler)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 3f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.75f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 1f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.CarryingCapacity, 0.25f);
                    yield break;
                case "Cleaning":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.MoveSpeed, 0.5f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.25f);
                    yield break;
                case "Research":
                    if (pawnSave.mainJob == Researcher)
                    {
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.6f);
                        yield return new KeyValuePair<StatDef, float>(StatDefOf.ResearchSpeed, 3f);
                        yield break;
                    }
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.WorkSpeedGlobal, 0.2f);
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.ResearchSpeed, 1f);
                    yield break;
                case "Managing":
                    yield return new KeyValuePair<StatDef, float>(StatDefOf.SocialImpact, 0.25f);
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