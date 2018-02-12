using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public static class Atlas
    {
        public class Slot : NesRect
        {
            public int slice;
            public int x, y;
            public bool flip;
            public TextureData tex;
            public Vector2 offset;
            public Vector2 texSize;

            public int NextX { get { return x + w; } }
            public int NextY { get { return y + h; } }

            public Slot(int _slice, int _x, int _y, int _w, int _h, bool _flip = false, TextureData _tex = null) : base(_w, _h)
            {
                slice = _slice;
                x = _x;
                y = _y;
                flip = _flip;
                tex = _tex;
                offset = new Vector2(x, y) / SIZE;
                if (tex != null)
                {
                    if (!flip)
                        texSize = new Vector2(tex.w, tex.h) / SIZE;
                    else
                        texSize = new Vector2(tex.h, tex.w) / SIZE;
                }
            }
        }

        public const int SIZE = 512;
        private const int SLICE_COUNT = 32;
        private const int MAX_SLOT_PER_FRAME = 1023;
        private static readonly TextureFormat format = TextureFormat.ARGB32;

        static public Texture2DArray atlas;
        private static Texture2D[] texList;

        private static LinkedList<Slot> freeSlots;
        private static Dictionary<TextureData, Slot> usedSlots;

        private static Slot[] newRegSlots;
        private static int head;
        private static volatile int tail;
        private static HashSet<int> unsyncedSlices;

        public static void Init()
        {
            atlas = new Texture2DArray(SIZE, SIZE, SLICE_COUNT, format, false);
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;
            atlas.Apply();
            texList = new Texture2D[SLICE_COUNT];
            for (int i = 0; i < texList.Length; i++)
            {
                texList[i] = new Texture2D(SIZE, SIZE, format, false);
                texList[i].filterMode = FilterMode.Point;
                texList[i].wrapMode = TextureWrapMode.Clamp;
            }

            freeSlots = new LinkedList<Slot>();

            usedSlots = new Dictionary<TextureData, Slot>();

            newRegSlots = new Slot[MAX_SLOT_PER_FRAME + 1];
            unsyncedSlices = new HashSet<int>();

            Reset();
        }

        public static void Reset()
        {
            usedSlots.Clear();
            freeSlots.Clear();
            for (int i = 0; i < SLICE_COUNT; i++)
                freeSlots.AddLast(new Slot(i, 0, 0, SIZE, SIZE));

            unsyncedSlices.Clear();
            head = tail = 0;
        }

        private static Slot FindFreeSlotFor(TextureData tex)
        {
            foreach(var node in freeSlots)
                if (node.CanContain(tex))
                    return node;
            throw new Exception("Out of texture pool");
        }

        private static void RegisterFreeSlot(Slot slot)
        {
            freeSlots.AddFirst(slot);
        }

        static public void Invalidate()
        {
            unsyncedSlices.Clear();
            int tailValue = tail;

            while (head != tailValue)
            {
                head = (head + 1) & MAX_SLOT_PER_FRAME;
                Slot slot = newRegSlots[head];
                newRegSlots[head] = null;

                TextureData tex = slot.tex;
                if (tex != null)
                {
                    var texture = texList[slot.slice];
                    for (int i = 0; i < tex.w; i++)
                        for (int j = 0; j < tex.h; j++)
                        {
                            if (tex.format == ColorFormat.Index)
                            {
                                Color c = new Color(0, 0, 0, tex.GetPixel(i, j) / 3f);
                                if (!slot.flip)
                                    texture.SetPixel(slot.x + i, slot.y + j, c);
                                else
                                    texture.SetPixel(slot.x + j, slot.y + i, c);
                            }
                            else
                            {
                                if (!slot.flip)
                                    texture.SetPixel(slot.x + i, slot.y + j, tex.GetColor(i, j));
                                else
                                    texture.SetPixel(slot.x + j, slot.y + i, tex.GetColor(i, j));
                            }
                        }
                    unsyncedSlices.Add(slot.slice);
                }
            }

            foreach (int i in unsyncedSlices)
            {
                texList[i].Apply();
                Graphics.CopyTexture(texList[i], 0, atlas, i);
            }
        }

        public static void Deregister(TextureData tex)
        {
            Slot slot = GetSlot(tex);
            if (slot != null)
            {
                slot.tex = null;
                usedSlots.Remove(tex);
                RegisterFreeSlot(slot);
            }
        }

        private static void Insert(this Slot slot, TextureData tex)
        {
            int w, h;
            bool rotated;

            freeSlots.Remove(slot);

            if (slot.min < tex.max)
            {
                if (slot.minIndex == tex.minIndex)
                    rotated = false;
                else
                    rotated = true;
            }
            else
            {
                if (slot.minIndex == tex.minIndex)
                    rotated = true;
                else
                    rotated = false;
            }

            if (rotated)
            {
                w = tex.h;
                h = tex.w;
            }
            else
            {
                w = tex.w;
                h = tex.h;
            }

            Slot regSlot = new Slot(slot.slice, slot.x, slot.y, w, h, rotated, tex);
            usedSlots[tex] = regSlot;
            tail = (tail + 1) & MAX_SLOT_PER_FRAME;
            newRegSlots[tail] = regSlot;

            if (regSlot.w < slot.w)
            {
                Slot freeSlot = new Slot(slot.slice, regSlot.NextX, slot.y, slot.w - regSlot.w, regSlot.h);
                RegisterFreeSlot(freeSlot);
            }

            if (regSlot.h < slot.h)
            {
                Slot freeSlot = new Slot(slot.slice, slot.x, regSlot.NextY, slot.w, slot.h - regSlot.h);
                RegisterFreeSlot(freeSlot);
            }
        }

        public static void Register(TextureData tex)
        {
            Slot slot = GetSlot(tex);
            if (slot == null) // new registration
            {
                slot = FindFreeSlotFor(tex);
                slot.Insert(tex);
            }
            else //update old slot
            {
                tail = (tail + 1) & MAX_SLOT_PER_FRAME;
                newRegSlots[tail] = slot;
            }
        }

        public static Slot GetSlot(TextureData tex)
        {
            Slot slot;
            usedSlots.TryGetValue(tex, out slot);
            return slot;
        }

        public static Vector2 Coord2Uv(TextureData tex, Vector2 coord)
        {
            Slot s = GetSlot(tex);
            if (s != null)
            {
                Vector2 c = s.flip == false ? coord : new Vector2(coord.y, coord.x);
                return c / SIZE;
            }
            return Vector2.zero;
        }
    }
}