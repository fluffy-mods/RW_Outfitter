using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace AutoEquip
{
    public class ITab_Pawn_GearModded : ITab
    {
        #region Modded 1
        private bool CanEdit { get { return SelPawnForGear.IsColonistPlayerControlled; } }

        #endregion

        private const float TopPadding = 20f;

        private const float ThingIconSize = 32f;

        private const float ThingRowHeight = 38f;

        private const float ThingLeftX = 36f;

        private Vector2 scrollPosition = Vector2.zero;

        private float scrollViewHeight;

        private static readonly Color ThingLabelColor = new Color(0.9f, 0.9f, 0.9f, 1f);

        private static readonly Color HighlightColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private static List<Thing> workingInvList = new List<Thing>();

        public override bool IsVisible
        {
            get
            {
                return this.SelPawnForGear.RaceProps.ToolUser || this.SelPawnForGear.inventory.container.Any<Thing>();
            }
        }

        private bool CanControl
        {
            get
            {
                return this.SelPawnForGear.IsColonistPlayerControlled;
            }
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

        public ITab_Pawn_GearModded()
        {
            this.size = new Vector2(432f, 450f);
            this.labelKey = "TabGear";
        }

        protected override void FillTab()
        {
            #region Modded

            if (SelPawnForGear.IsColonist)
            {
            }
            else
            {
                FillTabVanilla();
                return;
            }


            // main canvas


                // Outfit + Status button
                Rect rectStatus = new Rect(10f, 35f, 100f, 30f);

                // select outfit

                if (Widgets.ButtonText(rectStatus, SelPawn.outfits.CurrentOutfit.label, true, false))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    foreach (Outfit current in Current.Game.outfitDatabase.AllOutfits)
                    {
                        Outfit localOut = current;
                        list.Add(new FloatMenuOption(localOut.label, delegate
                        {
                            SelPawn.outfits.CurrentOutfit = localOut;
                        }, MenuOptionPriority.Medium, null, null));
                    }
                    Find.WindowStack.Add(new FloatMenu(list));
                }
                //edit outfit
                rectStatus = new Rect(rectStatus.xMax + 10f, rectStatus.y, rectStatus.width, rectStatus.height);

                if (Widgets.ButtonText(rectStatus, "OutfitEdit".Translate(), true, false))
                {
                    Find.WindowStack.Add(new Dialog_ManageOutfits(SelPawn.outfits.CurrentOutfit));
                }
            
            #endregion

            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, TopPadding + 50f, this.size.x, this.size.y - 20f);
            Rect rect2 = rect.ContractedBy(10f);
            Rect position = new Rect(rect2.x, rect2.y, rect2.width, rect2.height);
            GUI.BeginGroup(position);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, position.width, position.height);
            Rect viewRect = new Rect(0f, 0f, position.width - 16f, this.scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            float num = 0f;
            if (this.SelPawnForGear.equipment != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Equipment".Translate());
                foreach (ThingWithComps current in this.SelPawnForGear.equipment.AllEquipment)
                {
                    this.DrawThingRowModded(ref num, viewRect.width, current);
                }
            }
            if (this.SelPawnForGear.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in this.SelPawnForGear.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
                    this.DrawThingRowModded(ref num, viewRect.width, current2);
                }
            }
            if (this.SelPawnForGear.inventory != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Inventory".Translate());
                ITab_Pawn_GearModded.workingInvList.Clear();
                ITab_Pawn_GearModded.workingInvList.AddRange(this.SelPawnForGear.inventory.container);
                for (int i = 0; i < ITab_Pawn_GearModded.workingInvList.Count; i++)
                {
                    this.DrawThingRowModded(ref num, viewRect.width, ITab_Pawn_GearModded.workingInvList[i]);
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                this.scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected void FillTabVanilla()
        {
            Text.Font = GameFont.Small;
            Rect rect = new Rect(0f, 20f, this.size.x, this.size.y - 20f);
            Rect rect2 = rect.ContractedBy(10f);
            Rect position = new Rect(rect2.x, rect2.y, rect2.width, rect2.height);
            GUI.BeginGroup(position);
            Text.Font = GameFont.Small;
            GUI.color = Color.white;
            Rect outRect = new Rect(0f, 0f, position.width, position.height);
            Rect viewRect = new Rect(0f, 0f, position.width - 16f, this.scrollViewHeight);
            Widgets.BeginScrollView(outRect, ref this.scrollPosition, viewRect);
            float num = 0f;
            if (this.SelPawnForGear.equipment != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Equipment".Translate());
                foreach (ThingWithComps current in this.SelPawnForGear.equipment.AllEquipment)
                {
                    this.DrawThingRowVanilla(ref num, viewRect.width, current);
                }
            }
            if (this.SelPawnForGear.apparel != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Apparel".Translate());
                foreach (Apparel current2 in from ap in this.SelPawnForGear.apparel.WornApparel
                                             orderby ap.def.apparel.bodyPartGroups[0].listOrder descending
                                             select ap)
                {
                    this.DrawThingRowVanilla(ref num, viewRect.width, current2);
                }
            }
            if (this.SelPawnForGear.inventory != null)
            {
                Widgets.ListSeparator(ref num, viewRect.width, "Inventory".Translate());
                ITab_Pawn_GearModded.workingInvList.Clear();
                ITab_Pawn_GearModded.workingInvList.AddRange(this.SelPawnForGear.inventory.container);
                for (int i = 0; i < ITab_Pawn_GearModded.workingInvList.Count; i++)
                {
                    this.DrawThingRowVanilla(ref num, viewRect.width, ITab_Pawn_GearModded.workingInvList[i]);
                }
            }
            if (Event.current.type == EventType.Layout)
            {
                this.scrollViewHeight = num + 30f;
            }
            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawThingRowModded(ref float y, float width, Thing thing)
        {
            Apparel ap = thing as Apparel;

            Rect rect = new Rect(0f, y, width, ThingRowHeight);
            Widgets.InfoCardButton(rect.width - 24f, y + 4f, thing.def);
            rect.width -= 24f;
            if (this.CanControl)
            {
                Rect rect2 = new Rect(rect.width - 24f, y + 3f, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (Widgets.ButtonImage(rect2, TexButton.Drop))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    this.InterfaceDrop(thing);
                }
                rect.width -= 24f;
            }
            if (Mouse.IsOver(rect))
            {
                GUI.color = ITab_Pawn_GearModded.HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }

            #region Button Clicks

            // LMB doubleclick

            if (Widgets.ButtonInvisible(rect))
            {


                //Middle Mouse Button Menu
                if (Event.current.button == 2)
                {
                    Find.WindowStack.Add(new DialogPawnApparelDetail(SelPawn, (Apparel)thing));
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


                        #region CR Stuff #2

                        // Equip option
                        //  ThingWithComps eq = thing as ThingWithComps;
                        if (eq != null && eq.TryGetComp<CompEquippable>() != null)
                        {
                            /*
                            CompInventory compInventory = SelPawnForGear.TryGetComp<CompInventory>();
                            if (compInventory != null)
                            {
                                FloatMenuOption equipOption;
                                string eqLabel = GenLabel.ThingLabel(eq.def, eq.Stuff, 1);
                                if (SelPawnForGear.equipment.AllEquipment.Contains(eq) && SelPawnForGear.inventory != null)
                                {
                                    equipOption = new FloatMenuOption("CR_PutAway".Translate(eqLabel),
                                        delegate
                                        {
                                            ThingWithComps oldEq;
                                            SelPawnForGear.equipment.TryTransferEquipmentToContainer(SelPawnForGear.equipment.Primary, SelPawnForGear.inventory.container, out oldEq);
                                        });
                                }
                                else if (!SelPawnForGear.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                                {
                                    equipOption = new FloatMenuOption("CannotEquip".Translate(eqLabel), null);
                                }
                                else
                                {
                                    string equipOptionLabel = "Equip".Translate(eqLabel);
                                    if (eq.def.IsRangedWeapon && SelPawnForGear.story != null && SelPawnForGear.story.traits.HasTrait(TraitDefOf.Brawler))
                                    {
                                        equipOptionLabel = equipOptionLabel + " " + "EquipWarningBrawler".Translate();
                                    }
                                    equipOption = new FloatMenuOption(equipOptionLabel, delegate
                                    {
                                        compInventory.TrySwitchToWeapon(eq);
                                    });
                                }
                                floatOptionList.Add(equipOption);
                            }
                            */
                        }

                        #endregion CR Stuff #2

                        Action action = null;
                        if (ap != null)
                        {
                            Apparel unused;
                            action = delegate
                            {
                                SelPawnForGear.apparel.TryDrop(ap, out unused, SelPawnForGear.Position, true);
                            };
                        }
                        else if (eq != null && SelPawnForGear.equipment.AllEquipment.Contains(eq))
                        {
                            ThingWithComps unused;
                            action = delegate
                            {
                                SelPawnForGear.equipment.TryDropEquipment(eq, out unused, SelPawnForGear.Position, true);
                            };
                        }
                        else if (!thing.def.destroyOnDrop)
                        {
                            Thing unused;
                            action = delegate
                            {
                                SelPawnForGear.inventory.container.TryDrop(thing, SelPawnForGear.Position, ThingPlaceMode.Near, out unused);
                            };
                        }
                        floatOptionList.Add(new FloatMenuOption("DropThing".Translate(), action, MenuOptionPriority.Medium, null, null));
                    }

                    if ((SelPawnForGear != null) &&
                        (thing is Apparel))
                    {

                        floatOptionList.Add(new FloatMenuOption("AutoEquip Details", delegate
                        {
                            Find.WindowStack.Add(new DialogPawnApparelDetail(SelPawn, (Apparel)thing));
                        }, MenuOptionPriority.Medium, null, null));

                        floatOptionList.Add(new FloatMenuOption("AutoEquip Comparer", delegate
                        {
                //            Find.WindowStack.Add(new Dialog_PawnApparelComparer(pawnSave.Pawn, (Apparel)thing));
                        }, MenuOptionPriority.Medium, null, null));
                    }

                    FloatMenu window = new FloatMenu(floatOptionList, thing.LabelCap);
                    Find.WindowStack.Add(window);
                }
            }

            #endregion Button Clicks


            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, ThingIconSize, ThingIconSize), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ITab_Pawn_GearModded.ThingLabelColor;
            Rect rect3 = new Rect(ThingLeftX, y, width - ThingLeftX - 58f, ThingRowHeight);
            #region Modded
            string text = thing.LabelCap;
            string text_Score = Math.Round(ApparelStatsHelper.ApparelScoreRaw(ap, SelPawn), 2).ToString("N2");
            #endregion
            if (thing is Apparel && this.SelPawnForGear.outfits != null && this.SelPawnForGear.outfits.forcedHandler.IsForced((Apparel)thing))
            {
                text = text + ", " + "ApparelForcedLower".Translate();
            }
            else
            {
                text = text + ", " + text_Score;
            }
            Widgets.Label(rect3, text);
            y += ThingRowHeight;
        }

        private void DrawThingRowVanilla(ref float y, float width, Thing thing)
        {
            Rect rect = new Rect(0f, y, width, 28f);
            Widgets.InfoCardButton(rect.width - 24f, y, thing.def);
            rect.width -= 24f;
            if (this.CanControl)
            {
                Rect rect2 = new Rect(rect.width - 24f, y, 24f, 24f);
                TooltipHandler.TipRegion(rect2, "DropThing".Translate());
                if (Widgets.ButtonImage(rect2, TexButton.Drop))
                {
                    SoundDefOf.TickHigh.PlayOneShotOnCamera();
                    this.InterfaceDrop(thing);
                }
                rect.width -= 24f;
            }
            if (Mouse.IsOver(rect))
            {
                GUI.color = ITab_Pawn_GearModded.HighlightColor;
                GUI.DrawTexture(rect, TexUI.HighlightTex);
            }


            if (thing.def.DrawMatSingle != null && thing.def.DrawMatSingle.mainTexture != null)
            {
                Widgets.ThingIcon(new Rect(4f, y, 28f, 28f), thing);
            }
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = ITab_Pawn_GearModded.ThingLabelColor;
            Rect rect3 = new Rect(ThingLeftX, y, width - ThingLeftX, 28f);
            string text = thing.LabelCap;
            if (thing is Apparel && this.SelPawnForGear.outfits != null && this.SelPawnForGear.outfits.forcedHandler.IsForced((Apparel)thing))
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
                Pawn selPawnForGear = this.SelPawnForGear;
                if (selPawnForGear.drafter.CanTakeOrderedJob())
                {
                    Job job = new Job(JobDefOf.RemoveApparel, apparel);
                    job.playerForced = true;
                    selPawnForGear.drafter.TakeOrderedJob(job);
                }
            }
            else if (thingWithComps != null && this.SelPawnForGear.equipment.AllEquipment.Contains(thingWithComps))
            {
                ThingWithComps thingWithComps2;
                this.SelPawnForGear.equipment.TryDropEquipment(thingWithComps, out thingWithComps2, this.SelPawnForGear.Position, true);
            }
            else if (!t.def.destroyOnDrop)
            {
                Thing thing;
                this.SelPawnForGear.inventory.container.TryDrop(t, this.SelPawnForGear.Position, ThingPlaceMode.Near, out thing, null);
            }
        }
    }
}
