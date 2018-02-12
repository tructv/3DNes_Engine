using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace Nes3D.Engine3D
{
    public enum SelectionMode { Shape3D, Tile };

    public class FrameManager /*: IFrameManager*/
    {
        public const int BUFFER_COUNT = 2;
        public static FrameManager Ins { get; private set; }

        public static void Init(VideoInfo info)
        {
            Ins = new FrameManager(info);
        }

        private Frame[] frames = new Frame[BUFFER_COUNT];
        public Frame OnlineFrame { get { return frames[onlineIndex]; } }
        public Frame OfflineFrame { get { return frames[offlineIndex]; } }

        private int onlineIndex;
        private int offlineIndex;

        private bool offlineWorking;
        private bool onlineWorking;

        public SelectionMode Mode { get { return SettingManager.Ins.SelectionMode; } set { SettingManager.Ins.SelectionMode = value; } }

        public Tracker tracker;

        public void Reset()
        {
            ClearSelection();
            Mode = SelectionMode.Shape3D;
            foreach (var frame in frames)
                frame.Reset();
            tracker.Stop();
        }

        private Pattern3D trackShape;

        public Pattern3D SelectedShape3D
        {
            get
            {
                if ((SelectedShape3Ds != null) && (SelectedShape3Ds.Count > 0))
                    return SelectedShape3Ds[0];
                else
                    return null;
            }
            set
            {
                SelectedShape3Ds.Clear();
                if (value != null)
                    SelectedShape3Ds.Add(value);
            }
        }

        public List<Pattern3D> SelectedShape3Ds
        {
            get; private set;
        }

        public List<TileObject> SelectedTiles
        {
            get; private set;
        }

        public event Action onNewPatternTracked;

        private FrameManager(VideoInfo info)
        {
            tracker = new Tracker(false);

            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = new Frame(info);
                frames[i].Reset();
            }

            Mode = SelectionMode.Shape3D;
            SelectedShape3Ds = new List<Pattern3D>();
            SelectedTiles = new List<TileObject>();

            onlineIndex = 0;
            offlineIndex = 0;
            offlineWorking = false;
            onlineWorking = false;
        }

        public Frame TakeNewOnlineFrame()
        {
            lock (this)
            {
                onlineWorking = true;
                if (!offlineWorking)
                    onlineIndex = offlineIndex;
                else
                    onlineIndex = 1 - offlineIndex;
                Tracking();
                return OnlineFrame;
            }
        }

        public void ReleaseOnlineFrame()
        {
            lock (this)
            {
                onlineWorking = false;
            }
        }

        public Frame TakeNewOfflineFrame()
        {
            lock (this)
            {
                if (offlineIndex == onlineIndex || !onlineWorking)
                    offlineIndex = 1 - offlineIndex;
                OfflineFrame.Reset();
                offlineWorking = true;
                return OfflineFrame;
            }
        }

        public void ReleaseOfflineFrame()
        {
            lock (this)
            {
                offlineWorking = false;
            }
        }

        public void InteractWithTile(TileObject info)
        {
            if (tracker.Running)
                return;
            if (Mode == SelectionMode.Shape3D)
            {
                if (!IsSelected(info))
                {
                    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                    {
                        if (SelectedShape3Ds.Count == 0 || SelectedShape3D.shape.bg == info.shape.bg)
                            SelectedShape3Ds.Insert(0, info.pattern3D);
                    }
                    else
                        SelectedShape3D = info.pattern3D;
                }
                else
                {
                    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                        SelectedShape3Ds.Remove(info.pattern3D);
                    else
                        SelectedShape3D = info.pattern3D;
                }
            }
            else
            {
                if (!IsSelected(info))
                {

                    {
                        if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                        {
                            if (SelectedTiles.Count == 0 || SelectedTiles[0].shape.bg == info.shape.bg)
                                SelectedTiles.Add(info);
                        }
                        else
                        {
                            SelectedTiles.Clear();
                            SelectedTiles.Add(info);
                        }
                    }
                }
                else
                {
                    if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
                        SelectedTiles.Remove(info);
                    else
                    {
                        SelectedTiles.Clear();
                        SelectedTiles.Add(info);
                    }
                }
            }
        }

        public bool IsSelected(TileObject tile)
        {
            if (Mode == SelectionMode.Shape3D)
                return SelectedShape3Ds.Find(x => x == tile.pattern3D) != null;
            else
                return SelectedTiles.Find(x => x == tile) != null;
        }

        public bool SomethingIsSelected()
        {
            if (Mode == SelectionMode.Shape3D)
            {
                return (SelectedShape3Ds.Count > 0);
            }
            else
            {
                return (SelectedTiles.Count > 0);
            }
        }

        public void ClearSelection()
        {
            SelectedTiles.Clear();
            SelectedShape3Ds.Clear();
        }

        private List<Pattern3D> temp = new List<Pattern3D>();

        private void Tracking()
        {
            if (tracker.Running)
                TrackAutoAdjustment();
            else
                TrackSelection();
        }

        private void TrackAutoAdjustment()
        {
            var s = tracker.Track(OnlineFrame);
            SelectedShape3D = s != null ? s[0] : null;
        }

        private void TrackSelection()
        {
            int index;
            if (Mode == SelectionMode.Shape3D)
            {
                bool newPatternTracked = false; ;
                temp.Clear();
                for (int i = 0; i < SelectedShape3Ds.Count; i++)
                {
                    Shape oldShape = SelectedShape3Ds[i].shape;
                    Shape newShape = OnlineFrame.Track(oldShape);
                    if (newShape == null)
                        continue;
                    if (oldShape.refPattern != newShape.refPattern)
                        newPatternTracked = true;

                    if ((index = Mathf.Min(SelectedShape3Ds[i].index, newShape.InsCount - 1)) != -1)
                        if (!temp.Contains(newShape[index]))
                            temp.Add(newShape[index]);
                }
                SelectedShape3Ds.Clear();
                SelectedShape3Ds.AddRange(temp);

                if (newPatternTracked)
                    onNewPatternTracked();
            }
            else
            {
            }
        }

        public void MoveNext()
        {
            int index = SelectedShape3D.index;
            if (index == SelectedShape3D.shape.InsCount - 1)
            {
                index = OnlineFrame.shapes.FindIndex(x => x == SelectedShape3D.shape);
                if (index != -1)
                {
                    index++;
                    if (index == OnlineFrame.shapes.Count)
                        index = 0;
                    Shape s = OnlineFrame.shapes[index];
                    SelectedShape3D = s[0];
                }
                else
                    SelectedShape3D = null;
            }
            else
            {
                SelectedShape3D = SelectedShape3D.shape[index + 1];
            }
        }

        public void MoveBack()
        {
            int index = SelectedShape3D.index;
            if (index == 0)
            {
                index = OnlineFrame.shapes.FindIndex(x => x == SelectedShape3D.shape);
                if (index != -1)
                {
                    index--;
                    if (index == -1)
                        index = OnlineFrame.shapes.Count - 1;
                    Shape s = OnlineFrame.shapes[index];
                    SelectedShape3D = s[s.InsCount - 1];
                }
                else
                    SelectedShape3D = null;
            }
            else
            {
                SelectedShape3D = SelectedShape3D.shape[index - 1];
            }
        }

        public List<Pattern3D> GetShapesWithTag(string tag)
        {
            return OfflineFrame.GetShapes(tag);
        }

        public Pattern3D GetShapeWithTag(string tag)
        {
            return OfflineFrame.GetShape(tag);
        }

        public Pattern3D GetShape(int index)
        {
            return OfflineFrame.shapes3D[index];
        } 

        public int shapeCount { get { return OfflineFrame.shape3DCount; } }
        public int frameCounter { get { return OfflineFrame.frameCounter; } }
        public float time { get { return OfflineFrame.time; } }
    }
}