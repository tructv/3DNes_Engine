using Nes3D.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public enum Geo2DType
    {
        UNIDENTIFIED,
        CHAR,
        RECT, 
        FULL_RECT
    }; 

    public enum DensityType { EMPTY, LOW, AVERAGE, MEDIUM, HIGH, SOLID };

    public class RawTile2D
    {
        public long low;
        public long high;
 
		public bool flipX;
        public bool flipY;
        public int flip
        {
            get
            {
                return Convert.ToInt32(flipX) | (Convert.ToInt32(flipY) << 1);
            }
            private set
            {
                flipX = (value & 1) == 1;
                flipY = (value & 2) == 2;
            }
        }

        public RawTile2D() : base()
        {
        }

        public RawTile2D(RawTile2D raw) : base()
        {
            low = raw.low;
            high = raw.high;
            flipX = raw.flipX;
            flipY = raw.flipY;
        }

        public void InitData(ref TileData item)
        {
            low = item.low;
            high = item.high;
            flipX = item.flipX;
            flipY = item.flipY;
        }

        public override int GetHashCode()
        {
            int result = 0;

            result ^= (int)(low >> 32);
            result ^= (int)(low);
            result ^= (int)(high >> 32);
            result ^= (int)(high);
            result ^= flip;

            return result;
        }

        public override bool Equals(System.Object obj)
        {
            RawTile2D raw = obj as RawTile2D;
            return raw != null && low == raw.low && high == raw.high && flip == raw.flip;
        }

        public static bool operator >(RawTile2D x, RawTile2D y)
        {
            return x.high > y.high || (x.high == y.high && x.low > y.low) || (x.high == y.high && x.low == y.low && x.flip > y.flip);
        }

        public static bool operator <(RawTile2D x, RawTile2D y)
        {
            return x.high < y.high || (x.high == y.high && x.low < y.low) || (x.high == y.high && x.low == y.low && x.flip < y.flip);
        }
    }


    public class Tile2D : IComparable<Tile2D>
    {
        public const int SIZE = 8;

        public RawTile2D rawData;

        public bool flipX { get { return rawData.flipX; } }
        public bool flipY { get { return rawData.flipY; } }

        public bool IsNull { get { return pixelCount == -1; } }

        public override int GetHashCode()
        {
            return hash;
        }

        public static bool operator >(Tile2D x, Tile2D y)
        {
            return x.rawData > y.rawData;
        }

        public static bool operator <(Tile2D x, Tile2D y)
        {
            return x.rawData < y.rawData;
        }
        public int hash;

        public HashSet<Pattern> patternSet;
        public HashSet<Pattern> tempPatternSet;

        private byte[] border = new byte[32];

        public int pixelCount;

        public ColorSet colorFlags;
        public int colorCount;

        public IntVector2 size;
        public IntVector2 start;
        public IntVector2 end;
        public Geo2DType geo;
        public DensityType density;
        public bool selfConnected;

        public bool[] reachedDir = new bool[4];
        public byte borderCount;

        public bool IsPermanent
        {
            get { return patternSet.Count > 0; }
        }

        public bool IsTemp
        {
            get { return tempPatternSet.Count > 0; }
        }

        public bool IsNew
        {
            get { return patternSet.Count == 0 && tempPatternSet.Count == 0; }
        }

        public bool IsEmpty
        {
            get { return pixelCount == 0; }
        }

        public bool IsSolid
        {
            get { return pixelCount >= 61 && colorCount == 1; }
        }

        private bool IsCHR
        {
            get { return colorCount == 1 && density <= DensityType.MEDIUM && (borderCount <= 2 || !selfConnected && borderCount == 4); }
        }

        private Tile2D()
        {
            patternSet = new HashSet<Pattern>();
            tempPatternSet = new HashSet<Pattern>();
        }

        public Tile2D(RawTile2D _rawData)
        {
            rawData = new RawTile2D(_rawData);
            hash = rawData.GetHashCode();
            patternSet = new HashSet<Pattern>();
            tempPatternSet = new HashSet<Pattern>();
            LoadRaw();

            PatternManager.tiles.Add(this);
        }

        public void LoadRaw()
        {
            start = new IntVector2(7, 7);
            end = new IntVector2(0, 0);
            pixelCount = 0;

            for (int x = 0; x < SIZE; x++)
                for (int y = 0; y < SIZE; y++)
                {
                    int color = data[y << 3 | x] = GetColor(x, y);
                    if (color != 0)
                    {
                        pixelCount++;
                        start.x = Mathf.Min(start.x, x);
                        start.y = Mathf.Min(start.y, y);
                        end.x = Mathf.Max(end.x, x);
                        end.y = Mathf.Max(end.y, y);

                        colorFlags |= (ColorSet)(1 << color);
                    }
                }
            size = end - start + new IntVector2(1, 1);
            colorCount = Misc.FlagCount(colorFlags);

            CreateDirs();
            CreateBorder();
            DetectDensity();
            DetectSelfConnection();
            DetectGeo();
        }

        public byte GetColor(int x, int y)
        {
            x = !flipX ? x : ~x & 7;
            y = !flipY ? y : ~y & 7;
            int index = (y << 3) | (~x & 7);
            return (byte)((((rawData.high >> index) & 1) << 1) |
                    ((rawData.low >> index) & 1));
        }

        public bool Active(int x, int y)
        {
            return GetColor(x, y) != 0;
        }

        public bool ColorConnected(Tile2D tile)
        {
            return (colorFlags & tile.colorFlags) != 0;
        }

        public bool BorderConnected(int dir, Tile2D tile)
        {
            int segment = dir * 8;
            int segment1 = (3 - dir) * 8;
            for (int i = 0; i < 8; i++)
                if (border[segment + i] != 0 && tile.border[segment1 + i] != 0)
                    return true;
            return false;
        }

        private void CreateDirs()
        {
            reachedDir[0] = (end.x == 7);
            reachedDir[1] = (end.y == 7);
            reachedDir[2] = (start.y == 0);
            reachedDir[3] = (start.x == 0);

            borderCount = 0;
            for (int i = 0; i < 4; i++)
                if (reachedDir[i])
                    borderCount++;
        }

        private void CreateBorder()
        {
            for (int i = 0; i < SIZE; i++)
            {
                border[i] = GetColor(7, i);
                border[8 + i] = GetColor(i, 7);
                border[16 + i] = GetColor(i, 0);
                border[24 + i] = GetColor(0, i);
            }
        }

        private static Queue<IntVector2> queue = new Queue<IntVector2>();
        private static byte[] data = new byte[SIZE * SIZE];

        private void DetectGeo()
        {
            if (pixelCount >= 60)
                geo = Geo2DType.FULL_RECT;
            else if (IsCHR)
                geo = Geo2DType.CHAR;
            else if (pixelCount == size.x * size.y)
                geo = Geo2DType.RECT;
            else
                geo = Geo2DType.UNIDENTIFIED;
        }

        private void DetectSelfConnection()
        {
            IntVector2 pos = new IntVector2();
            IntVector2 pos1 = new IntVector2();

            byte area = 0;
            pos.y = start.y;
            for (int i = start.x; i <= end.x; i++)
                if (Active(i, pos.y))
                {
                    pos.x = i;
                    break;
                }

            data[pos.y << 3 | pos.x] = 0;
            area++;
            queue.Enqueue(pos);

            while (queue.Count > 0)
            {
                pos = queue.Dequeue();
                for (int i = 0; i < 4; i++)
                {
                    pos1 = pos + Converter.dirs[i];
                    if ((pos1.x >= 0) && (pos1.x < SIZE) && (pos1.y >= 0) && (pos1.y < SIZE))
                        if (data[pos1.y << 3 | pos1.x] > 0)
                        {
                            data[pos1.y << 3 | pos1.x] = 0;
                            area++;
                            queue.Enqueue(pos1);
                        }
                }
            }

            selfConnected = pixelCount == area;
        }

        private void DetectDensity()
        {
            if (pixelCount == 64)
                density = DensityType.SOLID;
            else if (pixelCount >= 48)
                density = DensityType.HIGH;
            else if (pixelCount >= 32)
                density = DensityType.MEDIUM;
            else if (pixelCount >= 16)
                density = DensityType.AVERAGE;
            else if (pixelCount > 0)
                density = DensityType.LOW;
            else
                density = DensityType.EMPTY;
        }

        public int CompareTo(Tile2D other)
        {
            return rawData > other.rawData ? 1 : rawData < other.rawData ? -1 : 0;
        }
    }
}