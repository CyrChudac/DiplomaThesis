using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UnityEngine;

namespace GameCreatingCore.GamePathing.NavGraphs {
	//from https://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
    internal class LineIntersectionDecider
    {
        /// <param name="tolerance">The maximal slope diffence between 2 lines to consider them paralel.</param>
        public static bool HasIntersection(Vector2 L1f, Vector2 L1e, 
            Vector2 L2f, Vector2 L2e, double tolerance = 0.001)
            => FindFirstIntersection(L1f, L1e, L2f, L2e, tolerance) != null;

        /// <summary>
        /// Finds the first spot where line 1 is intersecting line 2. (the only solution with more intersetions is if the overlap)
        /// </summary>
        /// <param name="tolerance">The maximal slope diffence between 2 lines to consider them paralel.</param>
        public static Vector2? FindFirstIntersection(Vector2 L1f, Vector2 L1e, 
            Vector2 L2f, Vector2 L2e, double tolerance = 0.001)
        {
            float x1 = L1f.x, y1 = L1f.y;
            float x2 = L1e.x, y2 = L1e.y;

            float x3 = L2f.x, y3 = L2f.y;
            float x4 = L2e.x, y4 = L2e.y;

            // equations of the form x=c (two vertical lines) with overlapping
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance && Math.Abs(x1 - x3) < tolerance)
            {
                return GetForOverlaping(y1, y2, y3, y4, (Vector2?)L1f, L2f, L2e, null);
            }

            //equations of the form y=c (two horizontal lines) with overlapping
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance && Math.Abs(y1 - y3) < tolerance)
            {
                return GetForOverlaping(x1, x2, x3, x4, (Vector2?)L1f, L2f, L2e, null);
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

            float x, y;

            //lineA is vertical x1 = x2
            //slope will be infinity
            //so lets derive another solution
            if (Math.Abs(x1 - x2) < tolerance)
            {
                //compute slope of line 2 (m2) and c2
                float m2 = (y4 - y3) / (x4 - x3);
                float c2 = -m2 * x3 + y3;

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
                float m1 = (y2 - y1) / (x2 - x1);
                float c1 = -m1 * x1 + y1;

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
                float m1 = (y2 - y1) / (x2 - x1);
                float c1 = -m1 * x1 + y1;

                //compute slope of line 2 (m2) and c2
                float m2 = (y4 - y3) / (x4 - x3);
                float c2 = -m2 * x3 + y3;

                if(m1 == m2) {
                    if(c1 != c2)
                        return null;

                    if(IsInsideLine(L2f, L2e, x1, y1))
                        return L1f;

                    if(IsInsideLine(L1f, L1e, x3, y3)) {
                        if(IsInsideLine(L1f, L1e, x4, y4)) {
                            if((L1f - L2f).sqrMagnitude < (L1f - L2e).sqrMagnitude)
                                return L2f;
                            else
                                return L1f;
                        }
                        return L2f;
                    }
                    if(IsInsideLine(L1f, L1e, x4, y4)) {
                        return L2e;
                    }
                    return null;
                }

                //solving equations (3) & (4) => x = (c1-c2)/(m2-m1)
                //plugging x value in equation (4) => y = c2 + m2 * x
                x = (c1 - c2) / (m2 - m1);
                y = c2 + m2 * x;

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

            //x,y can intersect outside the line segment since line is infinitely long
            //so finally check if x, y is within both the line segments
            if (IsInsideLine(L1f, L1e, x, y) &&
                IsInsideLine(L2f, L2e, x, y))
            {
                return new Vector2(x, y);
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

        /// <summary>
        /// If the 2 are constant on either axis, what is the first point of overlap.
        /// Returns one of the arguments given the first overlap point.
        /// </summary>
        public static T GetForOverlaping<T>(float n1, float n2, float n3, float n4, 
            T line1StartIsFirst, T line2StartIsFirst, T line2EndIsFirst, T noOverlap) { 

            if(n3 < n1 && n4 > n1) //line 1 start inside line 2
                return line1StartIsFirst;
            if(n3 < n2 && n4 > n2) { //line 1 end inside line 2
                if(Math.Abs(n1 - n3) < Math.Abs(n1 - n4)) 
                    return line2StartIsFirst; //line 1 start behind the line 2 start
                else 
                    return line2EndIsFirst; //line 1 start behind the line 2 end
            }
            if((n3 < n1 && n3 > n2) || (n3 > n1 && n3 < n2)) { // line 2 is whole within line 1
                if(Math.Abs(n1 - n3) < Math.Abs(n1 - n4))
                    return line2StartIsFirst; //line 2 start is more toward line 1 start
                else
                    return line2EndIsFirst; //line 2 end is more toward line 1 start
            }

            return noOverlap; //the lines have no intersection
        }


        // Returns true if given point(x,y) is inside the given line segment
        private static bool IsInsideLine(Vector2 Lf, Vector2 Le, double x, double y)
        {
            return (x >= Lf.x && x <= Le.x
                        || x >= Le.x && x <= Lf.x)
                   && (y >= Lf.y && y <= Le.y
                        || y >= Le.y && y <= Lf.y);
        }

    }
}
