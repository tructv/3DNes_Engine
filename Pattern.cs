using Nes3D.Utils;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Nes3D.Engine3D
{
    [Serializable]
    [ProtoContract(SkipConstructor = true)]
    public class Pattern
    {
        public static IntVector2 Unconstraint = new IntVector2(99, 99);

        // core data
        [NonSerialized]
        public List<Tile2D> tiles2D;

        [NonSerialized]
        [ProtoMember(1, AsReference = true)]
        public Tile2D[] tile2DGrid;

        [NonSerialized]
        public TextureData defTex;

        [ProtoMember(2)]
        public IntVector2 constraintSize { get; set; }

        [ProtoMember(3)]
        public IntVector2 tSize;
        [ProtoMember(4)]
        public bool permanent;

        //statistics
        [ProtoMember(5)]
        public int pixelCount;
        [ProtoMember(6)]
        public ColorSet colorFlags;
        [ProtoMember(7)]
        public DensityType tDensity; // density with tile as unit
        [ProtoMember(8)]
        public DensityType pDensity; // density with pixel as unit
        [ProtoMember(9)]
        public bool selfConnected;

        [NonSerialized]
        [ProtoMember(10, AsReference = true)]
        public List<Pattern3D> pattern3DList;

        // realtime supervision
        public int overlapCount;
        public int lastOverlapFrame;
        public int contactCount;
        public int lastContactFrame;
        public int startFrame;
        public int endFrame;

        [SerializeField]
        public Shape shape;

        public Pattern3D this[int index]
        {
            get { return pattern3DList[index]; }
        }


        public Pattern3D pattern3D { get { return pattern3DList[0]; } }

        public int count { get { return pattern3DList.Count; } }

#region shortcuts to access the first Shape3D fields

        public Geo3DType geo
        {
            get { return pattern3D.geo; }
            set { pattern3D.geo = value; }
        }

        public int sizeZ
        {
            get { return pattern3D.sizeZ; }
            set { pattern3D.sizeZ = value; }
        }

        public ZLayer layer
        {
            get { return pattern3D.layer; }
            set { pattern3D.layer = value; }
        }

        public bool antiShadow
        {
            get { return pattern3D.antiShadow; }
            set { pattern3D.antiShadow = value; }
        }

        public bool ui
        {
            get { return pattern3D.ui; }
            set { pattern3D.ui = value; }
        }

        public PVector3 pivot
        {
            get { return pattern3D.pivot; }
            set { pattern3D.pivot.content = value; }
        }

        public PVector3 rot
        {
            get { return pattern3D.rot; }
            set { pattern3D.rot.content = value; }
        }

        public PVector3 scale
        {
            get { return pattern3D.scale; }
            set { pattern3D.scale.content = value; }
        }

        public PVector3 offset
        {
            get { return pattern3D.offset; }
            set { pattern3D.offset.content = value; }
        }

#endregion

        public Tile3D Get3D(Tile tile)
        {
            return pattern3D.Get3D(tile);
        }

        public void Build()
        {
            for (int i = 0; i < pattern3DList.Count; i++)
                pattern3DList[i].Build(true);
        }

        public Pattern(Shape _shape)
        {
            tiles2D = new List<Tile2D>();
            pattern3DList = new List<Pattern3D>();
            pattern3DList.Add(new Pattern3D(this));
            shape = _shape;
        }

        public Pattern Clone(bool _permanent = true)
        {
            Pattern s = (Pattern)MemberwiseClone();
            s.shape = null;
            s.permanent = _permanent;
            s.tiles2D = new List<Tile2D>(tiles2D);

            s.pattern3DList = new List<Pattern3D>();
            s.pattern3DList.Add(pattern3D.Clone());
            s.pattern3DList[0].pattern = s;

            return s;
        }

        public void AssignFrom(Pattern sourcePattern, bool fromShape2Pattern = false)
        {
            int newCount = sourcePattern.count - count;
            for (int i = 0; i < newCount; i++)
                pattern3DList.Add(new Pattern3D(this));

            for (int i = 0; i < sourcePattern.count; i++)
                pattern3DList[i].AssignFrom(sourcePattern[i], fromShape2Pattern);

            constraintSize = sourcePattern.constraintSize;
            defTex = sourcePattern.defTex;
            startFrame = sourcePattern.startFrame;
            endFrame = sourcePattern.endFrame;
        }

        public bool IsPattern()
        {
            return shape == null;
        }

        public void Reset()
        {
            tiles2D.Clear();
            constraintSize = Unconstraint;
            colorFlags = ColorSet.TRANSPARENT;
            selfConnected = true;
            overlapCount = 0;
            lastOverlapFrame = 0;
            contactCount = 0;
            lastContactFrame = 0;
            startFrame = 0;

            defTex = null;

            foreach (Pattern3D p in pattern3DList)
                p.Reset();
        }

        public void Release()
        {
            foreach (var p in pattern3DList)
                p.Release();
        }

        public void Clone(Pattern3D p)
        {
            Pattern3D p1 = p.Clone();
            p1.offset.content += new Vector3(8, 8, 8);
            p1.InitData();
            p1.Build();
            pattern3DList.Add(p1);
        }

        public void Remove(Pattern3D p)
        {
            if (pattern3DList.Count > 1)
                pattern3DList.Remove(p);
            else
                PatternManager.Remove(this);
        }

        private void CreateDefaultTex()
        {
            defTex = new TextureData(tSize.x * 8, tSize.y * 8);
            for (int i = 0; i < defTex.w; i++)
                for (int j = 0; j < defTex.h; j++)
                {
                    var tile = tile2DGrid[(j >> 3) * tSize.x + (i >> 3)];
                    if (tile != null)
                        defTex.SetPixel(i, j, tile.GetColor(i & 7, j & 7));
                }
        }

        static List<Tile2D> checkList = new List<Tile2D>();
        public void InitData(Shape refShape)
        {
            foreach (Tile2D tile in tiles2D)
                if (permanent)
                    tile.patternSet.Add(this);
                else
                    tile.tempPatternSet.Add(this);

            tile2DGrid = new Tile2D[tSize.x * tSize.y];

            IntVector2 start = refShape.tStart;
            if (refShape.tSize > tSize)
                for (start.x = refShape.tStart.x; start.x <= refShape.tEnd.x - tSize.x; start.x++)
                    for (start.y = refShape.tStart.y; start.y <= refShape.tEnd.y - tSize.y; start.y++)
                        if (refShape.tileGrid[start.x + start.y * Frame.GridWidth].shape == refShape)
                        {
                            checkList.Clear();
                            for (int i = start.x; i < start.x + tSize.x; i++)
                                for (int j = start.y; j < start.y + tSize.y; j++)
                                {
                                    var tile = refShape.tileGrid[i + j * Frame.GridWidth];
                                    if (tile.shape == refShape && !checkList.Contains(tile.tile2D))
                                    {
                                        checkList.Add(tile.tile2D);
                                        if (checkList.Count == tiles2D.Count)
                                            goto StartPosDetermined;
                                    }
                                }
                        }
                            
StartPosDetermined:
            for (int i = 0; i < tSize.x; i++)
                for (int j = 0; j < tSize.y; j++)
                {
                    Tile tile = refShape.tileGrid[start.x + i + (start.y + j) * Frame.GridWidth];
                    if (tile.shapeId == refShape.id)
                        tile2DGrid[i + tSize.x * j] = tile.tile2D;
                    else
                        tile2DGrid[i + tSize.x * j] = null;
                }

            CreateDefaultTex();

            foreach (Pattern3D p in pattern3DList)
                p.InitData();
        }

        public void AfterDeser()
        {
            tiles2D = new List<Tile2D>();
            foreach (var t in tile2DGrid)
                if (t != null)
                {
                    t.AfterDeser();
                    if (!tiles2D.Contains(t))
                        tiles2D.Add(t);

                    if (permanent)
                        t.patternSet.Add(this);
                    else
                        t.tempPatternSet.Add(this);
                }

            CreateDefaultTex();

            foreach (Pattern3D p in pattern3DList)
            {
                p.pattern = this;
                p.AfterDeser();
            }

            if (permanent)
                PatternManager.patterns.Add(this);
            else
                PatternManager.tempPatterns.Add(this);
        }
    }

    public class PatternEquality : IEqualityComparer<Pattern>
    {
        public bool Equals(Pattern x, Pattern y)
        {
            if (x.tiles2D.Count != y.tiles2D.Count)
                return false;
            for (int i = 0; i < x.tiles2D.Count; i++)
                if (x.tiles2D[i] != y.tiles2D[i])
                    return false;
            return true;
        }

        public int GetHashCode(Pattern obj)
        {
            obj.tiles2D.Sort((x, y) => (x == y ? 0 : x > y ? 1 : -1));
            int hash = 0;
            foreach (Tile2D t in obj.tiles2D)
                hash = hash ^ t.hash;
            return hash;
        }
    }

    public class PatternCollection : KeyedCollection<Pattern, Pattern>
    {
        public PatternCollection(PatternEquality p) : base(p)
        {
        }

        protected override Pattern GetKeyForItem(Pattern item)
        {
            return item;
        }

        public bool TryGetValue(Pattern key, out Pattern item)
        {
            if (key == null)
                throw new ArgumentNullException("key");

            if (Dictionary != null)
                return Dictionary.TryGetValue(key, out item);

            foreach (Pattern itemInItems in Items)
            {
                Pattern keyInItems = GetKeyForItem(itemInItems);
                if (keyInItems != null && Comparer.Equals(key, keyInItems))
                {
                    item = itemInItems;
                    return true;
                }
            }

            item = null;
            return false;
        }
    }
}