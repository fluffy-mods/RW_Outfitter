using UnityEngine;
using Verse;

namespace Outfitter
{
    public static class Widgets_FloatRange
    {
        public enum Handle
        {
            None,
            Min,
            Max
        }

        private static Handle draggingHandle;
        private static int draggingId;

        static Widgets_FloatRange()
        {
            draggingHandle = Handle.None;
            draggingId = 0;
        }

        public static void FloatRange(Rect canvas, int id, ref FloatRange range, FloatRange sliderRange, ToStringStyle valueStyle = ToStringStyle.FloatTwo, string labelKey = null)
        {
            // margin
            canvas.xMin += 8f;
            canvas.xMax -= 8f;

            // label
            Color mainColor = GUI.color;
            GUI.color = new Color(0.4f, 0.4f, 0.4f);
            string text = range.min.ToStringByStyle(valueStyle) + " - " + range.max.ToStringByStyle(valueStyle);
            if (labelKey != null)
            {
                text = labelKey.Translate(text);
            }
            Text.Font = GameFont.Tiny;
            Rect labelRect = new Rect(canvas.x, canvas.y, canvas.width, 19f);
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, text);
            Text.Anchor = TextAnchor.UpperLeft;

            // background line
            Rect sliderRect = new Rect(canvas.x, labelRect.yMax, canvas.width, 2f);
            GUI.DrawTexture(sliderRect, BaseContent.WhiteTex);
            GUI.color = mainColor;

            // slider handle positions
            float pxPerUnit = sliderRect.width / sliderRange.Span;
            float minHandlePos = sliderRect.xMin + (range.min - sliderRange.min) * pxPerUnit;
            float maxHandlePos = sliderRect.xMin + (range.max - sliderRange.min) * pxPerUnit;

            // draw handles 
            Rect minHandleRect = new Rect(minHandlePos - 16f, sliderRect.center.y - 8f, 16f, 16f);
            GUI.DrawTexture(minHandleRect, LocalTextures.FloatRangeSliderTex);
            Rect maxHandleRect = new Rect(maxHandlePos + 16f, sliderRect.center.y - 8f, -16f, 16f);
            GUI.DrawTexture(maxHandleRect, LocalTextures.FloatRangeSliderTex);

            // interactions
            Rect interactionRect = canvas;
            interactionRect.xMin -= 8f;
            interactionRect.xMax += 8f;
            bool dragging = false;
            if (Mouse.IsOver(interactionRect) || draggingId == id)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    draggingId = id;
                    float x = Event.current.mousePosition.x;
                    if (x < minHandleRect.xMax)
                    {
                        draggingHandle = Handle.Min;
                    }
                    else if (x > maxHandleRect.xMin)
                    {
                        draggingHandle = Handle.Max;
                    }
                    else
                    {
                        float distToMin = Mathf.Abs(x - minHandleRect.xMax);
                        float distToMax = Mathf.Abs(x - (maxHandleRect.x - 16f));
                        draggingHandle = distToMin >= distToMax ? Handle.Max : Handle.Min;
                    }
                    dragging = true;
                    Event.current.Use();
                }
                if (dragging || (draggingHandle != Handle.None && Event.current.type == EventType.MouseDrag))
                {
                    // NOTE: this deviates from vanilla, vanilla seemed to assume that max == span?
                    float curPosValue = (Event.current.mousePosition.x - canvas.x) / canvas.width * sliderRange.Span + sliderRange.min;
                    curPosValue = Mathf.Clamp(curPosValue, sliderRange.min, sliderRange.max);
                    if (draggingHandle == Handle.Min)
                    {
                        range.min = curPosValue;
                        if (range.max < range.min)
                        {
                            range.max = range.min;
                        }
                    }
                    else if (draggingHandle == Handle.Max)
                    {
                        range.max = curPosValue;
                        if (range.min > range.max)
                        {
                            range.min = range.max;
                        }
                    }
                    Event.current.Use();
                }
            }
            if (draggingHandle != Handle.None && Event.current.type == EventType.MouseUp)
            {
                draggingId = 0;
                draggingHandle = Handle.None;
                Event.current.Use();
            }
        }
    }
}
