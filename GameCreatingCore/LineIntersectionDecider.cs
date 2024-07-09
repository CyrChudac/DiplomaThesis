using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UnityEngine;

namespace GameCreatingCore {
    public class LineIntersectionDecider
    {
        public const double defaultTolerance = 0.002D;
        public const double overlapMarginModifier = 2;
        public const double isOnLineMarginModifier = 5;

        /// <param name="tolerance">The maximal slope diffence between 2 lines to consider them paralel.</param>
        /// <param name="overlapIsIntersection">When true, two overlaping lines will return their intersection; 
        /// when false, they return null.</param>
        /// <param name="endsInclusive">When true, lines are considered as open interval,
        /// so ending points count as interrsections only for line overlaps.</param>
        public static bool HasIntersection(Vector2 L1f, Vector2 L1e, 
            Vector2 L2f, Vector2 L2e, bool overlapIsIntersection, bool endsInclusive, double tolerance = defaultTolerance)
            => FindFirstIntersection(L1f, L1e, L2f, L2e, overlapIsIntersection, endsInclusive, tolerance) != null;
        
	//from https://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
        /// <summary>
        /// Finds the first spot where line 1 is intersecting line 2. (the only solution with more intersetions is if the overlap)
        /// </summary>
        /// <param name="tolerance">The maximal slope diffence between 2 lines to consider them paralel.</param>
        /// <param name="overlapIsIntersection">When true, two overlaping lines will return their intersection; 
        /// when false, they return null.</param>
        /// <param name="endsInclusive">When true, lines are considered as open interval,
        /// so ending points count as interrsections only for line overlaps.</param>
        public static Vector2? FindFirstIntersection(Vector2 L1f, Vector2 L1e, 
            Vector2 L2f, Vector2 L2e, bool overlapIsIntersection, bool endsInclusive, double tolerance = defaultTolerance) {

            double x1 = L1f.x, y1 = L1f.y;
            double x2 = L1e.x, y2 = L1e.y;

            double x3 = L2f.x, y3 = L2f.y;
            double x4 = L2e.x, y4 = L2e.y;

            // equations of the form x=c (two vertical lines) with overlapping
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance && Math.Abs(x1 - x3) < tolerance)
            {
                if(overlapIsIntersection)
                    return GetForOverlaping(y1, y2, y3, y4, (Vector2?)L1f, L2f, L2e, null, isOnLineMarginModifier / 150);
                else
                    return null;
            }

            //equations of the form y=c (two horizontal lines) with overlapping
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance && Math.Abs(y1 - y3) < tolerance)
            {
                if(overlapIsIntersection)
                    return GetForOverlaping(x1, x2, x3, x4, (Vector2?)L1f, L2f, L2e, null, isOnLineMarginModifier / 150);
                else
                    return null;
            }

            //equations of the form x=c (two vertical parallel lines)
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance)
            {   
                //return no intersection
                return null;
            }

            //equations of the form y=c (two horizontal parallel lines)
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance)
            {
                //return no intersection
                return null;
            }

            //general equation of line is y = mx + c where m is the slope
            //assume equation of line 1 as y1 = m1x1 + c1 
            //=> -m1x1 + y1 = c1 ----(1)
            //assume equation of line 2 as y2 = m2x2 + c2
            //=> -m2x2 + y2 = c2 -----(2)
            //if line 1 and 2 intersect then x1=x2=x & y1=y2=y where (x,y) is the intersection point
            //so we will get below two equations 
            //-m1x + y = c1 --------(3)
            //-m2x + y = c2 --------(4)

            double x, y;

            //lineA is vertical x1 = x2
            //slope will be infinity
            //so lets derive another solution
            if (Math.Abs(x1 - x2) < tolerance)
            {
                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x1=c1=x
                //subsitute x=x1 in (4) => -m2x1 + y = c2
                // => y = c2 + m2x1 
                x = x1;
                y = c2 + m2 * x1;
            }
            //lineB is vertical x3 = x4
            //slope will be infinity
            //so lets derive another solution
            else if (Math.Abs(x3 - x4) < tolerance)
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x3=c3=x
                //subsitute x=x3 in (3) => -m1x3 + y = c1
                // => y = c1 + m1x3 
                x = x3;
                y = c1 + m1 * x3;
            }
            //lineA & lineB are not vertical 
            //(could be horizontal we can handle it with slope = 0)
            else
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;
                
                //solving equations (3) & (4) => x = (c1-c2)/(m2-m1)
                //plugging x value in equation (4) => y = c2 + m2 * x
                x = (c1 - c2) / (m2 - m1);
                y = c2 + m2 * x;

                //the 2 lines have the same slope
                if(FloatEquality.LessOrEqual(Math.Abs(m1 - m2), tolerance)) {
                    //the 2 lines overlap
                    if((!overlapIsIntersection))
                        return null;

                    if(FloatEquality.AreEqual(m1, m2)) {
                        if(!FloatEquality.AreEqual(c1, c2))
                            return null;
                    } else {
                        //we can use x, since m1 != m2
                        if(!FloatEquality.LessOrEqual(Math.Abs((m1 * x + c1) - (m2 * x + c2)), tolerance))
                            return null;
                    }

                    if(IsInsideLine(L2f, L2e, x1, y1, overlapMarginModifier))
                        return L1f;

                    if(IsInsideLine(L1f, L1e, x3, y3, overlapMarginModifier)) {
                        if(IsInsideLine(L1f, L1e, x4, y4, overlapMarginModifier)) {
                            if((L1f - L2f).sqrMagnitude < (L1f - L2e).sqrMagnitude)
                                return L2f;
                            else
                                return L1f;
                        }
                        return L2f;
                    }
                    if(IsInsideLine(L1f, L1e, x4, y4, overlapMarginModifier)) {
                        return L2e;
                    }
                    return null;
                }


                //verify by plugging intersection point (x, y)
                //in orginal equations (1) & (2) to see if they intersect
                //otherwise x,y values will not be finite and will fail this check
                if (!(Math.Abs(-m1 * x + y - c1) < tolerance
                    && Math.Abs(-m2 * x + y - c2) < tolerance))
                {
                    //return no intersection
                    return null;
                }
            }
            if(!endsInclusive) {
                if(FloatEquality.AreEqual(x, x1) && FloatEquality.AreEqual(y, y1))
                    return null;
                if(FloatEquality.AreEqual(x, x2) && FloatEquality.AreEqual(y, y2))
                    return null;
                if(FloatEquality.AreEqual(x, x3) && FloatEquality.AreEqual(y, y3))
                    return null;
                if(FloatEquality.AreEqual(x, x4) && FloatEquality.AreEqual(y, y4))
                    return null;
            }
            //x,y can intersect outside the line segment since line is infinitely long
            //so finally check if x, y is within both the line segments
            if (IsInsideLine(L1f, L1e, x, y, isOnLineMarginModifier) &&
                IsInsideLine(L2f, L2e, x, y, isOnLineMarginModifier))
            {
                return new Vector2((float)x, (float)y);
            }

            //return null (no intersection)
            return null;

        }
        
        
        //from https://stackoverflow.com/questions/23016676/line-segment-and-circle-intersection
        public static int FindLineCircleIntersections(Vector2 middle, float radius,
            Vector2 Lf, Vector2 Le, 
            out Vector2? intersection1, out Vector2? intersection2)
        {
            float dx, dy, A, B, C, det, t;
            float cx = middle.x;
            float cy = middle.y;

            dx = Le.x - Lf.x;
            dy = Le.y - Lf.y;

            A = dx * dx + dy * dy;
            B = 2 * (dx * (Lf.x - cx) + dy * (Lf.y - cy));
            C = (Lf.x - cx) * (Lf.x - cx) + (Lf.y - cy) * (Lf.y - cy) - radius * radius;

            det = B * B - 4 * A * C;

            intersection1 = null;
            intersection2 = null;

            if ((A <= 0.0000001) || (det < 0))
            {
                // No real solutions.
                return 0;
            }
            else if (det == 0)
            {
                // One solution.
                t = -B / (2 * A);
                var x = Lf.x + t * dx;
                var y = Lf.y + t * dy;
                if(IsInsideLine(Lf, Le, x, y)) {
                    intersection1 = new Vector2(x, y);
                    return 1;
                } else {
                    return 0;
                }
            }
            else
            {
                // Two solutions.
                t = (float)((-B + Math.Sqrt(det)) / (2 * A));
                int results = 0;
                var x = Lf.x + t * dx;
                var y = Lf.y + t * dy;
                if(IsInsideLine(Lf, Le, x, y)) {
                    intersection1 = new Vector2(x, y);
                    results++;
                }
                t = (float)((-B - Math.Sqrt(det)) / (2 * A));
                x = Lf.x + t * dx;
                y = Lf.y + t * dy;
                if(IsInsideLine(Lf, Le, x, y)) {
                    intersection2 = new Vector2(x, y);
                    results++;
                }
                return results;
            }
        }

        //from https://forum.unity.com/threads/how-do-i-find-the-closest-point-on-a-line.340058/
        public static Vector2 NearestPointOnLine(Vector2 lineStart, Vector2 lineEnd, Vector2 point)
        {
            var lineDir = (lineEnd - lineStart).normalized;//this needs to be a unit vector
            var v = point - lineStart;
            var d = Vector3.Dot(v, lineDir);
            var res = lineStart + lineDir * d;
            if(IsInsideLine(lineStart, lineEnd, res))
                return res;
            if((res - lineStart).sqrMagnitude < (res * lineEnd).sqrMagnitude)
                return lineStart;
            else
                return lineEnd;
        }
        
        /// <summary>
        /// If there are 2 lines on the same axis, what is the first point of their overlap.
        /// Returns one of the arguments given the first overlap point.
        /// </summary>
        public static T GetForOverlaping<T>(double n1, double n2, double n3, double n4, 
            T line1StartIsFirst, T line2StartIsFirst, T line2EndIsFirst, T noOverlap, double endTolerance = 0) { 

            if(FloatEquality.LessOrEqual(n3, n1) && FloatEquality.MoreOrEqual(n4, n1)
                || FloatEquality.LessOrEqual(n4, n1) && FloatEquality.MoreOrEqual(n3, n1)) //line 1 start inside line 2
                return line1StartIsFirst;
            if(FloatEquality.LessOrEqual(n3, n2) && FloatEquality.MoreOrEqual(n4, n2) 
                || FloatEquality.LessOrEqual(n4, n2) && FloatEquality.MoreOrEqual(n3, n2)) { //line 1 end inside line 2
                if(Math.Abs(n1 - n3) < Math.Abs(n1 - n4)) 
                    return line2StartIsFirst; //line 1 start behind the line 2 start
                else 
                    return line2EndIsFirst; //line 1 start behind the line 2 end
            }
            if((FloatEquality.LessOrEqual(n3, n1) && FloatEquality.MoreOrEqual(n3, n2)) 
                || (FloatEquality.MoreOrEqual(n3, n1) && FloatEquality.LessOrEqual(n3, n2))) { // line 2 is whole within line 1
                if(Math.Abs(n1 - n3) < Math.Abs(n1 - n4))
                    return line2StartIsFirst; //line 2 start is more toward line 1 start
                else
                    return line2EndIsFirst; //line 2 end is more toward line 1 start
            }
            if(FloatEquality.LessOrEqual(Math.Abs(n1 - n3), endTolerance)
                || FloatEquality.LessOrEqual(Math.Abs(n1 - n4), endTolerance))
                return line1StartIsFirst;
            if(FloatEquality.LessOrEqual(Math.Abs(n2 - n3), endTolerance))
                return line2StartIsFirst;
            if(FloatEquality.LessOrEqual(Math.Abs(n2 - n4), endTolerance))
                return line2EndIsFirst;
            return noOverlap; //the lines have no intersection
        }

        
        private static bool IsInsideLine(Vector2 Lf, Vector2 Le, Vector2 point, double tolerMult = 1)
            => IsInsideLine(Lf, Le, point.x, point.y, tolerMult);

        // Returns true if given vector2(x,y) is inside the given line segment 
        private static bool IsInsideLine(Vector2 Lf, Vector2 Le, double x, double y, double tolerMult = 1)
        {
            return      (FloatEquality.MoreOrEqual(x, Lf.x, tolerMult) && FloatEquality.LessOrEqual(x, Le.x, tolerMult)
                      || FloatEquality.MoreOrEqual(x, Le.x, tolerMult) && FloatEquality.LessOrEqual(x, Lf.x, tolerMult))
                     && (FloatEquality.MoreOrEqual(y, Lf.y, tolerMult) && FloatEquality.LessOrEqual(y, Le.y, tolerMult)
                      || FloatEquality.MoreOrEqual(y, Le.y, tolerMult) && FloatEquality.LessOrEqual(y, Lf.y, tolerMult));
        }
    }
}
