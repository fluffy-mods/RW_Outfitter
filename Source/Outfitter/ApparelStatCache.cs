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
    //  private int _lastStatUpdate;
    //  private int _lastTempUpdate;
    //  private Pawn _pawn;
    //  public List<ApparelStatCache.StatPriority> _cache;

    //  public ApparelStatCache(Pawn pawn)
    //  {
    //      _pawn = pawn;
    //      _cache = new List<StatPriority>();
    //      _lastStatUpdate = -5000;
    //      _lastTempUpdate = -5000;
    //  }


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