using Nes3D.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public enum ZLayer { UNIDENTIFIED, UI, L1, L2, L3, L4, COUNT };
    public enum Alignment { FRONT, CENTER, BACK, COUNT };
    public enum RenderAlgo { Greedy, Marching };
    public enum Geo3DType
    {
        Default,
        HCYLINDER,
        VCYLINDER,
        CUBE,
        HALFHCYLINDER,
        HALFVCYLINDER,
    };

    sealed public partial class Pattern3D
    {
        public enum State
        {
            Initialized,
            GeoCreated,
            DataCreated,
            Ready,
            Released
        }

        public const int MAX_ZSIZE = 96;

        public const float DefaultSpeed = 0;
        public const float DefaultOffset = Mathf.PI / 2;

        #region shortcuts
        public List<Tile2D> tiles2D
        {
            get { return pattern.tiles2D; }
        }

        public IntVector2 tSize
        {
            get { return pattern.tSize; }
        }

        private Tile2D[] tile2DGrid
        {
            get { return pattern.tile2DGrid; }
        }

        public RenderAlgo Mode
        {
            get
            {
                if (SettingManager.Ins.RenderMode == RenderAlgo.Greedy || fn)
                    return RenderAlgo.Greedy;
                return RenderAlgo.Marching;
            }
        }

        // shortcuts
        public TextureData defTex { get { return pattern?.defTex; } }

        public Shape shape
        {
            get { return pattern.shape; }
        }

        public Pattern refPattern
        {
            get { return refPattern3D.pattern; }
        }

        public int index
        {
            get { return pattern.pattern3DList.FindIndex(x => x == this); }
        }

        public Pattern3D refPattern3D;

        public bool ui
        {
            get { return layer == ZLayer.UI; }
            set { if (value) layer = ZLayer.UI; }
        }

        public int constraintWidth
        {
            get { return constraintSize.x; }
            set { constraintSize = new IntVector2(value, constraintSize.y); }
        }

        public int constraintHeight
        {
            get { return constraintSize.y; }
            set { constraintSize = new IntVector2(constraintSize.x, value); }
        }

        public IntVector2 constraintSize
        {
            get { return pattern.constraintSize; }
            set { pattern.constraintSize = value; }
        }

        public Atlas.Slot texSlot
        {
            get { return Atlas.GetSlot(tex); }
        }
        #endregion

        private Vector3 pos;
        public Vector3 Pos {
            get { return pos; }
            private set { pos = value; }
        }

        private Dictionary<Tile2D, Tile3D> tiles3D;
        private Tile3D[] tile3DGrid;
        public int sizeZ;
        public Geo3DType geo;
        public bool antiShadow;
        public bool flip;
        public bool solid;
        public bool fullWarp;
        public ZLayer layer;
        public PVector3 scale;
        public PVector3 rot;
        public PVector3 offset;
        public PVector3 pivot;
        public float alpha;
        public bool fn; // force render in normal mode
        public PVector3 deltaAmp;
        public PVector3 deltaPivot;
        public PMatrix4x4 deltaSpeed;
        public PMatrix4x4 deltaOffset;
        public bool enable;

        public Pattern pattern;
        public TextureData tex;

        public List<Script> linkedScript;

        private string tagStr;
        private List<string> tags;
        public string TagStr
        {
            get {
                return tagStr;
            }
            set
            {
                tagStr = value != null ? value : string.Empty;
                if (tags == null)
                    tags = new List<string>();
                else
                    tags.Clear();
                foreach (var s in tagStr.Split(Script.separatos))
                {
                    var s1 = s.Trim();
                    if (!s1.Equals(string.Empty))
                        tags.Add(s.Trim());
                }

                LinkToScripts();
            }
        }

        public bool ContainsTag(string value)
        {
            return tags.Contains(value);
        }

        private volatile State state;

        public void CalculatePos()
        {
            Pos = GetPos();
        }

        public bool animEnable { get { return deltaAmp.x != 0 || deltaAmp.y != 0 || deltaAmp.z != 0; } }

        public Pattern3D(Pattern _pattern)
        {
            pattern = _pattern;
            state = State.Initialized;

            scale = new PVector3();
            offset = new PVector3();
            rot = new PVector3();
            pivot = new PVector3();

            deltaSpeed = new PMatrix4x4(Matrix4x4.zero);
            deltaOffset = new PMatrix4x4(Matrix4x4.zero);
            deltaAmp = new PVector3(Vector3.zero);
            deltaPivot = new PVector3(Vector3.zero);
            tagStr = string.Empty;
        }

        public Pattern3D Clone()
        {
            Pattern3D p = (Pattern3D)MemberwiseClone();
            p.tiles3D = null;

            p.offset = new PVector3(offset);
            p.scale = new PVector3(scale);
            p.rot = new PVector3(rot);
            p.pivot = new PVector3(pivot);

            p.deltaAmp = new PVector3(deltaAmp);
            p.deltaPivot = new PVector3(deltaPivot);
            p.deltaSpeed = new PMatrix4x4(deltaSpeed);
            p.deltaOffset = new PMatrix4x4(deltaOffset);

            if (tex != null)
                p.tex = new TextureData(tex);

            p.linkedScript = new List<Script>();
            p.tags = new List<string>();
            p.TagStr = TagStr;
            return p;
        }

        private Tile3D Get3D(Tile2D tile)
        {
            Tile3D tile3D;
            tiles3D.TryGetValue(tile, out tile3D);
            return tile3D;
        }

        public Tile3D Get3D(Tile tile)
        {
            return Get3D(tile.tile2D);
        }

        public void AssignFrom(Pattern3D pattern, bool fromShape2Pattern = false)
        {
            constraintSize = pattern.constraintSize;
            geo = pattern.geo;
            sizeZ = pattern.sizeZ;

            layer = pattern.layer;

            offset.content = pattern.offset;
            scale.content = pattern.scale;
            rot.content = pattern.rot;
            pivot.content = pattern.pivot;

            deltaAmp.content = pattern.deltaAmp;
            deltaOffset.target = pattern.deltaOffset;
            deltaSpeed.target = pattern.deltaSpeed;
            deltaPivot.content = pattern.deltaPivot;

            tex = pattern.tex;
            alpha = pattern.alpha;
            fn = pattern.fn;
            enable = pattern.enable;

            antiShadow = pattern.antiShadow;
            flip = pattern.flip;
            solid = pattern.solid;
            fullWarp = pattern.fullWarp;

            if (fromShape2Pattern)
            {
                TagStr = pattern.TagStr;
                sizeZ = Mathf.Min(pattern.sizeZ, MAX_ZSIZE);
                Build();
            }
            else
            {
                refPattern3D = pattern;
                tagStr = pattern.tagStr;
                tags = pattern.tags;
                linkedScript = pattern.linkedScript;
            }
        }

        public void Reset()
        {
            refPattern3D = this;

            geo = Geo3DType.CUBE;
            layer = ZLayer.UNIDENTIFIED;

            scale.content = Vector3.one;
            offset.content = Vector3.zero;
            rot.content = Vector3.zero;
            pivot.content = Vector3.one / 2;

            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    deltaSpeed[i, j] = DefaultSpeed;
                    deltaOffset[i, j] = DefaultOffset;
                }
            deltaAmp.content = Vector3.zero;
            deltaPivot.content = Vector3.one / 2f;
            alpha = 1;
            tex = null;
            fn = false;
            enable = true;

            antiShadow = false;
            flip = false;
            solid = false;
            fullWarp = false;

            tagStr = string.Empty;
            linkedScript = null;
            tags = null;
        }

        public void InitData()
        {
            if (tex == null)
                tex = new TextureData(defTex);

            tiles3D = new Dictionary<Tile2D, Tile3D>();
            tile3DGrid = new Tile3D[tSize.x * tSize.y];

            foreach (Tile2D tile in tiles2D)
                tiles3D[tile] = new Tile3D(this, pattern.permanent);

            int index = 0;
            for (int j = 0; j < tSize.y; j++)
                for (int i = 0; i < tSize.x; i++)
                {
                    if (tile2DGrid[index] != null)
                    {
                        tile3DGrid[index] = Get3D(tile2DGrid[index]);
                        tile3DGrid[index].SetPos(new IntVector2(i * 8, j * 8));
                    }
                    else
                        tile3DGrid[index] = null;

                    index++;
                }
        }

        public void AdjustPattern()
        {
            refPattern3D?.AssignFrom(this, true);
        }

        public void Build(bool isMainThread = false)
        {
            Atlas.Register(tex);

            foreach (var tile in tiles3D.Values)
                tile.InitData(sizeZ);

            if (sizeZ > 0)
                switch (geo)
                {
                    case Geo3DType.CUBE: BuildCubeModel(); break;
                    case Geo3DType.VCYLINDER: BuildVCylinderModel(); break;
                    case Geo3DType.HCYLINDER: BuildHCylinderModel(); break;
                    case Geo3DType.Default: BuildDefaultModel(); break;
                    case Geo3DType.HALFVCYLINDER: BuildHalfVCylinderModel(); break;
                    case Geo3DType.HALFHCYLINDER: BuildHalfHCylinderModel(); break;
                    default: break;
                }
            state = State.GeoCreated;

            foreach (var tile in tiles3D.Values)
                tile.GenerateMesh();
            foreach (var tile in tiles3D.Values)
                tile.ReleasePixelData();
            state = State.DataCreated;

            if (isMainThread)
                SetMesh();
            else
                Misc.RunInMainThread(() => SetMesh());
        }

        public void SetMesh()
        {
            foreach (Tile3D tile in tiles3D.Values)
                tile.SetMesh();
            state = State.Ready;
        }

        public void Release()
        {
            state = State.Released;
            foreach (Tile3D tile in tiles3D.Values)
                tile.Release();
        }

        public Vector3 GetPos(Vector3 pivot)
        {
            Vector3 v1 = new Vector3(shape.pStart.x, Frame.HeightInPixel - shape.pEnd.y, 0);
            Vector3 v2 = new Vector3(shape.pEnd.x + 1, Frame.HeightInPixel - shape.pStart.y + 1, 0);
            ZLayer realLayer = (layer != ZLayer.UNIDENTIFIED) ? layer : (shape.bg ? SettingManager.Ins.Layer.DefLayer : ZLayer.L1);
            v1.z += SettingManager.Ins.Layer[realLayer].Offset + (sizeZ / 2f) * (SettingManager.Ins.Layer[realLayer].Coef - 1);
            v2.z += SettingManager.Ins.Layer[realLayer].Offset + (sizeZ / 2f) * (SettingManager.Ins.Layer[realLayer].Coef + 1);
            v1.Scale(Vector3.one - pivot);
            v2.Scale(pivot);
            return (v1 + v2) + offset;
        }

        public Vector3 GetPos()
        {
            return GetPos(pivot);
        }

        public Vector3 GetOriginalPos(Vector3 pivot)
        {
            Vector3 v1 = bottomLeft;
            Vector3 v2 = topRight;
            v1.Scale(Vector3.one - pivot);
            v2.Scale(pivot);
            return (v1 + v2);
        }


        public Vector3 GetTilePos(Tile tile)
        {
            return new Vector3(tile.x + 4 + offset.x, Frame.HeightInPixel - (tile.y + 4) + offset.y, pos.z);
        }

        public Vector3 GetOriginalTilePos(Tile tile)
        {
            return new Vector3(tile.x + 4, Frame.HeightInPixel - (tile.y + 4), 0);
        }

        public bool MeshReady
        {
            get { return state == State.Ready; }
        }

        public bool MeshDataReady
        {
            get { return state == State.DataCreated; }
        }

        ~Pattern3D()
        {
            if (tex != null)
                Atlas.Deregister(tex);
        }

        public Vector3 bottomLeft
        {
            get { return new Vector3(shape.pStart.x, Frame.HeightInPixel - shape.pEnd.y, - sizeZ / 2f); }
        }

        public Vector3 topRight
        {
            get { return new Vector3(shape.pEnd.x, Frame.HeightInPixel - shape.pStart.y, sizeZ / 2f); }
        }

        public Vector3 size
        {
            get { return shape.pEnd - shape.pStart + new Vector3(1, 1, sizeZ); }
        }

        #region scripting

        public void RunUpdateScript()
        {
            foreach (Script script in linkedScript)
                script.RunUpdateShape(this);
        }

        private bool IsLinkableScript(Script s)
        {
            if (s.TagStr.Contains(Script.AllTag))
                return true;
            foreach (var tag in s.tags)
                foreach (var tag1 in tags)
                    if (tag1.Equals(tag))
                        return true;

            return false;
        }

        public void LinkToScript(Script s)
        {
            if (IsLinkableScript(s))
            {
                if (!linkedScript.Contains(s))
                    linkedScript.Add(s);
            }
            else
            {
                linkedScript.Remove(s);
            }
        }

        private void LinkToScripts()
        {
            if (linkedScript == null)
                linkedScript = new List<Script>();
            else
                linkedScript.Clear();

            for (int i = 0; i < ScriptManager.Ins.Count; i++)
                LinkToScript(ScriptManager.Ins.GetScript(i));
        }

        #endregion scripting
    }
}