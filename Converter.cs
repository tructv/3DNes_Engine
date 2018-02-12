using Nes3D.Utils;
using System.Collections.Generic;

namespace Nes3D.Engine3D
{
    public class Converter
    {
        public static readonly IntVector2[] dirs = {   new IntVector2(1, 0), new IntVector2(0, 1), new IntVector2(0, -1), new IntVector2(-1, 0),//right - up - down - left
                                                new IntVector2(1, 1), new IntVector2(1, -1), new IntVector2(-1, 1), new IntVector2(-1, -1)
                                                };

        public static int[] absDirs;

        public static Frame frame;

        private static List<Pattern> patternCandidates = new List<Pattern>();
        private static List<Pattern> newPatternCadidates = new List<Pattern>();
        private static Queue<Tile> tileQueue = new Queue<Tile>();
        private static List<Tile> bgValidTiles = new List<Tile>();
        private static List<Tile> spValidTiles = new List<Tile>();
        private static List<Shape> rollbackList = new List<Shape>();

        public static void Init(VideoInfo info)
        {
            //absDirs = new int[8] { 1, FrameBuffer.GRID_WIDTH, -FrameBuffer.GRID_WIDTH, -1,//right - up - down - left
            //                       1 + FrameBuffer.GRID_WIDTH, 1 - FrameBuffer.GRID_WIDTH, -1 + FrameBuffer.GRID_WIDTH, -1 - FrameBuffer.GRID_WIDTH};
            absDirs = new int[4] { 1, Frame.GridWidth, -Frame.GridWidth, -1 };//right - up - down - left
        }

        static public IntVector2 World2Grid(int x, int y)
        {
            return new IntVector2((x + 7 >> 3) + 1, (y + 7 >> 3) + 1);
        }

        // local variable
        private static RawTile2D rawData = new RawTile2D();

        private static Tile tile;

        public static void RenderVideoCallback(TileData[] data, int count, byte[] palette, int maskX, int maskY)
        {
            frame = FrameManager.Ins.OfflineFrame;
            spValidTiles.Clear();
            bgValidTiles.Clear();

            frame.frameCounter = Emulator.Ins.engine.frameCounter;
            frame.mask = new IntVector2(maskX, maskY);

            for (int i = 0; i < frame.colorPalette.Length; i++)
                frame.colorPalette[i] = palette[i];

            for (int i = 0; i < count; i++)
            {
                var item = data[i];
                {
                    rawData.InitData(ref item);

                    if (item.isBg)
                    {
                        // convert from grid cordinate to grid with mask cordinate
                        IntVector2 pos = World2Grid(item.x, item.y);
                        tile = frame.bgTiles[pos.y * Frame.GridWidth + pos.x];

                        if (!PatternManager.tiles.TryGetValue(rawData, out tile.tile2D))
                            tile.tile2D = new Tile2D(rawData);

                        if (!tile.IsEmpty)
                        {
                            tile.Reset();
                            tile.x = item.x;
                            tile.y = item.y;
                            tile.palette = item.palette;
                            tile.hidden = item.hidden;
                            frame.bgTileCount++;
                            frame.bgPixelCount += tile.tile2D.pixelCount;
                            bgValidTiles.Add(tile);
                        }
                    }
                    else if (item.y < 240)
                    {
                        tile = frame.spTiles[item.index];
                        if (!PatternManager.tiles.TryGetValue(rawData, out tile.tile2D))
                            tile.tile2D = new Tile2D(rawData);
                        spValidTiles.Add(tile);
                        tile.Reset();
                        tile.x = item.x;
                        tile.y = item.y;
                        tile.tPos = World2Grid(tile.x, tile.y);
                        tile.palette = item.palette;
                        tile.hidden = item.hidden;
                        //tile.index = item.index;
                        tile.index = tile.tPos.y * Frame.GridWidth + tile.tPos.x;
                    }
                }
            }
        }

        public static void Process()
        {
            SegmentBackground();
            SegmentSprite();
            UpdateLifeCycle();
            AutoLayering();
            frame.SortShapeByLocation();
        }

        // shape segmentation
        public static void SegmentBackground()
        {
            Tile tile, tile1;
            Tile[] bgTiles;

            bgTiles = frame.bgTiles;
            tileQueue.Clear();

            rollbackList.Clear();

            for (int i = 0; i < bgValidTiles.Count; i++)
                if ((tile = bgValidTiles[i]).shapeId == 0)
                    if (tile.tile2D.IsPermanent && !tile.IsBorder())
                    {
                        Shape shape = Shape.Get();
                        shape.Reset(bgTiles);

                        patternCandidates.Clear();
                        foreach (var p in tile.tile2D.patternSet)
                            patternCandidates.Add(p);

                        shape.AddTile(tile);

                        // expand the shape
                        tileQueue.Clear();
                        tileQueue.Enqueue(tile);

                        while (tileQueue.Count > 0)
                        {
                            tile = tileQueue.Dequeue();
                            if (!tile.IsBorder())
                                for (int j = 0; j < absDirs.Length; j++)
                                    if ((tile1 = bgTiles[tile.index + absDirs[j]]).shapeId == 0)
                                        if (tile.ReachedBorder(j) && tile1.ReachedBorder(3 - j))
                                        {
                                            var newSize = shape.UpdateSize(tile1);
                                            newPatternCadidates.Clear();
                                            foreach (var p in patternCandidates)
                                                if (tile1.tile2D.patternSet.Contains(p) && newSize <= p.constraintSize)
                                                    newPatternCadidates.Add(p);

                                            if (newPatternCadidates.Count > 0)
                                            {
                                                var temp = patternCandidates;
                                                patternCandidates = newPatternCadidates;
                                                newPatternCadidates = temp;
                                                shape.AddTile(tile1);
                                                tileQueue.Enqueue(tile1);
                                            }
                                        }
                        }

                        if ((shape.IsSolid() && shape.tiles.Count >= 8 && shape.tiles2D.Count >= 2) || shape.IsBig())
                        {
                            Pattern selected = null;
                            foreach (var p in patternCandidates)
                                if (selected == null || selected.tiles2D.Count > p.tiles2D.Count)
                                    selected = p;

                            shape.PatternBinding(selected);
                            frame.AddShape(shape);
                        }
                        else
                            rollbackList.Add(shape);
                    }

            foreach (Shape s in rollbackList)
                s.Release();

            for (int i = 0; i < bgValidTiles.Count; i++)
                if ((tile = bgValidTiles[i]).shapeId == 0)
                {
                    if (!tile.tile2D.IsPermanent)
                    {
                        Shape shape = Shape.Get();
                        shape.Reset(bgTiles);
                        shape.AddTile(tile);

                        if (!tile.IsCHR)
                        {
                            // expand the shape
                            tileQueue.Clear();
                            tileQueue.Enqueue(tile);
                            while (tileQueue.Count > 0)
                            {
                                tile = tileQueue.Dequeue();
                                for (int j = 0; j < absDirs.Length; j++)
                                {
                                    if ((tile1 = bgTiles[tile.index + absDirs[j]]).shapeId == 0)
                                        if (!tile1.IsCHR)
                                            if (tile.palette == tile1.palette)
                                                if ((tile.ReachedBorder(j)) && (tile1.ReachedBorder(3 - j)))
                                                //if (tile.BorderConnected(j, tile1))
                                                {
                                                    shape.AddTile(tile1);
                                                    tileQueue.Enqueue(tile1);
                                                }
                                }
                            }
                        }

                        if (shape.Process(true))
                            frame.AddShape(shape);
                    }
                }

            for (int i = 0; i < bgValidTiles.Count; i++)
                if ((tile = bgValidTiles[i]).shapeId == 0)
                    if (tile.tile2D.IsPermanent)
                    {
                        Shape shape = Shape.Get();
                        shape.Reset(bgTiles);
                        shape.AddTile(tile);

                        patternCandidates.Clear();
                        foreach (var p in tile.tile2D.patternSet)
                            patternCandidates.Add(p);

                        // expand the shape
                        tileQueue.Clear();
                        tileQueue.Enqueue(tile);

                        while (tileQueue.Count > 0)
                        {
                            tile = tileQueue.Dequeue();
                            for (int j = 0; j < absDirs.Length; j++)
                            {
                                if ((tile1 = bgTiles[tile.index + absDirs[j]]).shapeId == 0)
                                {
                                    bool connect = tile.ReachedBorder(j) && tile1.ReachedBorder(3 - j);
                                    var newSize = shape.UpdateSize(tile1);
                                    newPatternCadidates.Clear();
                                    foreach (var p in patternCandidates)
                                        if (tile1.tile2D.patternSet.Contains(p) && newSize <= p.constraintSize && (!p.selfConnected || connect))
                                            newPatternCadidates.Add(p);

                                    if (newPatternCadidates.Count > 0)
                                    {
                                        var temp = patternCandidates;
                                        patternCandidates = newPatternCadidates;
                                        newPatternCadidates = temp;
                                        shape.AddTile(tile1);
                                        tileQueue.Enqueue(tile1);
                                    }
                                }
                            }
                        }

                        Pattern selected = null;
                        foreach (var p in patternCandidates)
                            if (selected == null || selected.tiles2D.Count > p.tiles2D.Count)
                                selected = p;

                        shape.PatternBinding(selected);
                        frame.AddShape(shape);
                    }

            frame.bgShapeCount = frame.shapes.Count;
        }

        private static void SegmentSprite()
        {
            Shape shape;
            Tile tile1;
            Tile[] tiles;

            tiles = frame.spGrid;

            for (int i = 0; i < spValidTiles.Count; i++)
                if ((tile = spValidTiles[i]).shapeId == 0 && !tile.tile2D.IsPermanent)
                {
                    shape = Shape.Get();
                    shape.Reset(tiles, false);

                    shape.AddTile(tile);
                    tileQueue.Clear();
                    tileQueue.Enqueue(tile);

                    while (tileQueue.Count > 0)
                    {
                        tile = tileQueue.Dequeue();

                        for (int j = 0; j < spValidTiles.Count; j++)
                            if ((tile1 = spValidTiles[j]).shapeId == 0 && tile.IsConnected(tile1) != -1)
                            {
                                tileQueue.Enqueue(tile1);
                                shape.AddTile(tile1);
                            }
                    }
                    if (shape.Process(false))
                        frame.AddShape(shape);
                }

            for (int i = 0; i < spValidTiles.Count; i++)
                if ((tile = spValidTiles[i]).shapeId == 0)
                {
                    shape = Shape.Get();
                    shape.Reset(tiles, false);

                    patternCandidates.Clear();
                    foreach (var p in tile.tile2D.patternSet)
                        patternCandidates.Add(p);

                    shape.AddTile(tile);
                    tileQueue.Clear();
                    tileQueue.Enqueue(tile);

                    while (tileQueue.Count > 0)
                    {
                        tile = tileQueue.Dequeue();
                        int dir;
                        for (int j = 0; j < spValidTiles.Count; j++)
                            if ((tile1 = spValidTiles[j]).shapeId == 0 && (dir = tile.IsConnected(tile1)) != -1 /*&& (tile.palette == tile1.palette)*/)
                            {
                                bool connect = tile.ReachedBorder(dir) && tile1.ReachedBorder(3 - dir);
                                var newSize = shape.UpdateSize(tile1);
                                newPatternCadidates.Clear();
                                foreach (var p in patternCandidates)
                                    if (tile1.tile2D.patternSet.Contains(p) && newSize <= p.constraintSize && (!p.selfConnected || connect))
                                        newPatternCadidates.Add(p);

                                if (newPatternCadidates.Count > 0)
                                {
                                    var t = patternCandidates;
                                    patternCandidates = newPatternCadidates;
                                    newPatternCadidates = t;
                                    shape.AddTile(tile1);
                                    tileQueue.Enqueue(tile1);
                                }
                            }
                    }

                    Pattern selected = null;
                    foreach (var p in patternCandidates)
                        if (selected == null || selected.tiles2D.Count > p.tiles2D.Count)
                            selected = p;

                    shape.PatternBinding(selected);
                    frame.AddShape(shape);
                }
        }

        private static void UpdateLifeCycle()
        {
            foreach (Shape s in frame.shapes)
            {
                if (s.refPattern.endFrame + 1 != frame.frameCounter)
                    s.refPattern.startFrame = frame.frameCounter;
                s.refPattern.endFrame = frame.frameCounter;
            }
        }

        private static void AutoLayering()
        {
            if (!SettingManager.Ins.Layer.Auto)
                return;
            frame.SortSpritesByArea();
            List<Shape> shapes = frame.shapes;

            Shape bgShape;
            for (int i = 0; i < frame.bgShapeCount; i++)
                if ((bgShape = shapes[i]).refPattern.layer == ZLayer.UNIDENTIFIED)
                    {
                        for (int j = frame.bgShapeCount; j < shapes.Count; j++)
                        {
                            Shape spShape = shapes[j];
                            if (spShape.tiles.Count < 4)
                                break;

                            if (bgShape.tiles.Count >= 4 && !spShape.hiden && spShape.refPattern.pDensity > DensityType.LOW && bgShape.IsContactWith(spShape))
                            {
                                if (bgShape.refPattern.lastContactFrame != frame.frameCounter)
                                {
                                    if (bgShape.refPattern.lastContactFrame + 1 == frame.frameCounter)
                                        bgShape.refPattern.contactCount++;
                                    else
                                        bgShape.refPattern.contactCount = 1;

                                    bgShape.refPattern.lastContactFrame = frame.frameCounter;

                                    if (bgShape.refPattern.contactCount > 30)
                                    {
                                        bgShape.refPattern.layer = ZLayer.L1;
                                        break;
                                    }
                                }
                            }

                            if (!spShape.hiden && (spShape.refPattern.layer == ZLayer.UNIDENTIFIED || spShape.refPattern.layer == ZLayer.L1) && spShape.refPattern.pDensity > DensityType.LOW && Shape.Overlaps(bgShape, spShape))
                            {
                                if (bgShape.refPattern.lastOverlapFrame != frame.frameCounter)
                                {
                                    if (bgShape.refPattern.lastOverlapFrame + 1 == frame.frameCounter)
                                    {
                                        bgShape.refPattern.overlapCount++;
                                    }
                                    else
                                    {
                                        bgShape.refPattern.overlapCount = 1;
                                    }

                                    bgShape.refPattern.lastOverlapFrame = frame.frameCounter;

                                    if (bgShape.refPattern.overlapCount > 30 && bgShape.bg)
                                    {
                                        bgShape.refPattern.layer = ZLayer.L2;
                                        break;
                                    }
                                }
                            }
                        }
                    }
        }
    }
}