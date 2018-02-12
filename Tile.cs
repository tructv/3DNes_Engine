using Nes3D.Utils;
using System;
using UnityEngine;

namespace Nes3D.Engine3D
{
     public class Tile
    {
        public Tile2D tile2D;
        public int shapeId;
        public int palette;
         public Shape shape;

        public int index;

        public bool flipX { get { return tile2D.flipX; } }
        public bool flipY { get { return tile2D.flipY; } }
        public bool hidden;
        public int x, y;

        public IntVector2 tPos;

        public Tile()
        {
            Reset();
        }

        static public bool operator >(Tile tile1, Tile tile2)
        {
            if ((tile1.y > tile2.y)
                || ((tile1.y == tile2.y) && (tile1.x > tile2.x)))
                return true;
            return false;
        }

        public void Reset()
        {
            shapeId = 0;
            shape = null;
        }

        public void Disable()
        {
            shapeId = -1;
            shape = null;
        }

        public bool ColorConnected(Tile tile)
        {
            return tile2D.ColorConnected(tile.tile2D);
        }

        public bool BorderConnected(int dir, Tile tile)
        {
            return tile2D.BorderConnected(dir, tile.tile2D);
        }

        public bool IsHBorder()
        {
            return (tPos.x <= 1 || tPos.x + 2 >= Frame.GridWidth);
        }

        public bool IsVBorder()
        {
            return (tPos.y <= 1 || tPos.y + 2 >= Frame.GridHeight);
        }

        public bool IsBorder()
        {
            return IsHBorder() || IsVBorder();
        }

        public bool IsCHR
        {
            get { return tile2D.geo == Geo2DType.CHAR; }
        }

        static public bool operator <(Tile tile1, Tile tile2)
        {
            if ((tile1.y < tile2.y)
                || ((tile1.y == tile2.y) && (tile1.x < tile2.x)))
                return true;
            return false;
        }

        public bool Active(int x, int y)
        {
            return tile2D.Active(x, y);
        }

        public byte GetColor(int x, int y)
        {
            return tile2D.GetColor(x, y);
        }

        public bool IsEmpty
        {
            get
            {
                return /*tile2D == null ||*/ tile2D.IsEmpty;
            }
        }

        public bool IsSolid
        {
            get { return /*tile2D == null ||*/ tile2D.IsSolid; }
        }

        public bool ReachedBorder(int dir)
        {
            return tile2D.reachedDir[dir];
        }

        public int IsConnected(Tile tile2)
        {
            if (palette == tile2.palette)
            {
                if (y == tile2.y)
                {
                    if (x + 8 == tile2.x)
                        return ReachedBorder(0) && tile2.ReachedBorder(3) ? 0 : -1;
                    else if (tile2.x + 8 == x)
                        return ReachedBorder(3) && tile2.ReachedBorder(0) ? 3 : -1;
                }
                else if (x == tile2.x)
                {
                    if (y + 8 == tile2.y)
                        return ReachedBorder(1) && tile2.ReachedBorder(2) ? 1 : -1;
                    else if (tile2.y + 8 == y)
                        return ReachedBorder(2) && tile2.ReachedBorder(1) ? 2 : -1;
                }
            }
            return -1;
        }
    }
}