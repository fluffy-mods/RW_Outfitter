using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Outfitter
{
    public class Window_PawnApparelDetail : Window
    {
        private readonly Pawn _pawn;
        private readonly Apparel _apparel;

        public Window_PawnApparelDetail(Pawn pawn, Apparel apparel)
        {
            doCloseX = true;
            closeOnEscapeKey = true;
            doCloseButton = true;
            preventCameraMotion = false;

            _pawn = pawn;
            _apparel = apparel;
        }

        private Pawn SelPawn => Find.Selector.SingleSelectedThing as Pawn;

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

        public override void WindowUpdate()
        {
            if (!IsVisible)
            {
                Close(false);
            }
        }

        protected override void SetInitialSizeAndPosition()
        {
            MainTabWindow_Inspect inspectWorker = (MainTabWindow_Inspect)MainTabDefOf.Inspect.Window;
            windowRect = new Rect(770f, (inspectWorker.PaneTopY - 30f - InitialSize.y), InitialSize.x, InitialSize.y).Rounded();
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(510f, 550f);
            }
        }

        private Vector2 _scrollPosition;
#pragma warning disable 649
        private ThingDef stuff;
#pragma warning restore 649
#pragma warning disable 649
        private Def def;
#pragma warning restore 649

        private Def Def
        {
            get
            {
                if (_apparel != null)
                    return _apparel.def;
                return def;
            }
        }

        private string GetTitle()
        {
            if (_apparel != null)
                return _apparel.LabelCap;
            ThingDef thingDef = Def as ThingDef;
            if (thingDef != null)
                return GenLabel.ThingLabel(thingDef, stuff, 1).CapitalizeFirst();
            return Def.LabelCap;
        }

        public override void DoWindowContents(Rect windowRect)
        {
            ApparelStatCache conf = new ApparelStatCache(_pawn);

            Rect rect1 = new Rect(windowRect);
            rect1.height = 34f;
            Text.Font = GameFont.Medium;
            Widgets.Label(rect1, GetTitle());
            Text.Font = GameFont.Small;

            Rect contentRect = windowRect;
            contentRect.height -= 250f;
            contentRect.y += 20f;

            GUI.BeginGroup(contentRect);

            float baseValue = 100f;
            float multiplierWidth = 100f;
            float finalValue = 100f;
            float labelWidth = contentRect.width - baseValue - multiplierWidth - finalValue - 48f;

            Rect itemRect = new Rect(0f, contentRect.y, contentRect.width / 2, Text.LineHeight * 1.2f); //original groupRect.width -8f

            DrawLine(ref itemRect,
                "Status", labelWidth,
                "Base", baseValue,
                "Strengh", multiplierWidth,
                "Score", finalValue);

            Widgets.DrawLineHorizontal(contentRect.x, contentRect.y + Text.LineHeight * 1.2f, contentRect.width);

            HashSet<StatDef> equippedOffsets = new HashSet<StatDef>();
            if (_apparel.def.equippedStatOffsets != null)
            {
                foreach (StatModifier equippedStatOffset in _apparel.def.equippedStatOffsets)
                {
                    equippedOffsets.Add(equippedStatOffset.stat);
                }
            }
            HashSet<StatDef> statBases = new HashSet<StatDef>();
            if (_apparel.def.statBases != null)
            {
                foreach (StatModifier statBase in _apparel.def.statBases)
                {
                    statBases.Add(statBase.stat);
                }
            }

            ApparelStatCache.infusedOffsets = new HashSet<StatDef>();
            foreach (ApparelStatCache.StatPriority statPriority in _pawn.GetApparelStatCache().StatCache)
                ApparelStatCache.FillInfusionHashset_PawnStatsHandlers(_pawn, _apparel, statPriority.Stat);

            Rect scrollviewRect = contentRect;

            scrollviewRect.yMin += Text.LineHeight * 2f;
            scrollviewRect.height -= Text.LineHeight;

            Rect viewRect = scrollviewRect;

            viewRect.height = (equippedOffsets.Count + statBases.Count + ApparelStatCache.infusedOffsets.Count) * Text.LineHeight * 1.2f + 16f;
            if (viewRect.height > scrollviewRect.height)
            {
                viewRect.width -= 20f;
            }

            // Detail list scrollable

            Widgets.BeginScrollView(scrollviewRect, ref _scrollPosition, viewRect);
            GUI.BeginGroup(viewRect);

            // relevant apparel stats


            // start score at 1
            float score = 1;

            // add values for each statdef modified by the apparel

            itemRect.yMax = 0f;


            foreach (ApparelStatCache.StatPriority statPriority in _pawn.GetApparelStatCache().StatCache.OrderBy(i => i.Stat.LabelCap))
            {

                string statLabel = statPriority.Stat.LabelCap;
                // statbases, e.g. armor

                //     ApparelStatCache.DoApparelScoreRaw_PawnStatsHandlers(_pawn, _apparel, statPriority.Stat, ref currentStat);

                if (statBases.Contains(statPriority.Stat))
                {
                    float statValue = _apparel.GetStatValue(statPriority.Stat);

                    //        statValue += ApparelStatCache.StatInfused(infusionSet, statPriority, ref baseInfused);

                    float statScore = statValue * statPriority.Weight;
                    score += statScore;

                    itemRect = new Rect(viewRect.x, itemRect.yMax, viewRect.width, Text.LineHeight * 1.2f);
                    if (Mouse.IsOver(itemRect))
                    {
                        GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                        GUI.color = Color.white;
                    }

                    DrawLine(ref itemRect,
                        statLabel, labelWidth,
                        statValue.ToString("N2"), baseValue,
                        statPriority.Weight.ToString("N2"), multiplierWidth,
                        statScore.ToString("N2"), finalValue);

                }

                if (equippedOffsets.Contains(statPriority.Stat))
                {
                    float statValue = ApparelStatCache.GetEquippedStatValue(_apparel, statPriority.Stat) - 1;

                    //       statValue += ApparelStatCache.StatInfused(infusionSet, statPriority, ref equippedInfused);

                    float statScore = statValue * statPriority.Weight;
                    score += statScore;

                    itemRect = new Rect(viewRect.x, itemRect.yMax, viewRect.width, Text.LineHeight * 1.2f);

                    if (Mouse.IsOver(itemRect))
                    {
                        GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                        GUI.color = Color.white;
                    }

                    DrawLine(ref itemRect,
                        statLabel, labelWidth,
                        statValue.ToString("N2"), baseValue,
                        statPriority.Weight.ToString("N2"), multiplierWidth,
                        statScore.ToString("N2"), finalValue);


                }

                GUI.color = Color.white;

            }
            foreach (ApparelStatCache.StatPriority statPriority in _pawn.GetApparelStatCache().StatCache.OrderBy(i => i.Stat.LabelCap))
            {
                GUI.color = Color.yellow;
                string statLabel = statPriority.Stat.LabelCap;

                if (ApparelStatCache.infusedOffsets.Contains(statPriority.Stat))
                {

                    //     float statInfused = ApparelStatCache.StatInfused(infusionSet, statPriority, ref dontcare);
                    float statValue = 0f;
                    ApparelStatCache.DoApparelScoreRaw_PawnStatsHandlers(_pawn, _apparel, statPriority.Stat, ref statValue);

                    float statScore = statValue * statPriority.Weight;

                    itemRect = new Rect(viewRect.x, itemRect.yMax, viewRect.width, Text.LineHeight * 1.2f);
                    if (Mouse.IsOver(itemRect))
                    {
                        GUI.DrawTexture(itemRect, TexUI.HighlightTex);
                        GUI.color = Color.white;
                    }

                    DrawLine(ref itemRect,
                        statLabel, labelWidth,
                        statValue.ToString("N2"), baseValue,
                        statPriority.Weight.ToString("N2"), multiplierWidth,
                        statScore.ToString("N2"), finalValue);


                    score += statScore;
                }
            }

            GUI.EndGroup();

            Widgets.EndScrollView();
            GUI.EndGroup();
            GUI.color = Color.white;

            Widgets.DrawLineHorizontal(contentRect.xMin, contentRect.yMax + 12f, contentRect.width);

            itemRect = new Rect(windowRect.x, contentRect.yMax + 24f, windowRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "", labelWidth,
                "Modifier", baseValue,
                "", multiplierWidth,
                "Subtotal", finalValue);

            itemRect = new Rect(windowRect.x, itemRect.yMax, windowRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "BasicStatusOfApparel".Translate(), labelWidth,
                "1.00", baseValue,
                "+", multiplierWidth,
                score.ToString("N2"), finalValue);

            score += conf.ApparelScoreRaw_Temperature(_apparel, _pawn) / 10;

            itemRect = new Rect(windowRect.x, itemRect.yMax, windowRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "OutfitterTemperature".Translate(), labelWidth,
                (conf.ApparelScoreRaw_Temperature(_apparel, _pawn) / 10).ToString("N2"), baseValue,
                "+", multiplierWidth,
                score.ToString("N2"), finalValue);


            itemRect = new Rect(windowRect.x, itemRect.yMax, windowRect.width, Text.LineHeight * 1.2f);

            float armor = ApparelStatCache.ApparelScoreRaw_ProtectionBaseStat(_apparel) * 0.05f;

            score += armor;

            DrawLine(ref itemRect,
                "OutfitterArmor".Translate(), labelWidth,
                armor.ToString("N2"), baseValue,
                "+", multiplierWidth,
                score.ToString("N2"), finalValue);

            if (_apparel.def.useHitPoints)
            {
                itemRect = new Rect(windowRect.x, itemRect.yMax, windowRect.width, Text.LineHeight * 1.2f);
                // durability on 0-1 scale
                float x = _apparel.HitPoints / (float)_apparel.MaxHitPoints;
                score = score * 0.15f + score * 0.85f * ApparelStatsHelper.HitPointsPercentScoreFactorCurve.Evaluate(x);

                DrawLine(ref itemRect,
                "OutfitterHitPoints".Translate(), labelWidth,
                x.ToString("N2"), baseValue,
                "weighted", multiplierWidth,
                score.ToString("N2"), finalValue);
            }


            itemRect = new Rect(windowRect.x, itemRect.yMax, windowRect.width, Text.LineHeight * 1.2f);
            DrawLine(ref itemRect,
                "OutfitterTotal".Translate(), labelWidth,
                "", baseValue,
                "=", multiplierWidth,
                conf.ApparelScoreRaw(_apparel, _pawn).ToString("N2"), finalValue);

            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawLine(ref Rect itemRect,
            string statDefLabelText, float statDefLabelWidth,
            string statDefValueText, float statDefValueWidth,
            string multiplierText, float multiplierWidth,
            string finalValueText, float finalValueWidth)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefLabelWidth, itemRect.height), statDefLabelText);
            itemRect.xMin += statDefLabelWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, statDefValueWidth, itemRect.height), statDefValueText);
            itemRect.xMin += statDefValueWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, multiplierWidth, itemRect.height), multiplierText);
            itemRect.xMin += multiplierWidth;

            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(new Rect(itemRect.xMin, itemRect.yMin, finalValueWidth, itemRect.height), finalValueText);
            itemRect.xMin += finalValueWidth;
        }
    }
}