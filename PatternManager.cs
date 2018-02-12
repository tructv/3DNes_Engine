using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using UnityEngine;
using Nes3D.Utils;
using System.Threading;

namespace Nes3D.Engine3D
{
    public static class PatternManager
    {
        public static KeyedCollectionEx<RawTile2D, Tile2D> tiles = new KeyedCollectionEx<RawTile2D, Tile2D>(delegate (Tile2D tile) { return tile.rawData; });
        public static PatternCollection patterns;
        public static PatternCollection tempPatterns;

        public static void Init()
        {
            patterns = new PatternCollection(new PatternEquality());
            tempPatterns = new PatternCollection(new PatternEquality());

            SettingManager.Ins.PropertyChanged += OnSettingChanged;
        }

        static private void OnSettingChanged(object sender, PropertyChangedEventArgs args)
        {
            if (ReferenceEquals(args.PropertyName, SettingManager.RenderModeId))
            {
                if (patterns.Count > 0)
                    Misc.RunInMainThread(() =>
                    {
                        Debug.Log("Change Render Mode To " + SettingManager.Ins.RenderMode);
                        foreach (Pattern p in patterns)
                            p.Release();
                        foreach (Pattern p in tempPatterns)
                            p.Release();

                        GC.Collect();
                        Thread.Sleep(100);

                        foreach (Pattern p in patterns)
                            p.Build();
                        foreach (Pattern p in tempPatterns)
                            p.Build();
                    });
            }
        }

        public static void Reset()
        {
            foreach (var p in patterns)
                p.Release();
            foreach (Pattern p in tempPatterns)
                p.Release();
            patterns.Clear();
            tempPatterns.Clear();
            tiles.Clear();
        }

        //local variable
        private static List<Pattern> temp = new List<Pattern>();
        public static void Add(Pattern pattern)
        {
            Pattern tempPattern;
            temp.Clear();
            if (pattern.permanent)
            {
                if (patterns.TryGetValue(pattern, out tempPattern))
                    Remove(tempPattern);

                foreach (Pattern s in tempPatterns)
                    if (pattern.tiles2D.Contains(s.tiles2D))
                        temp.Add(s);
                foreach (Pattern s in temp)
                    Remove(s);

                patterns.Add(pattern);
            }
            else
            {
                foreach (Pattern s in tempPatterns)
                    if (s.tiles2D.Contains(pattern.tiles2D))
                        temp.Add(s);
                foreach (Pattern s in temp)
                    Remove(s);

                tempPatterns.Add(pattern);
            }
        }

        public static void Remove(Pattern p)
        {
            if (p.permanent)
            {
                if (patterns.Contains(p))
                {
                    foreach (Tile2D tile in p.tiles2D)
                        tile.patternSet.Remove(p);

                    patterns.Remove(p);
                }
            }
            else
            {
                if (tempPatterns.Contains(p))
                {
                    foreach (Tile2D tile in p.tiles2D)
                        tile.tempPatternSet.Remove(p);

                    tempPatterns.Remove(p);
                }
            }
        }
    }
}