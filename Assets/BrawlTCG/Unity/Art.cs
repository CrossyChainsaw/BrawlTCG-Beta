using System.Collections.Generic;
using BrawlTCG.Core;
using UnityEngine;

namespace BrawlTCG.UnityApp
{
    public static class Art
    {
        static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (!Cache.TryGetValue(key, out Texture2D tex))
            {
                tex = Resources.Load<Texture2D>("BrawlTCG/Art/" + key);
                if (tex == null) Debug.LogWarning($"[BrawlTCG] Missing art '{key}'");
                Cache[key] = tex;
            }
            return tex;
        }

        public static Texture2D CardBack => Get("BrawlLogo");
    }

    public static class CardStyle
    {
        public static Color CardColor(Element e)
        {
            switch (e)
            {
                case Element.Magic: return new Color32(148, 0, 211, 255);   // DarkViolet
                case Element.Nature: return new Color32(0, 128, 128, 255);  // Teal
                case Element.Fire: return new Color32(139, 0, 0, 255);      // DarkRed
                case Element.Cosmic: return new Color32(0, 0, 139, 255);    // DarkBlue
                case Element.Shadow: return new Color32(30, 30, 30, 255);
                case Element.Wild: return new Color32(160, 82, 45, 255);    // Sienna
                case Element.Arctic: return new Color32(224, 255, 255, 255); // LightCyan
                default: return Color.gray;
            }
        }

        public static Color TextColor(Element e) => e == Element.Cosmic || e == Element.Shadow ? Color.white : Color.black;
    }
}
