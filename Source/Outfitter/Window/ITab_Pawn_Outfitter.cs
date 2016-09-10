using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Outfitter
{
    public class ITab_Pawn_Outfitter : ITab
    {
        private Vector2 _scrollPosition = Vector2.zero;


        public ITab_Pawn_Outfitter()
        {
            size = new Vector2(432f, 550f);
            labelKey = "OutfitterTab";
        }

        private Pawn SelPawnForGear
        {
            get
            {
                if (base.SelPawn != null)
                {
                    return base.SelPawn;
                }
                Corpse corpse = base.SelThing as Corpse;
                if (corpse != null)
                {
                    return corpse.innerPawn;
                }
                throw new InvalidOperationException("Gear tab on non-pawn non-corpse " + base.SelThing);
            }
        }

        public override void OnOpen()
        {
            Find.WindowStack.Add(new Window_Pawn_GearScore());
        }

        protected override void FillTab()
        {
            var pawnSave = MapComponent_Outfitter.Get.GetCache(SelPawnForGear);

            // Outfit + Status button
            Rect rectStatus = new Rect(10f, 15f, 120f, 30f);

            // select outfit

            if (Widgets.ButtonText(rectStatus, SelPawnForGear.outfits.CurrentOutfit.label, true, false))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();
                foreach (Outfit current in Current.Game.outfitDatabase.AllOutfits)
                {
                    Outfit localOut = current;
                    list.Add(new FloatMenuOption(localOut.label, delegate
                    {
                        SelPawnForGear.outfits.CurrentOutfit = localOut;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(list));
            }

            //edit outfit
            rectStatus = new Rect(rectStatus.xMax + 10f, rectStatus.y, rectStatus.width, rectStatus.height);

            if (Widgets.ButtonText(rectStatus, "OutfitterEditOutfit".Translate(), true, false))
            {
                Find.WindowStack.Add(new Dialog_ManageOutfits(SelPawnForGear.outfits.CurrentOutfit));
            }

            //show outfit
            rectStatus = new Rect(rectStatus.xMax + 10f, rectStatus.y, rectStatus.width, rectStatus.height);

            if (Widgets.ButtonText(rectStatus, "OutfitShow".Translate(), true, false))
            {
                Find.WindowStack.Add(new Window_Pawn_GearScore());
            }


            // Status checkboxes
            Rect rectCheckboxes = new Rect(10f, rectStatus.yMax + 15f, 130f, rectStatus.height);
            Text.Font= GameFont.Small;
            pawnSave.AddWorkStats = GUI.Toggle(new Rect(10f, rectCheckboxes.y, 120f, rectCheckboxes.height), pawnSave.AddWorkStats, "AddWorkStats".Translate());
            pawnSave.AddIndividualStats = GUI.Toggle(new Rect(140f, rectCheckboxes.y,rectCheckboxes.width+10f,rectCheckboxes.height),
                pawnSave.AddIndividualStats, "AddIndividualStats".Translate());

            Rect setWorkRect = new Rect(290f, rectCheckboxes.y, rectCheckboxes.width, rectCheckboxes.height);
            if (Widgets.ButtonText(setWorkRect, pawnSave.mainJob.ToString().Replace("00", " - ").Replace("_", " ")))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (SaveablePawn.MainJob mainJob in Enum.GetValues(typeof(SaveablePawn.MainJob)))
                {
                    options.Add(new FloatMenuOption(mainJob.ToString().Replace("00", " - ").Replace("_", " "), delegate
                    {
                        pawnSave.mainJob = mainJob;
                        pawnSave.forceStatUpdate = true;
                        
                        SelPawnForGear.mindState.nextApparelOptimizeTick = -99999;
                        //pawnStatCache.Stats.Insert(0, new Saveable_Pawn_StatDef(def, 0f, StatAssignment.Manual));
                    }));
                }
                FloatMenu window = new FloatMenu(options, "MainJob".Translate());

                Find.WindowStack.Add(window);
            }

            // main canvas
            Rect canvas = new Rect(0f, 60f, size.x, size.y-60f).ContractedBy(20f);
            GUI.BeginGroup(canvas);
            Vector2 cur = Vector2.zero;

            // header
            Rect tempHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Widgets.Label(tempHeaderRect, "PreferedTemperature".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
            GUI.color = Color.white;

            // some padding
            cur.y += 10f;

            // temperature slider
            //    SaveablePawn pawnStatCache = MapComponent_Outfitter.Get.GetCache(SelPawn);
            ApparelStatCache pawnStatCache = SelPawnForGear.GetApparelStatCache();
            FloatRange targetTemps = pawnStatCache.TargetTemperatures;
            FloatRange minMaxTemps = ApparelStatsHelper.MinMaxTemperatureRange;
            Rect sliderRect = new Rect(cur.x, cur.y, canvas.width - 20f, 40f);
            Rect tempResetRect = new Rect(sliderRect.xMax + 4f, cur.y + 10f, 16f, 16f);
            cur.y += 40f; // includes padding 

            // current temperature settings
            GUI.color = pawnSave.TargetTemperaturesOverride ? Color.white : Color.grey;
            Widgets_FloatRange.FloatRange(sliderRect, 123123123, ref targetTemps, minMaxTemps, ToStringStyle.Temperature);
            GUI.color = Color.white;

            if (Math.Abs(targetTemps.min - pawnStatCache.TargetTemperatures.min) > 1e-4 ||
                 Math.Abs(targetTemps.max - pawnStatCache.TargetTemperatures.max) > 1e-4)
            {
                pawnStatCache.TargetTemperatures = targetTemps;
            }

            if (pawnSave.TargetTemperaturesOverride)
            {
                if (Widgets.ButtonImage(tempResetRect, OutfitterTextures.resetButton))
                {
                    pawnSave.TargetTemperaturesOverride = false;
                    //   var saveablePawn = MapComponent_Outfitter.Get.GetCache(SelPawn);
                    //     saveablePawn.targetTemperaturesOverride = false;
                    pawnStatCache.UpdateTemperatureIfNecessary(true);
                }
                TooltipHandler.TipRegion(tempResetRect, "TemperatureRangeReset".Translate());
            }
            Text.Font = GameFont.Small;
            TryDrawComfyTemperatureRange(ref cur.y, canvas.width);



            // header
            Rect statsHeaderRect = new Rect(cur.x, cur.y, canvas.width, 30f);
            cur.y += 30f;
            Text.Anchor = TextAnchor.LowerLeft;
            Text.Font = GameFont.Small;
            Widgets.Label(statsHeaderRect, "PreferredStats".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            // add button
            Rect addStatRect = new Rect(statsHeaderRect.xMax - 16f, statsHeaderRect.yMin + 10f, 16f, 16f);
            if (Widgets.ButtonImage(addStatRect, OutfitterTextures.addButton))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (StatDef def in SelPawnForGear.NotYetAssignedStatDefs().OrderBy(i => i.label.ToString()))
                {
                    options.Add(new FloatMenuOption(def.LabelCap, delegate
                  {
                      SelPawnForGear.GetApparelStatCache().StatCache.Insert(0, new ApparelStatCache.StatPriority(def, 0f, StatAssignment.Manual));
                      //pawnStatCache.Stats.Insert(0, new Saveable_Pawn_StatDef(def, 0f, StatAssignment.Manual));
                  }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(addStatRect, "StatPriorityAdd".Translate());

            // line
            GUI.color = Color.grey;
            Widgets.DrawLineHorizontal(cur.x, cur.y, canvas.width);
            GUI.color = Color.white;

            // some padding
            cur.y += 10f;

            // main content in scrolling view
            Rect contentRect = new Rect(cur.x, cur.y, canvas.width, canvas.height - cur.y);
            Rect viewRect = contentRect;
            viewRect.height = SelPawnForGear.GetApparelStatCache().StatCache.Count * 30f + 10f;
            if (viewRect.height > contentRect.height)
            {
                viewRect.width -= 20f;
            }

            Widgets.BeginScrollView(contentRect, ref _scrollPosition, viewRect);
            GUI.BeginGroup(viewRect);
            cur = Vector2.zero;

            // none label
            if (!SelPawnForGear.GetApparelStatCache().StatCache.Any())
            {
                Rect noneLabel = new Rect(cur.x, cur.y, viewRect.width, 30f);
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(noneLabel, "None".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                cur.y += 30f;
            }
            else
            {
                // legend kind of thingy.
                Rect legendRect = new Rect(cur.x + (viewRect.width - 24) / 2, cur.y, (viewRect.width - 24) / 2, 20f);
                Text.Font = GameFont.Tiny;
                GUI.color = Color.grey;
                Text.Anchor = TextAnchor.LowerLeft;
                Widgets.Label(legendRect, "-1.5");
                Text.Anchor = TextAnchor.LowerRight;
                Widgets.Label(legendRect, "1.5");
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
                cur.y += 15f;

                // stat weight sliders
                foreach (ApparelStatCache.StatPriority stat in SelPawnForGear.GetApparelStatCache().StatCache)
                {
                    bool stop_UI;
                    ApparelStatCache.DrawStatRow(ref cur, viewRect.width, stat, SelPawnForGear, out stop_UI);
                    if (stop_UI)
                    {
                        // DrawStatRow can change the StatCache, invalidating the loop. So if it does that, stop looping - we'll redraw on the next tick.
                        break;
                    }
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();

            GUI.EndGroup();
        }

        public override bool IsVisible
        {
            get
            {
                Pawn selectedPawn = SelPawn;

                // thing selected is a pawn
                if (selectedPawn == null)
                {
                    Find.WindowStack.TryRemove(typeof(Window_PawnApparelDetail), false);
                    Find.WindowStack.TryRemove(typeof(Window_Pawn_GearScore), false);

                    return false;
                }

                // of this colony
                if (selectedPawn.Faction != Faction.OfPlayer)
                {
                    return false;
                }

                // and has apparel (that should block everything without apparel, animals, bots, that sort of thing)
                if (selectedPawn.apparel == null)
                {
                    return false;
                }
                return true;
            }
        }

        private void TryDrawComfyTemperatureRange(ref float curY, float width)
        {
            if (this.SelPawnForGear.Dead)
            {
                return;
            }
            Rect rect = new Rect(0f, curY, width, 22f);
            float statValue = this.SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMin, true);
            float statValue2 = this.SelPawnForGear.GetStatValue(StatDefOf.ComfyTemperatureMax, true);
            Widgets.Label(rect, string.Concat(new string[]
            {
                "ComfyTemperatureRange".Translate(),
                ": ",
                statValue.ToStringTemperature("F0"),
                " ~ ",
                statValue2.ToStringTemperature("F0")
            }));
            curY += 22f;
        }

    }
}
