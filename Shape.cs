using Nes3D.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public class ComparerByLocation : IComparer<Shape>
    {
        public int Compare(Shape x, Shape y)
        {
            int start = IntVector2.Comparer(x.tStart, y.tStart);
            if (start != 0)
                return start;
            return IntVector2.Comparer(x.tEnd, y.tEnd);
        }
    }

    public class ComparerByArea : IComparer<Shape>
    {
        public int Compare(Shape x, Shape y)
        {
            return x.tiles.Count.CompareTo(y.tiles.Count);
        }
    }

    public class ReverseComparerByArea : IComparer<Shape>
    {
        public int Compare(Shape x, Shape y)
        {
            return -x.tiles.Count.CompareTo(y.tiles.Count);
        }
    }

    sealed public partial class Shape
    {
        private const int BgMinHeight = 26;
        private const int BgMinWidth = 28;

        public static readonly int DensityCount = Enum.GetNames(typeof(DensityType)).Length;

        public static int maxId = 0;

        private static readonly int PoolIncremental = 2048;
        private static Stack<Shape> Pool;

        static Shape()
        {
            Pool = new Stack<Shape>();
        }

        public static Shape Get()
        {
            if (Pool.Count == 0)
            {
                for (int i = 0; i < PoolIncremental; i++)
                    Pool.Push(new Shape());
            }
            return Pool.Pop();
        }

        public static void Push(Shape s)
        {
            Pool.Push(s);
        }

        public void Release()
        {
            ReturnTiles();
            Push(this);
        }

        public float Distance(Shape s)
        {
            var dis = this[0].Pos - s[0].Pos;
            return dis.sqrMagnitude;
        }

        public Pattern pattern;
        public Pattern refPattern;

        public Pattern3D this[int index]
        {
            get { return pattern[index]; }
        }

        public Pattern3D shape3D
        {
            get { return this[0]; }
        }

        //shortcuts
        public List<Tile2D> tiles2D
        {
            get { return pattern.tiles2D; }

            set { pattern.tiles2D = value; }
        }

        public ColorSet colorFlags
        {
            get { return pattern.colorFlags; }

            set { pattern.colorFlags = value; }
        }

        public IntVector2 tSize
        {
            get { return pattern.tSize; }

            set { pattern.tSize = value; }
        }

        public Geo3DType geo
        {
            get { return pattern.geo; }

            set { pattern.geo = value; }
        }

        public int sizeZ
        {
            get { return pattern.sizeZ; }

            set { pattern.sizeZ = value; }
        }

        public ZLayer layer
        {
            get { return pattern.layer; }

            set { pattern.layer = value; }
        }

        public bool selfConnected
        {
            get { return pattern.selfConnected; }

            set { pattern.selfConnected = value; }
        }

        public DensityType tDensity
        {
            get { return pattern.tDensity; }

            set { pattern.tDensity = value; }
        }

        public DensityType pDensity
        {
            get { return pattern.pDensity; }

            set { pattern.pDensity = value; }
        }

        public bool antiShadow
        {
            get { return pattern.antiShadow; }

            set { pattern.antiShadow = value; }
        }

        public int pixelCount
        {
            get { return pattern.pixelCount; }

            set { pattern.pixelCount = value; }
        }

        public PVector3 pivot
        {
            get { return pattern.pivot; }

            set { pattern.pivot.content = value; }
        }

        public PVector3 rot
        {
            get { return pattern.rot; }

            set { pattern.rot.content = value; }
        }

        public PVector3 scale
        {
            get { return pattern.scale; }

            set { pattern.scale.content = value; }
        }

        public IntVector2 constraintSize
        {
            get { return pattern.constraintSize; }
            set { pattern.constraintSize = value; }
        }

        public int InsCount
        {
            //get { return Mathf.Min(pattern.count, refPattern.count); }
            get { return refPattern.count; }
        }


        public int id;
        public List<Tile> tiles;
        public Tile[] tileGrid;
        public bool hiden;
        public int palIndex;
        public IntVector2 tStart;
        public IntVector2 tEnd;
        public IntVector2 pStart, pEnd;
        public bool bg;

        IntVector2 optimizedSize;

        public void PatternBinding(Pattern _pattern)
        {
            refPattern = _pattern;
            pattern.AssignFrom(refPattern);
            center = (Vector3)(pStart + pEnd) / 2;
        }

        public void CalculatePos()
        {
            foreach (Pattern3D p in pattern.pattern3DList)
                p.CalculatePos();
        }

        public Vector3 center;

        public void Reset(Tile[] _tileGrid, bool _background = true)
        {
            hiden = false;
            pattern.Reset();
            tiles.Clear();
            pixelCount = 0;
            tStart = tEnd = tSize = new IntVector2(0, 0);
            tileGrid = _tileGrid;
            bg = _background;
            refPattern = pattern;
        }

        public Shape()
        {
            id = maxId++;
            pattern = new Pattern(this);
            tiles = new List<Tile>();
            refPattern = pattern;
        }

        public void ReturnTiles()
        {
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i].shapeId == id)
                    tiles[i].Reset();
        }

        public void RunUpdateScript()
        {
            for (int i = 0; i < InsCount; i++)
                this[i].RunUpdateScript();
        }

        public void AddTile(Tile tile, bool auto = true)
        {
            if (!auto && tiles.Contains(tile))
                return;
            if (!bg)
            {
                tileGrid[tile.index] = tile;
                hiden |= tile.hidden;
            }
            tiles.Add(tile);
            tile.shapeId = id;
            tile.shape = this;

            pixelCount += tile.tile2D.pixelCount;

            if (tiles.Count == 1)
            {
                palIndex = tile.palette + (bg ? 0 : 4);
                tStart = tEnd = tile.tPos;
                tSize = new IntVector2(1, 1);
                pStart.x = tile.x + tile.tile2D.start.x;
                pEnd.x = tile.x + tile.tile2D.end.x;
                pStart.y = tile.y + tile.tile2D.start.y;
                pEnd.y = tile.y + tile.tile2D.end.y;
            }
            else
            {
                tStart.x = Mathf.Min(tStart.x, tile.tPos.x);
                tStart.y = Mathf.Min(tStart.y, tile.tPos.y);
                tEnd.x = Mathf.Max(tEnd.x, tile.tPos.x);
                tEnd.y = Mathf.Max(tEnd.y, tile.tPos.y);
                tSize = tEnd - tStart + new IntVector2(1, 1);

                pStart.x = Math.Min(pStart.x, tile.x + tile.tile2D.start.x);
                pStart.y = Math.Min(pStart.y, tile.y + tile.tile2D.start.y);
                pEnd.x = Math.Max(pEnd.x, tile.x + tile.tile2D.end.x);
                pEnd.y = Math.Max(pEnd.y, tile.y + tile.tile2D.end.y);
            }

            if (!tiles2D.Contains(tile.tile2D))
                tiles2D.Add(tile.tile2D);

            colorFlags |= tile.tile2D.colorFlags;
            selfConnected &= tile.tile2D.selfConnected;

            if (tile.tile2D.geo != Geo2DType.FULL_RECT)
                geo = Geo3DType.Default;
        }

        public IntVector2 UpdateSize(Tile tile)
        {
            if (tiles.Count > 0)
            {
                IntVector2 start, end;
                start.x = Mathf.Min(tStart.x, tile.tPos.x);
                start.y = Mathf.Min(tStart.y, tile.tPos.y);
                end.x = Mathf.Max(tEnd.x, tile.tPos.x);
                end.y = Mathf.Max(tEnd.y, tile.tPos.y);
                return end - start + new IntVector2(1, 1);
            }
            else
                return new IntVector2(1, 1);
        }

        public bool IsContactWith(Shape other)
        {
            int ratio = 2;
            int low = other.pEnd.y - 3;
            int high = other.pEnd.y + 2;
            int count = 0;
            for (int i = other.pStart.x; i <= other.pEnd.x; i++)
            {
                if (ActiveAt(new IntVector2(i, low)))
                    return false;
                if (ActiveAt(new IntVector2(i, high)))
                    count++;
            }

            return count * ratio > other.pEnd.x - other.pStart.x;
        }

        public static bool Overlaps(Shape s1, Shape s2)
        {
            if (s1.tiles.Count >= s2.tiles.Count)
                return s1.OverlapsWith(s2);
            else
                return s2.OverlapsWith(s1);
        }

        public bool OverlapsWith(Shape other)
        {
            IntVector2 start = other.pStart + new IntVector2(3, 3);
            IntVector2 end = other.pEnd - new IntVector2(3, 3);
            Rect rect1 = new Rect(pStart, pEnd - pStart);
            Rect rect2 = new Rect(start, end - start);
            if (rect1.Overlaps(rect2))
                return ActiveAt(new IntVector2((start.x + end.x) >> 1, end.y)) || ActiveAt(new IntVector2((start.x + end.x) >> 1, start.y))
                        || ActiveAt(new IntVector2(start.x, (start.y + end.y) >> 1)) || ActiveAt(new IntVector2(end.x, (start.y + end.y) >> 1))
                        || ActiveAt(new IntVector2(start.x, start.y)) || ActiveAt(new IntVector2(start.x, end.y))
                        || ActiveAt(new IntVector2(end.x, start.y)) || ActiveAt(new IntVector2(end.x, end.y))
                        || ActiveAt(new IntVector2((start.x + end.x) >> 1, (start.y + end.y) >> 1));
            return false;
        }

        public bool Contains(Tile tile)
        {
            IntVector2 pos = tile.tPos;
            if (ContainsInTile(pos))
            {
                int index = tile.index - 1;
                for (int i = tStart.x; i < pos.x; i++)
                    if (tileGrid[index--].shapeId == id)
                        goto l1;

                return false;

                l1: index = tile.index + 1;
                for (int i = pos.x + 1; i <= tEnd.x; i++)
                    if (tileGrid[index++].shapeId == id)
                        goto l2;

                return false;

                l2: index = tile.index - Frame.GridWidth;
                for (int i = tStart.y; i < pos.y; i++, index -= Frame.GridWidth)
                    if (tileGrid[index].shapeId == id)
                        goto l3;

                return false;

                l3: index = tile.index + Frame.GridWidth;
                for (int i = pos.y + 1; i <= tEnd.y; i++, index += Frame.GridWidth)
                    if (tileGrid[index].shapeId == id)
                        goto l4;

                return false;

                l4: return true;
            }
            return false;
        }

        public bool ContainsInTile(IntVector2 pos)
        {
            return (tStart.x < pos.x) && (tStart.y < pos.y) && (tEnd.x > pos.x) && (tEnd.y > pos.y);
        }

        public bool ActiveAt(IntVector2 pos)
        {
            return (GetColor(pos.x, pos.y) > 0);
        }

        private byte GetColor(int x, int y)
        {
            int fineX = (x - tiles[0].x) & 7;
            int fineY = (y - tiles[0].y) & 7;
            IntVector2 gridPos = Converter.World2Grid(x - fineX, y - fineY);
            Tile tile = tileGrid[gridPos.y * Frame.GridWidth + gridPos.x];
            if (tile.shapeId != id)
                return 0;
            return tile.GetColor(fineX, fineY);
        }

        public bool IsFinished()
        {
            return Inside || IsBig();
        }

        public bool Inside
        {
            get { return ((pStart.x > FrameManager.Ins.OfflineFrame.mask.x) && (pEnd.x + FrameManager.Ins.OfflineFrame.mask.x < 255) && (pStart.y > FrameManager.Ins.OfflineFrame.mask.y) && (pEnd.y + FrameManager.Ins.OfflineFrame.mask.y < 239)); }
        }

        public bool AtBorder
        {
            get { return !Inside; }
        }

        public bool IsBig()
        {
            return (tSize.x >= BgMinWidth) || (tSize.y >= BgMinHeight);
        }

        public bool IsRectSolid()
        {
            return tiles.Count == tSize.x * tSize.y;
        }

        public bool IsSolid()
        {
            Tile tile;
            IntVector2 pos = new IntVector2();
            for (pos.x = tStart.x + 1; pos.x < tEnd.x; pos.x++)
                for (pos.y = tStart.y + 1; pos.y < tEnd.y; pos.y++)
                    if ((tile = tileGrid[pos.y * Frame.GridWidth + pos.x]).shapeId != id && Contains(tile))
                        return false;
            return true;
        }

        static private Queue<Tile> _tileQueue = new Queue<Tile>();

        private void Expand()
        {
            Tile tile1;
            _tileQueue.Clear();
            foreach (Tile tile in tiles)
                _tileQueue.Enqueue(tile);

            while (_tileQueue.Count > 0)
            {
                Tile tile = _tileQueue.Dequeue();
                for (int i = 0; i < Converter.absDirs.Length; i++)
                {
                    if ((tile1 = tileGrid[tile.index + Converter.absDirs[i]]).shapeId == 0)
                        if (tiles2D.Contains(tile1.tile2D))
                        {
                            AddTile(tile1);
                            _tileQueue.Enqueue(tile1);
                        }
                }
            }
        }

        public bool Process(bool preCheck = true)
        {
            IntVector2 pos = new IntVector2();
            if (preCheck && IsBig() && !IsRectSolid())
            {
                // cut into smaller shapes
                Shape shape = Get();
                shape.Reset(tileGrid, bg);

                if (tSize.x >= tSize.y)
                {
                    bool begin = false;
                    for (pos.y = tStart.y; pos.y <= tEnd.y; pos.y++)
                    {
                        bool solid = true;
                        for (pos.x = tStart.x; pos.x <= tEnd.x; pos.x++)
                        {
                            if (tileGrid[pos.y * Frame.GridWidth + pos.x].shapeId != id)
                            {
                                solid = false;
                                break;
                            }
                        }
                        if (solid)
                        {
                            for (pos.x = tStart.x; pos.x <= tEnd.x; pos.x++)
                                shape.AddTile(tileGrid[pos.y * Frame.GridWidth + pos.x]);
                            begin = true;
                        }
                        else if (begin)
                            break;
                    }
                }
                else
                {
                    bool begin = false;
                    for (pos.x = tStart.x; pos.x <= tEnd.x; pos.x++)
                    {
                        bool solid = true;
                        for (pos.y = tStart.y; pos.y <= tEnd.y; pos.y++)
                        {
                            if (tileGrid[pos.y * Frame.GridWidth + pos.x].shapeId != id)
                            {
                                solid = false;
                                break;
                            }
                        }
                        if (solid)
                        {
                            for (pos.y = tStart.y; pos.y <= tEnd.y; pos.y++)
                                shape.AddTile(tileGrid[pos.y * Frame.GridWidth + pos.x]);
                            begin = true;
                        }
                        else if (begin)
                            break;
                    }
                }

                if (shape.tiles.Count > 0)
                {
                    Release();
                    shape.Expand();
                    if (shape.Process(false))
                        FrameManager.Ins.OfflineFrame.AddShape(shape);
                    return false;
                }
                else
                    Process(false);
            }

            Pattern savePattern;

            if (IsFinished())
            {
                if (PatternManager.patterns.TryGetValue(pattern, out savePattern))
                    PatternBinding(savePattern);
                else
                    CreateAndBindNewPattern();
            }
            else  // maybe not finished shape
            {
                if (PatternManager.tempPatterns.TryGetValue(pattern, out savePattern))
                {
                    PatternBinding(savePattern);
                }
                else
                {
                    // partial temp shape
                    foreach (Pattern s in PatternManager.tempPatterns)
                        if (s.tiles2D.Contains(tiles2D))
                        {
                            PatternBinding(s);
                            return true;
                        }

                    //new pattern
                    CreateAndBindNewPattern(false);
                }
            }

            return true;
        }

        public Pattern ClonePattern(bool permanent = true)
        {
            return pattern.Clone(permanent);
        }

        //local variable
        private static List<Pattern> temp = new List<Pattern>();
        public Pattern CreateNewPattern(bool permanent = true, bool autoDetect = true)
        {
            Pattern newPattern = ClonePattern(permanent);
            if (autoDetect)
                newPattern.tSize = optimizedSize;
            newPattern.InitData(this);
            newPattern.pattern3D.Build();

            PatternManager.Add(newPattern);
            return newPattern;
        }

        void CheckCycle()
        {
            int count = 0;
            for (int j = tStart.y; j <= tEnd.y; j++)
            {
                int index = tStart.x + j * Frame.GridWidth - 1;
                for (int i = tStart.x; i <= tEnd.x; i++)
                {
                    index++;
                    var tile = tileGrid[index];
                    if (tile.shape != this)
                        continue;
                    var tile2D = tile.tile2D;
                    var next = tileGrid[index + 1];
                    if (hCycle[0] != -1 && (i + 1 <= tEnd.x) && next.shape == this)
                        if (tile2D != next.tile2D)
                        {
                            hCycle[0] = -1;
                            count++;
                        }
                        else
                            hCycle[0] = 1;
                    next = tileGrid[index + Frame.GridWidth];
                    if (vCycle[0] != -1 && (j + 1 <= tEnd.y) && next.shape == this)
                        if (tile2D != next.tile2D)
                        {
                            vCycle[0] = -1;
                            count++;
                        }
                        else
                            vCycle[0] = 1;
                    if (i + 2 <= Frame.GridWidth)
                    {
                        next = tileGrid[index + 2];
                        if (hCycle[1] != -1 && (i + 2 <= tEnd.x) && next.shape == this)
                            if (tile2D != next.tile2D)
                            {
                                hCycle[1] = -1;
                                count++;
                            }
                            else
                                hCycle[1] = 1;
                    }

                    if (j + 2 <= Frame.Height)
                    {
                        next = tileGrid[index + 2 * Frame.GridWidth];
                        if (vCycle[1] != -1 && (j + 2 <= tEnd.y) && next.shape == this)
                            if (tile2D != next.tile2D)
                            {
                                vCycle[1] = -1;
                                count++;
                            }
                            else
                                vCycle[1] = 1;
                    }
                    if (count == 4)
                        return;
                }
            }
        }

        // local variables
        static int[] vCycle = new int[2];
        static int[] hCycle = new int[2];
        private void OptimizeSize()
        {
            optimizedSize = tSize;
            if (geo == Geo3DType.Default || geo == Geo3DType.CUBE)
                if (tiles2D.Count <= 4 && tiles.Count > 1)
                {
                    for (int i = 0; i < hCycle.Length; i++)
                        hCycle[i] = vCycle[i] = 0;

                    CheckCycle();

                    for (int i = 0; i < hCycle.Length; i++)
                        if (hCycle[i] == 1)
                        {
                            optimizedSize.x = i + 1;
                            break;
                        }
                    for (int i = 0; i < hCycle.Length; i++)
                        if (vCycle[i] == 1)
                        {
                            optimizedSize.y = i + 1;
                            break;
                        }
                }
        }

        public Pattern CreateAndBindNewPattern(bool permanent = true, bool autoDetect = true)
        {
            DetectSelfConnection();
            if (autoDetect)
                DetectParameters();
            refPattern = CreateNewPattern(permanent, autoDetect);
            PatternBinding(refPattern);

            return refPattern;
        }

        private void DetectGeo()
        {
            if (geo == Geo3DType.Default)
                if (tiles.Count == 1 && tiles[0].IsCHR)
                {
                    geo = Geo3DType.Default;
                    constraintSize = new IntVector2(1, 1);
                }
                else if (bg && selfConnected)
                {
                    if (pEnd.y - pStart.y >= pEnd.x - pStart.x)
                        geo = Geo3DType.VCYLINDER;
                    else
                        geo = Geo3DType.HCYLINDER;
                }
        }

        private void DetectDensity()
        {
            if (tiles.Count == tSize.x * tSize.y)
                tDensity = DensityType.SOLID;
            else
                tDensity = (DensityType)Mathf.Ceil(tiles.Count * (DensityCount - 2) / (float)(tSize.x * tSize.y));

            if (pixelCount == tSize.x * tSize.y * 64)
                pDensity = DensityType.SOLID;
            else
            {
                pDensity = (DensityType)Mathf.Ceil(pixelCount * (DensityCount - 2) / (float)(tSize.x * tSize.y * 64));
            }
        }

        private void DetectDepth()
        {
            if (geo == Geo3DType.Default)
            {
                sizeZ = Mathf.Min(4, Mathf.Min(tSize.x, tSize.y)) * 2;
            }
            else
                sizeZ = Mathf.Min(4, Mathf.Min(tSize.x, tSize.y)) * 8;
        }

        private static HashSet<Tile> data = new HashSet<Tile>();
        private static Queue<Tile> queue = new Queue<Tile>();

        private void DetectSelfConnection()
        {
            if (selfConnected == false)
                return;

            data.Clear();
            for (int i = 0; i < tiles.Count; i++)
                data.Add(tiles[i]);
            queue.Clear();

            Tile tile = tiles[0];
            Tile tile1;
            data.Remove(tile);
            queue.Enqueue(tile);
            int count = 1;

            while (queue.Count > 0)
            {
                tile = queue.Dequeue();
                for (int i = 0; i < Converter.absDirs.Length; i++)
                {
                    int newIndex = tile.index + Converter.absDirs[i];
                    tile1 = tileGrid[newIndex];
                    if (data.Contains(tile1) && tile.ReachedBorder(i) && tile1.ReachedBorder(3 - i))
                    {
                        data.Remove(tile1);
                        queue.Enqueue(tile1);
                        count++;
                    }
                }
            }
            selfConnected = (count == tiles.Count);
        }

        public bool Completed
        {
            get { return tiles2D.Count == refPattern.tiles2D.Count; }
        }

        private void DetectParameters()
        {
            DetectDensity();
            DetectGeo();
            DetectDepth();
            OptimizeSize();
        }
    }
}