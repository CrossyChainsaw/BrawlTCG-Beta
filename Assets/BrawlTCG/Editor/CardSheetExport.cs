using System;
using System.IO;
using BrawlTCG.Core;
using BrawlTCG.UnityApp;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlTCG.EditorTools
{
    /// <summary>Renders a set of cards with the real card painter into one PNG (batch mode: -executeMethod ... -cardIds 1,2 -cardSheetOut path).</summary>
    public static class CardSheetExport
    {
        const float SlotW = 360, SlotH = 460, Scale = 2;
        const int Columns = 4;

        [MenuItem("BrawlTCG/Export New Cards Sheet", priority = 100)]
        static void FromMenu()
        {
            string output = Path.Combine(Directory.GetCurrentDirectory(), "CardSheet.png");
            Render(new[] { 530, 531, 532, 110, 5007, 522, 6000 }, output);
            EditorUtility.RevealInFinder(output);
        }

        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string ids = Arg(args, "-cardIds") ?? "530,531,532,110,5007,522,6000";
            string output = Arg(args, "-cardSheetOut") ?? Path.Combine(Directory.GetCurrentDirectory(), "CardSheet.png");
            string[] parts = ids.Split(',');
            Render(Array.ConvertAll(parts, int.Parse), output);
        }

        static string Arg(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        static void Render(int[] ids, string output)
        {
            int rows = (ids.Length + Columns - 1) / Columns;
            int cols = Math.Min(Columns, ids.Length);
            int pw = (int)(cols * SlotW * Scale), ph = (int)(rows * SlotH * Scale);

            var rt = new RenderTexture(pw, ph, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            var camGo = new GameObject("SheetCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(14, 16, 34, 255);
            cam.orthographic = true;
            cam.targetTexture = rt;

            var canvasGo = new GameObject("SheetCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 1;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = Scale;

            for (int i = 0; i < ids.Length; i++)
            {
                Card card = CardCatalogue.Create(ids[i]);
                float x = (i % Columns) * SlotW + 30, y = (i / Columns) * SlotH + 30;
                var face = UI.Place(UI.Rect(card.Name, canvasGo.transform), x, y, 300, 400);
                CardArt.PaintFront(face, card, detailed: true);
            }

            Canvas.ForceUpdateCanvases();
            cam.Render();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, pw, ph), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(output, tex.EncodeToPNG());
            Debug.Log("[CardSheet] wrote " + output);

            UnityEngine.Object.DestroyImmediate(canvasGo);
            UnityEngine.Object.DestroyImmediate(camGo);
            rt.Release();
        }
    }
}
