using Nes3D.Utils;
using UnityEngine;

namespace Nes3D.Engine3D
{
    // drawing part
    public partial class Pattern3D
    {
        private void SetUV(int x, int y, int z, IntVector2 uv, int d = -1)
        {
            var tile3D = tile3DGrid[(y >> 3) * tSize.x + (x >> 3)];
            if (tile3D != null)
                tile3D.SetUV(x & 7, y & 7, z, uv, d);
        }

        private void SetUV(int x, int y, int z, int d = -1)
        {
            SetUV(x, y, z, new IntVector2(x, y), d);
        }

        public IntVector2 GetUV(int x, int y, int z, int d)
        {
            var tile3D = tile3DGrid[(y >> 3) * tSize.x + (x >> 3)];
            if (tile3D != null)
                return tile3D.GetUV(x & 7, y & 7, z, d);
            return Tile3D.EmptyUV;
        }

        private static IntVector3[] posArray = new IntVector3[4];
        private static IntVector2[] uvArray = new IntVector2[2];

        public void DrawEllipse(int x0, int y0, int dx, int dy, int h, int dir, int drawQuater = 15)
        {
            int a2 = 2 * dx * dx;
            int b2 = 2 * dy * dy;
            int error = dx * dx * dy;
            int x = 0;
            int y = dy;
            int stopy = 0;
            int stopx = a2 * dy;
            int x1, x2, y1, y2;

            if ((dx == 0) || (dy == 0))
            {
                if (dir == 0)
                {
                    SetUV(x0 >> 1, h, y0 >> 1);
                }
                else
                {
                    SetUV(h, x0 >> 1, y0 >> 1);
                }
                return;
            }
            int maxX = dir == 0 ? tSize.x * 8 : tSize.y * 8;
            while (stopy <= stopx)
            {
                x1 = Mathf.Clamp((x0 + x - 1) >> 1, 0, maxX);
                x2 = Mathf.Clamp((x0 - x - 1) >> 1, 0, maxX);
                y1 = (y0 + y) >> 1;
                y2 = (y0 - y + 1) >> 1;
                if (dir == 0)
                {
                    uvArray[0] = new IntVector2(x1, h);
                    uvArray[1] = new IntVector2(x2, h);
                    posArray[0] = new IntVector3(x1, h, y1);
                    posArray[1] = new IntVector3(x1, h, y2);
                    posArray[2] = new IntVector3(x2, h, y1);
                    posArray[3] = new IntVector3(x2, h, y2);
                }
                else
                {
                    uvArray[0] = new IntVector2(h, x1);
                    uvArray[1] = new IntVector2(h, x2);
                    posArray[0] = new IntVector3(h, x1, y1);
                    posArray[1] = new IntVector3(h, x1, y2);
                    posArray[2] = new IntVector3(h, x2, y1);
                    posArray[3] = new IntVector3(h, x2, y2);
                }

                if (!solid)
                {
                    for (int i = 0; i < 4; i++)
                        if ((drawQuater & (1 << i)) != 0)
                            SetUV(posArray[i].x, posArray[i].y, posArray[i].z, uvArray[i / 2]);
                }
                else
                {
                    for (int j = y2; j <= y1; j++)
                    {
                        if ((drawQuater & 3) == 3)
                            SetUV(posArray[0].x, posArray[0].y, j, uvArray[0]);
                        if ((drawQuater & 12) == 12)
                            SetUV(posArray[2].x, posArray[2].y, j, uvArray[1]);
                    }
                }

                ++x;
                error -= b2 * (x - 1);
                stopy += b2;
                if (error <= 0)
                {
                    error += a2 * (y - 1);
                    --y;
                    stopx -= a2;
                }
            }

            error = dy * dy * dx;
            x = dx;
            y = 0;
            stopy = b2 * dx;
            stopx = 0;
            while (stopy >= stopx)
            {
                x1 = (x0 + x - 1) >> 1;
                x2 = (x0 - x) >> 1;
                y1 = (y0 + y) >> 1;
                y2 = (y0 - y + 1) >> 1;
                if (dir == 0)
                {
                    if (antiShadow)
                    {
                        uvArray[0] = new IntVector2((x0 + y) >> 1, h);
                        uvArray[1] = new IntVector2((x0 - y + 1) >> 1, h);
                    }
                    else
                    {
                        uvArray[0] = new IntVector2(x1, h);
                        uvArray[1] = new IntVector2(x2, h);
                    }
                    posArray[0] = new IntVector3(x1, h, y1);
                    posArray[1] = new IntVector3(x1, h, y2);
                    posArray[2] = new IntVector3(x2, h, y1);
                    posArray[3] = new IntVector3(x2, h, y2);
                }
                else
                {
                    if (antiShadow)
                    {
                        uvArray[0] = new IntVector2(h, (x0 + y) >> 1);
                        uvArray[1] = new IntVector2(h, (x0 - y + 1) >> 1);
                    }
                    else
                    {
                        uvArray[0] = new IntVector2(h, x1);
                        uvArray[1] = new IntVector2(h, x2);
                    }

                    posArray[0] = new IntVector3(h, x1, y1);
                    posArray[1] = new IntVector3(h, x1, y2);
                    posArray[2] = new IntVector3(h, x2, y1);
                    posArray[3] = new IntVector3(h, x2, y2);
                }

                if (!solid)
                {
                    for (int i = 0; i < 4; i++)
                        if ((drawQuater & (1 << i)) != 0)
                            SetUV(posArray[i].x, posArray[i].y, posArray[i].z, uvArray[i / 2]);
                }
                else
                {
                    for (int j = y2; j <= y1; j++)
                    {
                        if ((drawQuater & 3) == 3)
                            SetUV(posArray[0].x, posArray[0].y, j, uvArray[0]);
                        if ((drawQuater & 12) == 12)
                            SetUV(posArray[2].x, posArray[2].y, j, uvArray[1]);
                    }
                }

                ++y;
                error -= a2 * (y - 1);
                stopx += a2;
                if (error < 0)
                {
                    error += b2 * (x - 1);
                    --x;
                    stopy -= b2;
                }
            }
        }

        public void BuildHCylinderModel()
        {
            int j, i, k;
            int zDiameter;
            int realSizeZ = 0;

            int sizeY = tSize.y * 8;
            int sizeX = tSize.x * 8;

            for (i = 0; i < sizeX; i++)
                for (j = 0; j < sizeY; j++)
                {
                    if (tex.GetPixel(i, j) != 0)
                    {
                        k = j;
                        do
                            k++;
                        while (k < sizeY && tex.GetPixel(i, k) != 0);
                        k--;

                        zDiameter = Mathf.Min((sizeZ - 1), k - j);
                        DrawEllipse(k + j, sizeZ - 1, k - j, zDiameter, i, 1);
                        j = k;
                        realSizeZ = Mathf.Max(realSizeZ, zDiameter + 1);
                    }
                }
            sizeZ = realSizeZ;
        }

        public void BuildVCylinderModel()
        {
            int j, i, k;
            int zDiameter;
            int realSizeZ = 0;

            int sizeY = tSize.y * 8;
            int sizeX = tSize.x * 8;

            for (j = 0; j < sizeY; j++)
                for (i = 0; i < sizeX; i++)
                {
                    if (tex.GetPixel(i, j) != 0)
                    {
                        k = i;
                        do
                            k++;
                        while (k < sizeX && tex.GetPixel(k, j) != 0);
                        k--;

                        zDiameter = Mathf.Min(sizeZ - 1, k - i);
                        DrawEllipse(k + i, sizeZ - 1, k - i, zDiameter, j, 0);
                        i = k;
                        realSizeZ = Mathf.Max(realSizeZ, zDiameter + 1);
                    }
                }
            sizeZ = realSizeZ;
        }

        public void BuildHalfHCylinderModel()
        {
            int j, i, k;
            int zDiameter;
            int realSizeZ = 0;

            int sizeY = tSize.y * 8;
            int sizeX = tSize.x * 8;

            for (i = 0; i < sizeX; i++)
                for (j = 0; j < sizeY; j++)
                {
                    if (tex.GetPixel(i, j) != 0)
                    {
                        k = j;
                        do
                            k++;
                        while (k < sizeY && tex.GetPixel(i, k) != 0);
                        k--;

                        zDiameter = Mathf.Min((sizeZ - 1), (k - j) * 2);
                        DrawEllipse(flip ? j * 2 : k * 2, sizeZ - 1, (k - j) * 2, zDiameter, i, 1, flip ? 3 : 12);
                        j = k;
                        realSizeZ = Mathf.Max(realSizeZ, zDiameter + 1);
                    }
                }
            sizeZ = realSizeZ;
        }

        public void BuildHalfVCylinderModel()
        {
            int j, i, k;
            int zDiameter;
            int realSizeZ = 0;

            int sizeY = tSize.y * 8;
            int sizeX = tSize.x * 8;

            for (j = 0; j < sizeY; j++)
                for (i = 0; i < sizeX; i++)
                {
                    if (tex.GetPixel(i, j) != 0)
                    {
                        k = i;
                        do
                            k++;
                        while (k < sizeX && tex.GetPixel(k, j) != 0);
                        k--;

                        zDiameter = Mathf.Min(sizeZ - 1, (k - i) * 2);
                        DrawEllipse(flip ? k * 2 : i * 2, sizeZ - 1, (k - i) * 2, zDiameter, j, 0, flip ? 12 : 3);
                        i = k;
                        realSizeZ = Mathf.Max(realSizeZ, zDiameter + 1);
                    }
                }
            sizeZ = realSizeZ;
        }

        public void BuildCubeModel()
        {
            int i, j;

            int sizex = tSize.x * 8;
            int sizey = tSize.y * 8;

            for (i = 0; i < sizex; i++)
                for (j = 0; j < sizeZ; j++)
                {
                    SetUV(i, sizey - 1, j, new IntVector2(i, j), 1);
                    SetUV(i, 0, j, new IntVector2(i, sizeZ - 1 - j), 1);
                }
            for (i = 0; i < sizey; i++)
                for (j = 0; j < sizeZ; j++)
                {
                    SetUV(0, i, j, new IntVector2(sizeZ - 1 - j, i), 0);
                    SetUV(sizex - 1, i, j, new IntVector2(sizeZ - 1 - j, i), 0);
                }

            for (i = 0; i < sizex; i++)
                for (j = 0; j < sizey; j++)
                {
                    SetUV(i, j, sizeZ - 1, new IntVector2(i, j), 2);
                    SetUV(i, j, 0, new IntVector2(i, j), 2);
                }
        }

        private void BuildDefaultModel()
        {
            if (fullWarp)
            {
                for (int i = 0; i < tSize.x * 8; i++)
                    for (int j = 0; j < tSize.y * 8; j++)
                        for (int k = 0; k < sizeZ; k++)
                            if (tex.GetPixel(i, j) != 0)
                            {
                                int j1 = j + k;
                                while (tex.GetPixel(i, j1) == 0)
                                    j1--;
                                SetUV(i, j, k, new IntVector2(i, j1));
                            }
            }
            else
            {
                for (int i = 0; i < tSize.x * 8; i++)
                    for (int j = 0; j < tSize.y * 8; j++)
                        for (int k = 0; k < sizeZ; k++)
                            SetUV(i, j, k);
            }
        }
    }
}