using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PotionPanic
{
    // These helpers create real Unity UI objects: Images, Text, Buttons, Sliders and RectTransforms.
    // Positions are measured from the top-left of the 1440 x 900 design board.
    public static class PotionUI
    {
        public static readonly Color Ink = C("F3EAD8"), Muted = C("BFB5CD"), Gold = C("EFBE72"), Green = C("94D6AA");
        public static readonly Color[] Liquids = { C("EF8397"), C("9FA5F5"), C("88CCAD") };
        static Font font;
        static Sprite circle;
        public static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out Color c); return c; }
        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h);
            return r;
        }
        public static Image Panel(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var image = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        public static Text Label(Transform parent, string message, float x, float y, float w, float h, int size = 20, Color? color = null, bool bold = false)
        {
            if (font == null)
            {
#if UNITY_2023_1_OR_NEWER
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            }
            var text = Rect("Text", parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.text = message; text.fontSize = size; text.color = color ?? Ink;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.raycastTarget = false; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        public static Button Button(Transform parent, string caption, float x, float y, float w, float h, Action action, bool accent = false)
        {
            var image = Panel(parent, caption, x, y, w, h, accent ? Gold : C("44384F"));
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(.75f, .75f, .75f); colors.disabledColor = new Color(.5f, .5f, .5f);
            button.colors = colors;
            // Mouse controls avoid a stale selected button being activated after switching screens.
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            Label(image.transform, caption, 10, 0, w - 20, h, 18, accent ? C("302536") : Ink, true).alignment = TextAnchor.MiddleCenter;
            if (action != null) button.onClick.AddListener(() => action());
            return button;
        }
        public static RectTransform Meter(Transform parent, float x, float y, float w, Color color, float target = -1, float zone = 0)
        {
            var track = Panel(parent, "Meter track", x, y, w, 20, C("50455D"));
            if (target >= 0 && zone > 0)
                Panel(track.transform, "Perfect zone", w * (target-zone)/100, -4, w*zone*2/100, 28, new Color(.58f,.84f,.67f,.5f));
            var fill = Panel(track.transform, "Meter fill", 0, 4, 0, 12, color).rectTransform;
            if (target >= 0) Panel(track.transform, "Target marker", w * target/100, -5, 2, 30, Ink);
            return fill;
        }
        public static void Circle(Transform parent, float x, float y, float w, float h, Color color)
        {
            if (circle == null)
            {
                var texture = new Texture2D(96,96, TextureFormat.RGBA32,false);
                for (int j=0;j<96;j++) for (int i=0;i<96;i++)
                    texture.SetPixel(i,j,new Color(1,1,1,Mathf.Clamp01(48-Vector2.Distance(new Vector2(i+.5f,j+.5f),new Vector2(48,48)))));
                texture.Apply();
                circle = Sprite.Create(texture,new UnityEngine.Rect(0,0,96,96),new Vector2(.5f,.5f));
            }
            Panel(parent,"Circle",x,y,w,h,color).sprite = circle;
        }
        public static void Bottle(Transform parent,float x,float y,float scale,int type,Color liquid)
        {
            Panel(parent,"Cork",x+34*scale,y,32*scale,14*scale,Gold);
            Panel(parent,"Neck",x+36*scale,y+14*scale,28*scale,30*scale,C("BBCDD1"));
            if (type==0) { Circle(parent,x+5*scale,y+34*scale,90*scale,96*scale,C("BBCDD1")); Circle(parent,x+12*scale,y+47*scale,76*scale,75*scale,liquid); }
            else { Panel(parent,"Vial",x+24*scale,y+35*scale,52*scale,96*scale,C("BBCDD1")); Panel(parent,"Potion",x+30*scale,y+52*scale,40*scale,73*scale,liquid); }
            Panel(parent,"Glass shine",x+34*scale,y+57*scale,6*scale,35*scale,new Color(1,1,1,.4f));
        }
        public static void Cauldron(Transform parent,float x,float y,Color liquid)
        {
            Circle(parent,x,y+24,290,190,C("171722"));
            Panel(parent,"Left foot",x+36,y+166,25,55,C("171722")); Panel(parent,"Right foot",x+226,y+166,25,55,C("171722"));
            Circle(parent,x+10,y,270,73,C("686172")); Circle(parent,x+25,y+12,240,47,liquid);
            Label(parent,"*",x+125,y+108,60,60,44,Gold,true);
            for(int i=0;i<5;i++) Circle(parent,x+55+i*39,y-15-(i%3)*17,9+i*2,9+i*2,liquid);
        }
    }
}
