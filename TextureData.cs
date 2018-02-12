using Nes3D.Utils;
using System;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public enum ColorFormat { Index, Direct };
    public class NesRect
    {
        public int w;
        public int h;

        public int max { get { return w > h ? w : h; } }
        public int min { get { return w < h ? w : h; } }
        public int maxIndex { get { return w > h ? 0 : 1; } }
        public int minIndex { get { return 1 - maxIndex; } }

        public int Count { get { return w * h; } }

        protected NesRect()
        {
        }

        public NesRect(int _w, int _h)
        {
            w = _w;
            h = _h;
        }

        public bool CanContain(NesRect x)
        {
            return min >= x.min && max >= x.max;
        }

        public static bool operator >(NesRect x, NesRect y)
        {
            return (x.min > y.min) || (x.min == y.min && x.max >= y.max);
        }

        public static bool operator <(NesRect x, NesRect y)
        {
            return (x.min < y.min) || (x.min == y.min && x.max <= y.max);
        }
    }

    public class TextureData : NesRect
    {
        public static readonly TextureData Null = new TextureData();
        private static int GetPixel(int x, int y, int w, int h, int[] data, ColorFormat format)
        {
            x %= w;
            y %= h;
            int index = y * w + x;
            if (format == ColorFormat.Index)
            {
                int pos = (index & 15) * 2;
                return (data[index >> 4] >> pos) & 3;
            }
            else
                return data[index];
        }

        private static void SetPixel(int x, int y, int color, int w, int h, int[] data, ColorFormat format)
        {
            x %= w;
            y %= h;
            int index = y * w + x;
            if (format == ColorFormat.Index)
            {
                int pos = (index & 15) * 2;
                int mask = ~(3 << pos);
                data[index >> 4] = data[index >> 4] & mask | (color << pos);
            }
            else
                data[index] = color;
        }

        private int[] data;
        public ColorFormat format;

        const int BigSize = 64000;
        public static TextureData CreateBig()
        {
            TextureData tex = new TextureData()
            {
                data = new int[BigSize]
            };
            
            return tex;
        }

        public int Size
        {
            get { return format == ColorFormat.Index ? Mathf.CeilToInt(Count / 16f) : Count; }
        }

        private TextureData() : base(0, 0)
        {
            format = ColorFormat.Index;
        }

        public TextureData(int _w, int _h, int color = 0, ColorFormat _format = ColorFormat.Index) : base(_w, _h)
        {
            format = _format;
            data = new int[Size];

            for (int i = 0; i < data.Length; i++)
                data[i] = color;
        }

        public TextureData(TextureData tex) : base(tex.w, tex.h)
        {
            format = tex.format;
            data = new int[Size];
            Array.Copy(tex.data, data, Size);
        }

        public void CloneFrom(TextureData tex)
        {
            w = tex.w;
            h = tex.h;
            format = tex.format;

            if (data.Length != BigSize && data.Length != tex.Size)
                data = new int[tex.Size];

            Array.Copy(tex.data, data, tex.Size);
        }

        public Color32 GetColor(int x, int y)
        {
            return Int2Color(GetPixel(x, y));
        }

        public int GetPixel(int x, int y)
        {
            return GetPixel(x, y, w, h, data, format);
        }

        public void SetPixel(int x, int y, int color)
        {
            SetPixel(x, y, color, w, h, data, format);
        }

        public TextureData GetRect(int x, int y, int _w, int _h)
        {
            TextureData tex = new TextureData(_w, _h);
            for (int i = 0; i < _w; i++)
                for (int j = 0; j < _h; j++)
                    tex.SetPixel(i, j, GetPixel(x + i, y + j));
            return tex;
        }

        public bool CanCopyFrom(TextureData source)
        {
            return format == source.format;
        }

        public void SetRect(TextureData tex, int x = 0, int y = 0)
        {
            if (CanCopyFrom(tex))
            {
                for (int i = 0; i < tex.w; i++)
                    for (int j = 0; j < tex.h; j++)
                        SetPixel(x + i, y + j, tex.GetPixel(i, j));
            }
            else if (format == ColorFormat.Direct)
            {
                Debug.Log("Can not copy data between textures with different color formats.");
            }
        }

        public Texture2D ToTexture(int palIndex, byte alpha = 230)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    if (format == ColorFormat.Index)
                    {
                        Color32 c;
                        int colorIndex = GetPixel(i, h - 1 - j);
                        c = FrameManager.Ins.OnlineFrame.colorPalette.ToColor(palIndex * 4 + colorIndex);
                        c.a = alpha;
                        tex.SetPixel(i, j, c);
                    }
                    else
                    {
                        int colorIndex = GetPixel(i, h - 1 - j);
                        Color32 c = Int2Color(colorIndex);
                        c.a = alpha;
                        tex.SetPixel(i, j, c);
                    }
                }
            return tex;
        }

        public void ToDirectFormat(int palIndex)
        {
            if (format == ColorFormat.Index)
            {
                int[] newData = new int[w * h];
                ColorFormat newFormat = ColorFormat.Direct;
                for (int i = 0; i < w; i++)
                    for (int j = 0; j < h; j++)
                    {
                        int colorIndex = GetPixel(i, j);
                        if (colorIndex > 0)
                        {
                            Color32 c = FrameManager.Ins.OnlineFrame.colorPalette.ToColor(palIndex * 4 + colorIndex);
                            SetPixel(i, j, Color2Int(c), w, h, newData, newFormat);
                        }
                        else
                            SetPixel(i, j, 0, w, h, newData, newFormat);
                    }
                Array.Copy(newData, data, w * h);
                format = newFormat;
            }
        }

        public bool IsIdentical(TextureData tex)
        {
            if (tex == null || tex.format != format || tex.w != w || tex.h != h)
                return false;

            for (int i = 0; i < Size; i++)
                if (data[i] != tex.data[i])
                    return false;

            return true;
        }

        public static int Color2Int(Color32 color)
        {
            return Misc.Color2Int(color);
        }

        public static Color32 Int2Color(int color)
        {
            if (color != 0)
                return Misc.Int2Color(color);
            else
                return FrameManager.Ins.OnlineFrame.colorPalette.ToColor(0);
        }
    }
}