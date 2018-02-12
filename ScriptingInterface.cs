using Nes3D.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Nes3D.Engine3D
{
    //public interface IScriptManager
    //{
    //    IScript Get(string name);
    //    byte ReadMem(ushort address);
    //    void WriteMem(ushort address, byte value);
    //}

    //public interface IScript
    //{
    //    string Name { get; }
    //    bool Enable { get; set; }
    //    bool Error { get; }
    //}

    //public interface Shape
    //{
    //    int Count { get; }
    //    Pattern3D Shape3D(int index = 0);
    //    IntVector2 Start { get; }
    //    IntVector2 End { get; }
    //    IntVector2 Size { get; }
    //    IntVector2 TSize { get; }
    //    bool Bg { get; }
    //    bool Hiden { get; }
    //    int Palette { get; }
    //    int TileCount { get; }

    //    int Age { get; }
    //    bool Completed { get; }
    //    bool Inside { get; }
    //    bool AtBorder { get; }
    //}

    //public interface Pattern3D
    //{
    //    Shape Shape2D { get; }
    //    int Index { get; }
    //    ZLayer Layer { get; set; }
    //    bool UI { get; set; }
    //    PVector3 Scale { get; }
    //    PVector3 Rot { get; }
    //    PVector3 Offset { get; }
    //    PVector3 Pivot { get; }
    //    float Alpha { get; set; }
    //    PVector3 DeformAmp { get; }
    //    PVector3 DeformPivot { get; }
    //    PMatrix4x4 DeformSpeed { get; }
    //    PMatrix4x4 DeformOffset { get; }
    //    bool Enable { get; set; }

    //    bool Sp { get; }
    //    bool Bg { get; }
    //    Vector3 OriPos { get; }
    //    Vector3 BottomLeft { get; }
    //    Vector3 TopRight { get; }
    //    Vector3 GetDeform(Vector3 pos);
    //}

    //public interface IFrameManager 
    //{
    //    List<Pattern3D> GetShapesWithTag(string tag);
    //    Pattern3D GetShapeWithTag(string tag);
    //    Pattern3D GetShape(int index);
    //    int shapeCount { get; }
    //    int frameCounter { get; }
    //    float time { get; }
    //}
}
