using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nes3D.Engine3D
{
    public class Tracker
    {
        private static HashSet<Pattern> includedPatterns;
        private static HashSet<Pattern> excludedPatterns;
        
        Vector3 pos;
        Frame frame;
        Shape lastShape;
        public event Action<Pattern> onNewPatternTracked = delegate { };
        bool single = true;
        bool sp;
        bool running;

        public Tracker(bool _single = true)
        {
            single = _single;
            includedPatterns = new HashSet<Pattern>();
            excludedPatterns = new HashSet<Pattern>();
            running = false;
        }

        public void Start(Shape shape)
        {
            if (shape != null)
            {
                includedPatterns.Clear();
                excludedPatterns.Clear();
                pos = shape.center;
                includedPatterns.Add(shape.refPattern);
                sp = !shape.bg;
                running = true;
                lastShape = shape;
            }
            else
                throw new Exception("Shape to be tracked should not be null.");
        }

        public bool Running
        {
            get { return running; }
        }

        public void Stop()
        {
            running = false;
        }

        public Shape Track(Frame _frame)
        {
            if (running)
            {
                Shape s = null;
                frame = _frame;
                if (single)
                    s = TrackSingle();
                else
                    s = TrackMulti();
                return s;
            }
            else
                return null;
        }

        public Shape TrackMulti()
        {
            Shape shape;
            Shape newShape = null;
            float dis = 8;
            float newDis;
            bool newPaternTracked = true;
            int start, end;
            if (sp)
            {
                start = frame.bgShapeCount;
                end = frame.shapeCount;
            }
            else
            {
                start = 0;
                end = frame.bgShapeCount;
            }


            if (lastShape != null)
            {
                for (int i = start; i < end; i++)
                {
                    shape = frame.shapes[i];
                    newDis = Vector3.Distance(pos, shape.center);
                    if (includedPatterns.Contains(shape.refPattern) && dis > newDis)
                    {
                        newShape = shape;
                        newPaternTracked = false;
                        break;
                    }
                }

                if (newShape == null)
                for (int i = start; i < end; i++)
                {
                    shape = frame.shapes[i];
                    newDis = Vector3.Distance(pos, shape.center);

                    if (dis > newDis)
                    {
                        newShape = shape;
                        dis = newDis;
                    }
                }
            }
            else
            {
                for (int i = start; i < end; i++)
                {
                    shape = frame.shapes[i];
                    if (includedPatterns.Contains(shape.refPattern))
                    {
                        newShape = shape;
                        newPaternTracked = false;
                        break;
                    }
                }
            }

            lastShape = newShape;
            if (lastShape != null)
            {
                pos = lastShape.center;
                if (newPaternTracked)
                {
                    includedPatterns.Add(lastShape.refPattern);
                    onNewPatternTracked(lastShape.pattern);
                }
            }

            return newShape;
        }

        public Shape TrackSingle()
        {
            Shape shape;
            Shape newShape = null;
            float dis = 9;
            float newDis;
            bool newPaternTracked = true;
            int start, end;

            if (sp)
            {
                start = frame.bgShapeCount;
                end = frame.shapeCount;
            }
            else
            {
                start = 0;
                end = frame.bgShapeCount;
            }

            for (int i = start; i < end; i++)
            {
                shape = frame.shapes[i];
                if (includedPatterns.Contains(shape.refPattern))
                {
                    for (int j = i; j >= 0; j--)
                        if (!includedPatterns.Contains(frame.shapes[j].refPattern))
                            excludedPatterns.Add(frame.shapes[j].refPattern);

                    if (newShape != null)
                        excludedPatterns.Add(newShape.refPattern);

                    newShape = shape;
                    newPaternTracked = false;
                    break;
                }
                else
                {
                    if (!excludedPatterns.Contains(shape.refPattern))
                    {
                        if (dis > (newDis = Vector3.Distance(pos, shape.center)))
                        {
                            if (newShape != null)
                                excludedPatterns.Add(newShape.refPattern);
                            newShape = shape;
                            dis = newDis;
                        }
                        else
                            excludedPatterns.Add(shape.refPattern);
                    }
                }
            }

            if (newShape != null)
            {
                pos = newShape.center;
                if (newPaternTracked)
                    includedPatterns.Add(newShape.refPattern);
            }

            return newShape;
        }
    }
}
