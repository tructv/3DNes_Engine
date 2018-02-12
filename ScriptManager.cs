using Nes3D.Utils;
using System;

namespace Nes3D.Engine3D
{
    public partial class ScriptManager
    {
        public static ScriptManager Ins { get; private set; }

        public KeyedCollectionEx<string, Script> Scripts { get; set; }

        public static void Init()
        {
            Ins = new ScriptManager();
            Script.Init();
        }

        public ScriptManager()
        {
            Scripts = new KeyedCollectionEx<string, Script>(delegate (Script script) { return script.name; });
        }

        public Script CreateNewScript(string _name, string _source, string _tagStr, bool _enable = true, bool overwrite = true)
        {
            if (_name.Equals(string.Empty))
                return null;
            if (Scripts.Contains(_name))
            {
                if (overwrite)
                    Scripts.Remove(_name);
                else
                    return null;
            }

            Script t = new Script(_name, _source, _tagStr, _enable);
            t.Relink();
            Add(t);
            return t;
        }

        public int Count { get { return Scripts.Count; } }

        public void ChangeItemKey(Script s, string name)
        {
            Scripts.ChangeItemKey(s, name);
        }

        public void Add(Script s)
        {
            Scripts.Add(s);
        }

        public void Remove(Script s)
        {
            Scripts.Remove(s);
            s.UnLink();
        }

        public void Reset()
        {
            for (int i = Scripts.Count - 1; i >= 0; i--)
                Remove(Scripts[i]);
        }

        public Script GetScript(string name)
        {
            Script s;
            Scripts.TryGetValue(name, out s);
            return s;
        }

        public Script GetScript(int index)
        {
            if (index < Scripts.Count)
                return Scripts[index];
            else
                return null;
        }

        public void RunStart()
        {
            foreach (Script s in Scripts)
                s.RunStart();
        }

        public void RunEnd()
        {
            foreach (Script s in Scripts)
                s.RunEnd();
        }

        public void RunUpdate()
        {
            for (int i = 0; i < Scripts.Count; i++)
                Scripts[i].RunUpdate();

            var shapes = FrameManager.Ins.OfflineFrame.shapes3D;
            for (int i = 0; i < shapes.Count; i++)
                shapes[i].RunUpdateScript();
        }
    }
}