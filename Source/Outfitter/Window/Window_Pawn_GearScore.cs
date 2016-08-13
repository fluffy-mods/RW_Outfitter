using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace Outfitter
{
    public sealed class Window_Pawn_GearScore : Window
    {
        #region Modded 1
        private bool CanEdit { get { return SelPawn.IsColonistPlayerControlled; } }

        #endregion

        private const float TopPadding = 20f;

        private const float ThingIconSize = 32f;

        private const float ThingRowHeight = 48f;

        private const float ThingLeftX = 40f;

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;

        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static List<Thing> workingInvList = new List<Thing>();


        public bool IsVisible
        {
            get
            {
                return SelPawn.RaceProps.ToolUser || SelPawn.inventory.container.Any();
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

        public new Vector2 InitialSize = new Vector2(298f, 550f);

        protected override void SetInitialSizeAndPosition()
        {
            MainTabWindow_Inspect inspectWorker = (MainTabWindow_Inspect)MainTabDefOf.Inspect.Window;
            windowRect = new Rect(432f, (inspectWorker.PaneTopY - 30f - InitialSize.y), InitialSize.x, InitialSize.y).Rounded();
        }

        public override void WindowUpdate()
        {
            if (SelPawn == null)
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
            Rect viewRect = new Rect(0f, 0f, calcScore.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            float num = 0f;

            if (SelPawn.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in SelPawn.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
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
            Apparel ap = thing as Apparel;

            if (ap == null)
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
                    Find.WindowStack.Add(new Window_PawnApparelDetail(SelPawn,ap));
                }

                // RMB menu
                else if (Event.current.button == 1)
                {
                    List<FloatMenuOption> floatOptionList = new List<FloatMenuOption>();
                    floatOptionList.Add(new FloatMenuOption("ThingInfo".Translate(), delegate
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(thing));
                    }, MenuOptionPriority.Medium, null, null));

                    if (CanEdit)
                    {
                        ThingWithComps eq = thing as ThingWithComps;

                        Action action = null;
                        if (ap != null)
                        {
                            Apparel unused;
                            action = delegate
                            {
                                SelPawn.apparel.TryDrop(ap, out unused, SelPawn.Position, true);
                            };
                        }
                        else if (eq != null && SelPawn.equipment.AllEquipment.Contains(eq))
                        {
                            ThingWithComps unused;
                            action = delegate
                            {
                                SelPawn.equipment.TryDropEquipment(eq, out unused, SelPawn.Position, true);
                            };
                        }
                        else if (!thing.def.destroyOnDrop)
                        {
                            Thing unused;
                            action = delegate
                            {
                                SelPawn.inventory.container.TryDrop(thing, SelPawn.Position, ThingPlaceMode.Near, out unused);
                            };
                        }
                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), action, MenuOptionPriority.Medium, null, null));
                    }

                    FloatMenu window = new FloatMenu(floatOptionList, "");
                    Find.WindowStack.Add(window);
                }
            }

            #endregion Button Clicks


            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, ThingIconSize, ThingIconSize), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ThingLabelColor;
            Rect textRect = new Rect(ThingLeftX, y, width - ThingLeftX, ThingRowHeight);
            #region Modded
            ApparelStatCache conf = new ApparelStatCache(SelPawn);
            string text = thing.LabelCap;
            string text_Score = Math.Round(conf.ApparelScoreRaw(ap, SelPawn), 2).ToString("N2");
            #endregion
            if (thing is Apparel && SelPawn.outfits != null && SelPawn.outfits.forcedHandler.IsForced((Apparel)thing))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
            }
            else
            {
                text = text + ", " + text_Score;
            }
            Widgets.Label(textRect, text);
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
            TutorUIHighlighter.HighlightOpportunity("InfoCard", rect);
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
                SelPawn.equipment.TryDropEquipment(thingWithComps, out thingWithComps2, SelPawn.Position, true);
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                SelPawn.inventory.container.TryDrop(t, SelPawn.Position, ThingPlaceMode.Near, out thing, null);
            }
        }


    }
}
