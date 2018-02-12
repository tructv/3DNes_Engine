using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public class Frame
    {
        public static int Width, Height, SpriteCount, GridWidth, GridHeight, WidthInPixel, HeightInPixel;

        private static ReverseComparerByArea areaComparer = new ReverseComparerByArea();
        private static ComparerByLocation locComparer = new ComparerByLocation();

        public Tile[] bgTiles;
        public Tile[] spTiles;

        public Tile[] spGrid;

        public List<Shape> shapes = new List<Shape>();
        public List<Pattern3D> shapes3D = new List<Pattern3D>();

        public PaletteIndex colorPalette = new PaletteIndex();

        public Vector2 mask;
        public int bgTileCount;
        public int bgPixelCount;
        public int bgShapeCount;

        public int tile3DCount;
        public int shape3DCount { get { return shapes3D.Count; } }
        public int shapeCount { get { return shapes.Count; } }

        public float time;
        private int counter;
        public int frameCounter
        {
            get { return counter; }
            set
            {
                counter = value;
                time = counter / 60f;
            }
        }

        private static Tile defaultTile = new Tile();

        public Frame(VideoInfo info)
        {
            Width = info.width;
            WidthInPixel = Width << 3;
            Height = info.height;
            HeightInPixel = Height << 3;
            GridHeight = Height + 2;
            GridWidth = Width + 2;
            SpriteCount = info.spCount;
            bgTiles = new Tile[GridWidth * GridHeight];
            spTiles = new Tile[SpriteCount * 2];
            spGrid = new Tile[GridWidth * GridHeight];

            mask = Vector2.zero;
            bgTileCount = 0;
            bgPixelCount = 0;
            bgShapeCount = 0;

            for (int i = 0; i < bgTiles.Length; i++)
            {
                bgTiles[i] = new Tile();
                bgTiles[i].tPos.x = i % GridWidth;
                bgTiles[i].tPos.y = i / GridWidth;
                bgTiles[i].index = i;
            }

            for (int i = 0; i < spTiles.Length; i++)
                spTiles[i] = new Tile();
        }

        public void CalculatePos()
        {
            foreach (var s in shapes)
                s.CalculatePos();
        }

        public void SortShapeByLocation()
        {
            shapes.Sort(0, bgShapeCount, locComparer);
            shapes.Sort(bgShapeCount, shapes.Count - bgShapeCount, locComparer);
        }

        public void SortSpritesByArea()
        {
            shapes.Sort(bgShapeCount, shapes.Count - bgShapeCount, areaComparer);
        }

        public Shape Track(Shape s)
        {
            if (s == null)
                return null;
            Shape found = null;
            float dis, newDis;
            if (s.bg)
            {
                dis = 50;
                for (int i = 0; i < bgShapeCount; i++)
                    if (((newDis = s.Distance(shapes[i])) < dis)
                            || ((newDis == dis) && (shapes[i].id == s.id)))
                    {
                        dis = newDis;
                        found = shapes[i];
                    }
            }
            else
            {
                dis = 6;
                for (int i = bgShapeCount; i < shapes.Count; i++)
                    if (((newDis = s.Distance(shapes[i])) < dis)
                        || ((newDis == dis) && (shapes[i].id == s.id)))
                    {
                        dis = newDis;
                        found = shapes[i];
                    }
            }
            return found;
        }

        public void Reset()
        {
            foreach (Shape s in shapes)
                Shape.Push(s);
            shapes.Clear();
            shapes3D.Clear();

            mask = Vector2.zero;
            bgTileCount = 0;
            bgPixelCount = 0;
            bgShapeCount = 0;
            tile3DCount = 0;

            for (int i = 0; i < bgTiles.Length; i++)
                bgTiles[i].Disable();

            for (int i = 0; i < spTiles.Length; i++)
                spTiles[i].Disable();

            for (int i = 0; i < spGrid.Length; i++)
                spGrid[i] = defaultTile;
        }

        public void AddShape(Shape s)
        {
            shapes.Add(s);
            for (int i = 0; i < s.InsCount; i++)
                shapes3D.Add(s[i]);
            tile3DCount += s.InsCount * s.tiles.Count;
        }

        public static List<Pattern3D> result = new List<Pattern3D>();
        public List<Pattern3D> GetShapes(string tag)
        {
            result.Clear();
            foreach (var s in shapes3D)
                if (s.ContainsTag(tag))
                    result.Add(s);
            return result;
        }

        public Pattern3D GetShape(string tag)
        {
            foreach (var s in shapes3D)
                if (s.ContainsTag(tag))
                    return s;
            return null;
        }
    }
}