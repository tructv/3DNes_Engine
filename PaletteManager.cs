using System.Collections.Generic;
using UnityEngine;
using Nes3D.Utils;

namespace Nes3D.Engine3D
{
    public class PaletteIndex
    {
        protected byte[] data;

        public PaletteIndex()
        {
            data = new byte[32];
        }

        public PaletteIndex(PaletteIndex source)
        {
            data = new byte[32];
            for (int i = 0; i < data.Length; i++)
                data[i] = source[i];
        }

        public byte this[int index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }

        public Color32 ToColor(int index)
        {
            if ((index & 3) == 0)
                return PaletteManager.NES_PALETTE[data[0]];
            return PaletteManager.NES_PALETTE[data[index]];
        }

        public int Length { get { return data.Length; } }

        public override int GetHashCode()
        {
            int value = 0;
            for (int i = 0; i < 32; i += 4)
                value ^= data[i] | (data[i + 1] << 8) | (data[i + 2] << 16) | (data[i + 3] << 24);
            return value;
        }

        public override bool Equals(System.Object obj)
        {
            PaletteIndex pal = obj as PaletteIndex;
            if (pal == null)
                return false;
            for (int i = 0; i < data.Length; i++)
                if (data[i] != pal[i])
                    return false;
            return true;
        }
    }

    internal static class PaletteManager
    {
        public static int[] NES_PALETTE_32 =
        {
            0x788084, 0x0000FC, 0X0000C4, 0x4028C4, 0x94008C, 0xAC0028, 0xAC1000, 0x8C1800,
            0x503000, 0x007800, 0x006800, 0x005800, 0x004058, 0x000000, 0x000000, 0x000000,
            0xBCC0C4, 0x0078FC, 0x0088FC, 0x6848FC, 0xDC00D4, 0xE40060, 0xFC3800, 0xE46018,
            0xAC8000, 0x00B800, 0x00A800, 0x00A848, 0x008894, 0x2C2C2C, 0x000000, 0x000000,
            0xFCF8FC, 0x38C0FC, 0x6888FC, 0x9C78FC, 0xFC78FC, 0xFC589C, 0xFC7858, 0xFCA048,
            0xFCB800, 0xBCF818, 0x58D858, 0x58F89C, 0x00E8E4, 0x606060, 0x000000, 0x000000,
            0xFCF8FC, 0xA4E8FC, 0xBCB8FC, 0xDCB8FC, 0xFCB8FC, 0xF4C0E0, 0xF4D0B4, 0xFCE0B4,
            0xFCD884, 0xDCF878, 0xB8F878, 0xB0F0D8, 0x00F8FC, 0xC8C0C0, 0x000000, 0x000000
        };

        public static Color32[] NES_PALETTE = new Color32[64];
        private static Dictionary<PaletteIndex, Texture2D> paletteDict;
        private static Color32[] temp;

        public static void Init()
        {
            for (int i = 0; i < NES_PALETTE.Length; i++)
            {
                NES_PALETTE[i] = Misc.Int2Color(NES_PALETTE_32[i] | (0xFF << 24));
            }

            paletteDict = new Dictionary<PaletteIndex, Texture2D>();
            temp = new Color32[32];
        }

        public static Color32 bgColor
        {
            get { return SettingManager.Ins.BgColor ? NES_PALETTE[(int)FrameManager.Ins.OnlineFrame.colorPalette[0]] : (Color32)SettingManager.Ins.CustomBgColor; }
        }

        public static Texture GetTexture(PaletteIndex palIndex)
        {
            Texture2D tex;
            if (!paletteDict.TryGetValue(palIndex, out tex))
            {
                tex = new Texture2D(4, 8, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                for (int i = 0; i < 32; i++)
                    temp[i] = NES_PALETTE[palIndex[i]];

                tex.SetPixels32(temp);
                tex.Apply();
                paletteDict[new PaletteIndex(palIndex)] = tex;
            }
            return tex;
        }

        public static void Reset()
        {
            paletteDict.Clear();
        }
    }
}