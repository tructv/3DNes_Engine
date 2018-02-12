using Nes3D.Utils;
using System;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public class Engine3D
    {
        private static string dataPath = string.Empty;
        private static string backupPath = string.Empty;

        public static void Init(VideoInfo info)
        {
            PaletteManager.Init();
            PatternManager.Init();
            Atlas.Init();
            FrameManager.Init(info);
            Converter.Init(info);
            ScriptManager.Init();

            Emulator.Ins.engine.onStart += OnStart;
            Emulator.Ins.engine.onEnd += OnEnd;
            Emulator.Ins.engine.onStartFrame += OnStartFrame;
            Emulator.Ins.engine.onEndFrame += OnEndFrame;
            Emulator.Ins.engine.onResume += OnResume;
        }

        private static void OnResume()
        {
            Misc.RunInMainThread(() => FrameManager.Ins.ClearSelection());
        }

        private static void OnStart()
        {
            Load3DN();
            ScriptManager.Ins.RunStart();
        }

        private static void OnEnd()
        {
            ScriptManager.Ins.RunEnd();
            Reset();
        }

        private static void OnStartFrame()
        {
            FrameManager.Ins.TakeNewOfflineFrame();
        }

        private static void OnEndFrame()
        {
            Converter.Process();
            ScriptManager.Ins.RunUpdate();
            FrameManager.Ins.OfflineFrame.CalculatePos();
            FrameManager.Ins.ReleaseOfflineFrame();
        }

        public static void Reset()
        {
            Misc.RunInMainThread(() =>
            {
                FrameManager.Ins.Reset();
                PaletteManager.Reset();
                PatternManager.Reset();
                Atlas.Reset();
                ScriptManager.Ins.Reset();
                GC.Collect();
            });
            Thread.Sleep(100);
        }
    }
}