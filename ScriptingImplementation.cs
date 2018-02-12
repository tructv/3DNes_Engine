using Nes3D.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public partial class ScriptManager /*: IScriptManager*/
    {
        public Script Get(string name)
        {
            return GetScript(name);
        }

        public byte ReadMem(ushort address)
        {
            return Engine.Ins.core.ReadMemory(address);
        }

        public void WriteMem(ushort address, byte value)
        {
            Engine.Ins.core.WriteMemory(address, value);
        }
    }

    public partial class Script /*: IScript*/
    {
        public bool Enable { get { return enable; } set { enable = value; } }
        public bool Error { get { return error; } }
    }

    public partial class Shape 
    {
        public int Count { get { return InsCount; } }
        public Pattern3D Shape3D(int index = 0) { return this[index]; }
        public IntVector2 Start { get { return pStart; } }
        public IntVector2 End { get { return pEnd; } }
        public IntVector2 Size { get { return pEnd - pStart + new IntVector2(1, 1); } }
        public IntVector2 TStart { get { return tStart; } }
        public IntVector2 TEnd { get { return tEnd; } }
        public IntVector2 TSize { get { return tSize; } }
        public bool Bg { get { return bg; } }
        public bool Hiden { get { return hiden; } }
        public int Palette { get { return palIndex; } }
        public int TileCount { get { return tiles.Count; } }
        public int Age { get { return refPattern.endFrame - refPattern.startFrame + 1; } }
    }

    public partial class Pattern3D 
    {
        public Shape Shape2D { get { return shape; } }
        public int Index { get { return index; } }
        public ZLayer Layer { get { return layer; } set { layer = value; } }
        public bool UI { get { return ui; } set { ui = value; } }
        public PVector3 Scale { get { return scale; } }
        public PVector3 Rot { get { return rot; } }
        public PVector3 Offset { get { return offset; } }
        public PVector3 Pivot { get { return pivot; } }
        public float Alpha { get { return alpha; } set { alpha = value; } }
        public PVector3 DeformAmp { get { return deltaAmp; } }
        public PVector3 DeformPivot { get { return deltaPivot; } }
        public PMatrix4x4 DeformSpeed { get { return deltaSpeed; } }
        public PMatrix4x4 DeformOffset { get { return deltaOffset; } }
        public bool Enable { get { return enable; } set { enable = value; } }

        public bool Sp { get { return !shape.Bg; } }
        public bool Bg { get { return shape.Bg; } }

        public Vector3 OriPos { get { return(shape.pStart + shape.pEnd) / 2f;} }
        public Vector3 BottomLeft { get { return bottomLeft;} }
        public Vector3 TopRight { get { return topRight;} }

        public Vector3 GetDeform(Vector3 pos)
        {
            Vector3 result = Vector3.zero;
            Vector3 _size = size;
            float time = FrameManager.Ins.time;
            pos = pos - GetOriginalPos(deltaPivot);
            pos = new Vector3(pos.x / _size.x, pos.y / _size.y, pos.z / _size.z);
            result.x = Mathf.Sin(pos.x * deltaSpeed.m00 + deltaOffset.m00) * Mathf.Sin(pos.y * deltaSpeed.m01 + deltaOffset.m01) * Mathf.Sin(pos.z * deltaSpeed.m02 + deltaOffset.m02) * Mathf.Sin(time * deltaSpeed.m03 + deltaOffset.m03);
            result.y = Mathf.Sin(pos.x * deltaSpeed.m10 + deltaOffset.m10) * Mathf.Sin(pos.y * deltaSpeed.m11 + deltaOffset.m11) * Mathf.Sin(pos.z * deltaSpeed.m12 + deltaOffset.m12) * Mathf.Sin(time * deltaSpeed.m13 + deltaOffset.m13);
            result.z = Mathf.Sin(pos.x * deltaSpeed.m20 + deltaOffset.m20) * Mathf.Sin(pos.y * deltaSpeed.m21 + deltaOffset.m21) * Mathf.Sin(pos.z * deltaSpeed.m22 + deltaOffset.m22) * Mathf.Sin(time * deltaSpeed.m23 + deltaOffset.m23);
            result.Scale(deltaAmp);
            result.Scale(_size);
            return result;
        }
    }
}