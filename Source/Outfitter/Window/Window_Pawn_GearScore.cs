using System;
using System.Collections.Generic;
using System.Linq;
using Outfitter.Window;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Outfitter
{
    public sealed class Window_Pawn_GearScore : Verse.Window
    {
        #region Modded 1
        private bool CanEdit { get { return SelPawn.IsColonistPlayerControlled; } }

        #endregion

        private const float TopPadding = 20f;

        private const float ThingIconSize = 30f;

        private const float ThingRowHeight = 64f;

        private const float ThingLeftX = 40f;

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;

        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);


        public bool IsVisible
        {
            get
            {
                if (!typeof(ITab_Pawn_Outfitter).IsVisible)
                {
                    return false;
                }

                // thing selected is a pawn
                if (SelPawn == null)
                {
                    return false;
                }

                // of this colony
                if (SelPawn.Faction != Faction.OfPlayer)
                {
                    return false;
                }

                // and has apparel (that should block everything without apparel, animals, bots, that sort of thing)
                if (SelPawn.apparel == null)
                {
                    return false;
                }
                return true;
            }
        }

        private bool CanControl
        {
            get
            {
                return SelPawn.IsColonistPlayerControlled;
            }
        }

        public Window_Pawn_GearScore()
        {
            doCloseX = true;
            preventCameraMotion = false;
        }

        public new Vector2 InitialSize = new Vector2(338f, 550f);

        protected override void SetInitialSizeAndPosition()
        {
            MainTabWindow_Inspect inspectWorker = (MainTabWindow_Inspect)MainTabDefOf.Inspect.Window;
            windowRect = new Rect(432f, (inspectWorker.PaneTopY - 30f - InitialSize.y), InitialSize.x, InitialSize.y).Rounded();
        }

        public override void WindowUpdate()
        {
            if (!IsVisible)
            {
                Close(false);
            }
        }

        private Pawn SelPawn => Find.Selector.SingleSelectedThing as Pawn;

        public override void DoWindowContents(Rect rect)
        {
            // main canvas
            Text.Font = GameFont.Small;
            //     Rect rect2 = rect.ContractedBy(10f);
            Rect calcScore = new Rect(rect.x, rect.y, rect.width, rect.height);
            GUI.BeginGroup(calcScore);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, calcScore.width, calcScore.height);
            Rect viewRect = outRect;
            viewRect.height = scrollViewHeight;
            if (viewRect.height > outRect.height)
            {
                viewRect.width -= 20f;
            }
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float num = 0f;

            if (SelPawn.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in SelPawn.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
                    var bp = "";
                    var layer = "";
                    foreach (var apparelLayer in current2.def.apparel.layers)
                    {
                        foreach (var bodyPartGroupDef in current2.def.apparel.bodyPartGroups)
                        {
                            bp += bodyPartGroupDef.LabelCap + " - ";
                        }
                        layer = apparelLayer.ToString();
                    }
                    Widgets.ListSeparator(ref num, viewRect.width, bp + layer);
                    DrawThingRowModded(ref num, viewRect.width, current2);
                }

            }


            if (Event.current.type == EventType.Layout)
            {
                scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static readonly Color _highlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static readonly Color _thingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private void DrawThingRowModded(ref float y, float width, Thing thing)
        {
            Apparel apparel = thing as Apparel;

            if (apparel == null)
            {
                DrawThingRowVanilla(ref y, width, thing);
                return;
            }

            Rect rect = new Rect(0f, y, width, ThingRowHeight);

            if (Mouse.IsOver(rect))
            {
                GUI.color = _highlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            GUI.color = _thingLabelColor;

            // Rect rect2a = new Rect(rect.width - 24f, y + 3f, 24f, 24f);
            //
            // TooltipHandler.TipRegion(rect2a, "DefInfoTip".Translate());
            // if (Widgets.ButtonImage(rect2a, LocalTextures.Info))
            // {
            //     Find.WindowStack.Add(new Dialog_InfoCard(thing));
            // }
            //
            // rect.width -= 24f;
            // if (CanControl)
            // {
            //     Rect rect2 = new Rect(rect.width - 24f, y + 3f, 24f, 24f);
            //     TooltipHandler.TipRegion(rect2, "DropThing".Translate());
            //     if (Widgets.ButtonImage(rect2, LocalTextures.Drop))
            //     {
            //         SoundDefOf.TickHigh.PlayOneShotOnCamera();
            //         InterfaceDrop(thing);
            //     }
            //     rect.width -= 24f;
            // }



            #region Button Clicks

            // LMB doubleclick

            if (Widgets.ButtonInvisible(rect))
            {
                //Left Mouse Button Menu
                if (Event.current.button == 0)
                {
                    Find.WindowStack.Add(new Window_PawnApparelDetail(SelPawn, apparel));
                }

                // RMB menu
                else if (Event.current.button == 1)
                {
                    List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                    floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(thing));
                    }));

                    floatOptionList.Add(new FloatMenuOption("OutfitterComparer".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Dialog_PawnApparelComparer(SelPawn, apparel));
                    }));

                    if (CanEdit)
                    {
                        Action dropApparel = delegate
                        {
                            SoundDefOf.TickHigh.PlayOneShotOnCamera();
                            InterfaceDrop(thing);
                        };
                        Action dropApparelHaul = delegate
                        {
                            SoundDefOf.TickHigh.PlayOneShotOnCamera();
                            InterfaceDropHaul(thing);
                        };
                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), dropApparel));
                        floatOptionList.Add(new FloatMenuOption("DropThingHaul".Translate(), dropApparelHaul));
                    }

                    FloatMenu window = new FloatMenu(floatOptionList, "");
                    Find.WindowStack.Add(window);
                }
            }

            #endregion Button Clicks


            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y + 5f, ThingIconSize, ThingIconSize), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect textRect = new Rect(ThingLeftX, y, width - ThingLeftX, ThingRowHeight - Text.LineHeight);
            Rect scoreRect = new Rect(ThingLeftX, textRect.yMax, width - ThingLeftX, Text.LineHeight);
            #region Modded
            ApparelStatCache conf = new ApparelStatCache(SelPawn);
            string text = thing.LabelCap;
            string text_Score = Math.Round(conf.ApparelScoreRaw(apparel, SelPawn), 2).ToString("N2");

            #endregion
            if (thing is Apparel && SelPawn.outfits != null && SelPawn.outfits.forcedHandler.IsForced((Apparel)thing))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
                Widgets.Label(textRect, text);
            }
            else
            {
                GUI.color = new Color(0.75f, 0.75f, 0.75f);
                if (apparel.def.useHitPoints)
                {
                    float x = apparel.HitPoints / (float)apparel.MaxHitPoints;
                    if (x < 0.5f)
                    {
                        GUI.color = Color.yellow;
                    }
                    if (x < 0.2f)
                    {
                        GUI.color = Color.red;
                    }
                }
                Widgets.Label(textRect, text);
                GUI.color = Color.white;
                Widgets.Label(scoreRect, text_Score);
            }
            y += ThingRowHeight;

        }

        private void DrawThingRowVanilla(ref float y, float width, Thing thing)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            if (Mouse.IsOver(rect))
            {
                GUI.color = (_highlightColor);
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }
            GUI.color = _thingLabelColor;
            Rect rect2a = new Rect(rect.width - 24f, y, 24f, 24f);
            UIHighlighter.HighlightOpportunity(rect, "InfoCard");
            TooltipHandler.TipRegion(rect2a, "DefInfoTip".Translate());
            if (Widgets.ButtonImage(rect2a, LocalTextures.Info))
            {
                Find.WindowStack.Add(new Dialog_InfoCard(thing));
            }
            if (CanControl)
            {
                Rect rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (Widgets.ButtonImage(rect2, LocalTextures.Drop))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    InterfaceDrop(thing);
                }
                rect.width -= 24f;
            }

            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect rect3 = new Rect(ThingLeftX, y, width - ThingLeftX, 28f);
            string text = thing.LabelCap;
            if (thing is Apparel && SelPawn.outfits != null && SelPawn.outfits.forcedHandler.IsForced((Apparel)thing))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
            }
            Widgets.Label(rect3, text);
            y += ThingRowHeight;
        }

        private void InterfaceDrop(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null)
            {
                Pawn selPawnForGear = SelPawn;
                if (selPawnForGear.drafter.CanTakeOrderedJob())
                {
                    Job job = new Job(JobDefOf.RemoveApparel, apparel);
                    job.playerForced = true;
                    selPawnForGear.drafter.TakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null && SelPawn.equipment.AllEquipment.Contains(thingWithComps))
            {
                ThingWithComps thingWithComps2;
                SelPawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps2, SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawn.inventory.container.TryDrop(t, SelPawn.Position, ThingPlaceMode.Near, out thing);
            }
        }

        private void InterfaceDropHaul(Thing t)
        {
            ThingWithComps thingWithComps = t as ThingWithComps;
            Apparel apparel = t as Apparel;
            if (apparel != null)
            {
                Pawn selPawnForGear = SelPawn;
                if (selPawnForGear.drafter.CanTakeOrderedJob())
                {
                    Job job = new Job(JobDefOf.RemoveApparel, apparel);
                    job.playerForced = true;
                    job.haulDroppedApparel = true;
                    selPawnForGear.drafter.TakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null && SelPawn.equipment.AllEquipment.Contains(thingWithComps))
            {
                ThingWithComps thingWithComps2;
                SelPawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps2, SelPawn.Position);
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawn.inventory.container.TryDrop(t, SelPawn.Position, ThingPlaceMode.Near, out thing);
            }
        }

    }
}
