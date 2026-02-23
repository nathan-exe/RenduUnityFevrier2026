using UnityEngine;

namespace _Scripts.Extensions
{
    public static class ColorExtensions
    {
        public static Color HueShift(this Color color, float hueShift01)
        {
            float h, s, v;
            Color.RGBToHSV(color,out h,out s,out v);
            h += hueShift01;
            h = h % 1f;
            return Color.HSVToRGB(h,s,v);
        }
    }
}