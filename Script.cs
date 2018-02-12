using Nes3D.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using XLua;

namespace Nes3D.Engine3D
{
#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else

    using LuaAPI = XLua.LuaDLL.Lua;
    using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
    using RealStatePtr = System.IntPtr;

#endif

    public partial class Script
    {
        #region static

        private enum Function
        { Start, Update, UpdateS, End, Count };

        public static readonly char[] separatos = { ',' };

        private static string[] FNames;
        public const string AllTag = "*";

        public static LuaEnv session;

        public static event Action<string> onNewError;
        public static event Action<string> onNewInput;
        public static event Action<string> onNewOutput;
        public static event Action onClear;

        public static void Init()
        {
            SettingManager.Ins.PropertyChanged += OnSettingChanged;

            const string initScript = @"Vector3 = CS.UnityEngine.Vector3
                                        Vector2 = CS.UnityEngine.Vector2
                                        Color = CS.UnityEngine.Color
                                        IntVector2 = CS.Nes3D.Utils.IntVector2
                                        IntVector3 = CS.Nes3D.Utils.IntVector3
                                       ";
            FNames = new string[(int)Function.Count];
            for (int i = 0; i < FNames.Length; i++)
                FNames[i] = ((Function)i).ToString();

            session = new LuaEnv();

            // overide default implementation of print function
            LuaAPI.lua_pushstdcallcfunction(session.L, Print);
            LuaAPI.xlua_setglobal(session.L, "print");

            DoString(initScript);

            AssignSetting();

            session.Global.Set("gamepad", Gamepads.Ins);
            session.Global.Set("gamepad1", Gamepads.Ins[0]);
            session.Global.Set("gamepad2", Gamepads.Ins[1]);

            session.Global.Set("Script", ScriptManager.Ins);
            session.Global.Set("Frame", FrameManager.Ins);

            session.Global.Set("clear", new Action(Clear));
        }

        private static void AssignSetting()
        {
            session.Global.Set("Setting", SettingManager.Ins);
            session.Global.Set("Light", SettingManager.Ins.Light);
            session.Global.Set("Layer", SettingManager.Ins.Layer);
            session.Global.Set("Clipping", SettingManager.Ins.Clipping);
        }

        private static void OnSettingChanged(object sender, PropertyChangedEventArgs args)
        {
            if (ReferenceEquals(args.PropertyName, SettingManager.LightId)
                || ReferenceEquals(args.PropertyName, SettingManager.LayerId)
                || ReferenceEquals(args.PropertyName, SettingManager.ClippingId)
                )
                AssignSetting();
        }

        public static void DoString(string text)
        {
            try
            {
                onNewInput?.Invoke(text);
                session.DoString(text, "console");
            }
            catch (LuaException e)
            {
                onNewError?.Invoke(e.Message);
            }
        }

        private static void Clear()
        {
            onClear();
        }

        [MonoPInvokeCallback(typeof(LuaCSFunction))]
        private static int Print(RealStatePtr L)
        {
            try
            {
                int n = LuaAPI.lua_gettop(L);
                string s = String.Empty;

                if (0 != LuaAPI.xlua_getglobal(L, "tostring"))
                {
                    return LuaAPI.luaL_error(L, "can not get tostring in print:");
                }

                for (int i = 1; i <= n; i++)
                {
                    LuaAPI.lua_pushvalue(L, -1);  /* function to be called */
                    LuaAPI.lua_pushvalue(L, i);   /* value to print */
                    if (0 != LuaAPI.lua_pcall(L, 1, 1, 0))
                    {
                        return LuaAPI.lua_error(L);
                    }
                    s += LuaAPI.lua_tostring(L, -1);

                    if (i != n) s += "\t";

                    LuaAPI.lua_pop(L, 1);  /* pop result */
                }
                onNewOutput.Invoke(s);
                return 0;
            }
            catch (System.Exception e)
            {
                return LuaAPI.luaL_error(L, "c# exception in print:" + e);
            }
        }

        #endregion static

        private Action start, update, end, updateShape;
        public string name;
        public bool enable;
        public bool error;

        private string source;
        public string Source
        {
            get { return source; }
            set
            {
                try
                {
                    source = value;
                    LoadSource();
                    RunStart();
                }
                catch (LuaException e)
                {
                    error = true;
                    start = update = null;
                    updateShape = null;
                    onNewError?.Invoke(e.Message);
                }
            }
        }

        private void LoadSource()
        {
            error = false;
            try
            {
                onNewInput?.Invoke(name);
                session.DoString(source, name);
                session.Global.Get(FNames[0], out start);
                session.Global.Get(FNames[1], out update);
                session.Global.Get(FNames[2], out updateShape);
                session.Global.Get(FNames[3], out end);
            }
            catch (Exception e)
            {
                error = true;
                start = update = updateShape = end = null;
                onNewError?.Invoke(e.Message);
            }
        }
    
        public string Name
        {
            get { return name; }
            set
            {
                if (!name.Equals(value))
                {
                    ScriptManager.Ins.ChangeItemKey(this, value);
                    name = value;
                }
            }
        }

        private string tagStr;
        public List<string> tags;
        public string TagStr
        {
            get { return tagStr; }
            set
            {
                tagStr = value;
                CreateTags();
                Relink();
            }
        }

        private void CreateTags()
        {
            if (tags == null)
                tags = new List<string>();
            else
                tags.Clear();
            foreach (var s in tagStr.Split(separatos))
                tags.Add(s.Trim());
        }

        public Script(string _name, string _source, string _tagStr, bool _enable)
        {
            name = _name;
            enable = _enable;
            source = _source;
            start = end = update = null;
            TagStr = _tagStr;
            Source = _source;
        }

        public override string ToString()
        {
            return "Script " + name + " with Tags: " + tagStr;
        }

        private void UpdateContext()
        {
            session.Global.Set("self", this);
            session.Global.Set("frame", FrameManager.Ins.frameCounter);
            session.Global.Set("time", FrameManager.Ins.time);
        }

        public void RunStart()
        {
            if (start != null)
                if (enable && !error)
                    try
                    {
                        UpdateContext();
                        start();
                    }
                    catch (LuaException e)
                    {
                        error = true;
                        onNewError(e.Message);
                    }
        }

        public void RunEnd()
        {
            if (end != null)
                if (enable && !error)
                    try
                    {
                        UpdateContext();
                        end();
                    }
                    catch (LuaException e)
                    {
                        error = true;
                        onNewError(e.Message);
                    }
        }

        public void RunUpdate()
        {
            if (update != null)
                if (enable & !error)
                    try
                    {
                        UpdateContext();
                        update();
                    }
                    catch (LuaException e)
                    {
                        error = true;
                        onNewError(e.Message);
                    }
        }

        public void RunUpdateShape(Pattern3D pattern)
        {
            if (updateShape != null)
                if (enable & !error)
                    try
                    {
                        UpdateContext();
                        session.Global.Set("shape", pattern);
                        session.Global.Set("shape2D", pattern.Shape2D);
                        updateShape();
                    }
                    catch (LuaException e)
                    {
                        error = true;
                        onNewError(e.Message);
                    }
        }

        public void Delete()
        {
            ScriptManager.Ins.Remove(this);
        }

        public void Relink()
        {
            foreach (Pattern p in PatternManager.patterns)
                foreach (Pattern3D p3D in p.pattern3DList)
                    p3D.LinkToScript(this);
            foreach (Pattern p in PatternManager.tempPatterns)
                foreach (Pattern3D p3D in p.pattern3DList)
                    p3D.LinkToScript(this);
        }

        public void UnLink()
        {
            foreach (Pattern p in PatternManager.patterns)
                foreach (Pattern3D p3D in p.pattern3DList)
                    p3D.linkedScript.Remove(this);

            foreach (Pattern p in PatternManager.tempPatterns)
                foreach (Pattern3D p3D in p.pattern3DList)
                    p3D.linkedScript.Remove(this);
        }

        public void Adjust(string _name, string code, string _tagStr)
        {
            Source = code;
            TagStr = _tagStr;
            name = _name;
        }
    }
}