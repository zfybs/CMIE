using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace FaceWall
{
    /// <summary>
    /// 将曲线集合按指定的需求进行重新排列等构造
    /// </summary>
    public static class CurvesFormator
    {
        #region  曲线连续性-CurveArrArray

        /// <summary> 将 CurveArray 中的曲线进行重新排列，以组成连续的曲线链 </summary>
        /// <param name="curveArr"> 要进行重组的曲线集合 </param>
        /// <returns>如果不能形成连续的曲线链，则返回 null </returns>
        public static CurveArrArray GetContiguousCurvesFromCurveArrArray(CurveArrArray curveArr)
        {
            CurveArrArray curveOut = new CurveArrArray();
            foreach (CurveArray cArr in curveArr)
            {
                CurveArray conti;
                GetContiguousCurvesFromCurveArray(cArr, out conti);
                if (conti == null)
                {
                    return null;
                }
                curveOut.Append(conti);
            }
            return curveOut;
        }

        /// <summary> 将 CurveArray 中的曲线进行重新排列，以组成连续的曲线链 </summary>
        /// <param name="curveArr"> 要进行重组的曲线集合 </param>
        /// <param name="contigeousCurves"> 如果不能形成连续的曲线链，则返回 null </param>
        public static void GetContiguousCurvesFromCurveArray(CurveArray curveArr, out IList<Curve> contigeousCurves)
        {
            IList<Curve> curves = curveArr.Cast<Curve>().ToList();
            // Build a list of curves from the curve elements
            contigeousCurves = CurvesFormator.GetContiguousCurvesFromCurves(curves);
        }
        
        /// <summary> 将 CurveArray 中的曲线进行重新排列，以组成连续的曲线链 </summary>
        /// <param name="curveArr"> 要进行重组的曲线集合 </param>
        /// <param name="contigeousCurves"> 如果不能形成连续的曲线链，则返回 null，从外部调用来看，此参数可以与第一个参数curveArr赋同一个实参。 </param>
        public static void GetContiguousCurvesFromCurveArray(CurveArray curveArr, out CurveArray contigeousCurves)
        {
            IList<Curve> curves;

            // Build a list of curves from the curve elements
            CurvesFormator.GetContiguousCurvesFromCurveArray(curveArr, out curves);

            contigeousCurves = new CurveArray();
            if (curves == null)
            {
                contigeousCurves = null;
                return;
            }
            foreach (Curve c in curves)
            {
                contigeousCurves.Append(c);
            }
        }

        #endregion

        #region  曲线连续性-EdgeArrayArray

        /// <summary> 将 Face中的边缘线集合 进行重新排列，以组成连续的曲线链，如果不能全部形成连续的曲线链，则返回null  </summary>
        /// <param name="edgeLoops"> 一个Face中的边缘线集合，对于开洞的Face，其中有多个封闭的曲线集合 </param>
        /// <returns>如果不能形成连续的曲线链，则返回 null </returns>
        public static CurveArrArray GetContiguousCurvesFromEdgeArrArray(EdgeArrayArray edgeLoops)
        {
            CurveArrArray curveOut = new CurveArrArray();
            foreach (EdgeArray eArr in edgeLoops)
            {
                CurveArray conti;
                GetContiguousCurvesFromCurveArray(eArr, out conti);

                if (conti == null)
                {
                    return null;
                }
                curveOut.Append(conti);
            }
            return curveOut;
        }


        /// <summary> 将 Face中的边缘线集合 进行重新排列，以组成连续的曲线链，如果不能全部形成连续的曲线链，则返回null  </summary>
        /// <param name="edgeLoops"> 一个Face中的边缘线集合，对于开洞的Face，其中有多个封闭的曲线集合 </param>
        /// <param name="curveLoops"> 输出的连续曲线链集合， </param>
        /// <returns>如果不能形成连续的曲线链，则返回 null </returns>
        public static void GetContiguousCurvesFromEdgeArrArray(EdgeArrayArray edgeLoops, out IList<CurveLoop> curveLoops)
        {
            curveLoops = new List<CurveLoop>();
            foreach (EdgeArray eArr in edgeLoops)
            {
                CurveArray conti;
                GetContiguousCurvesFromCurveArray(eArr, out conti);

                if (conti == null)
                {
                    curveLoops = null;
                    return;
                }

                CurveLoop cl = new CurveLoop();
                foreach (Curve c in conti)
                {
                    cl.Append(c);
                }

                // The loops must be closed, coplanar(共面的), and without intersections, self-intersections, or degeneracies.
                if (cl.IsOpen())
                {
                    curveLoops = null;
                    return;
                }

                curveLoops.Add(cl);
            }

        }


        /// <summary> 将 CurveArray 中的曲线进行重新排列，以组成连续的曲线链，如果不能连续，则返回null </summary>
        /// <param name="edgeArr"> 要进行重组的曲线集合 </param>
        /// <param name="contigeousCurves"> 如果不能形成连续的曲线链，则返回 null </param>
        public static void GetContiguousCurvesFromCurveArray(EdgeArray edgeArr, out CurveArray contigeousCurves)
        {
            IList<Curve> curves = (from Edge eg in edgeArr select eg.AsCurve()).ToList();

            // Build a list of curves from the curve elements
            if (curves.Count == 0)
            {
                contigeousCurves = null;
                return;
            }

            var contCurves = CurvesFormator.GetContiguousCurvesFromCurves(curves);
            if (contCurves == null)
            {
                contigeousCurves = null;
                return;
            }
            // 将结果进行导出
            contigeousCurves = new CurveArray();

            foreach (var c in contCurves)
            {
                contigeousCurves.Append(c);
            }
        }

        #endregion

        #region  曲线连续性-Selection

        /// <summary>
        /// 从选择的Curve Elements中，获得连续排列的多段曲线（不一定要封闭）。
        /// </summary>
        /// <param name="doc">曲线所在文档</param>
        /// <param name="SelectedCurves">多条曲线元素所对应的Reference，可以通过Selection.PickObjects返回。
        /// 注意，SelectedCurves中每一条曲线都必须是有界的（IsBound），否则，其GetEndPoint会报错。</param>
        /// <returns>如果输入的曲线可以形成连续的多段线，则返回重新排序后的多段线集合；
        /// 如果输入的曲线不能形成连续的多段线，则返回Nothing！</returns>
        public static IList<Curve> GetContiguousCurvesFromSelectedCurveElements(Document doc, IList<Reference> SelectedCurves)
        {
            IList<Curve> curves = new List<Curve>();

            // Build a list of curves from the curve elements
            foreach (Reference reference in SelectedCurves)
            {
                CurveElement curveElement = doc.GetElement(reference) as CurveElement;
                curves.Add(curveElement.GeometryCurve.Clone());
            }
            //
            curves = CurvesFormator.GetContiguousCurvesFromCurves(curves);
            return curves;
        }

        #endregion

        /// <summary> 
        /// 从指定的Curve集合中中，获得连续排列的多段曲线（不一定要封闭）。如果不连续，则返回Nothing。
        /// </summary>
        /// <param name="curves">多条曲线元素所对应的集合
        /// 注意，curves 集合中每一条曲线都必须是有界的（IsBound），否则，其 GetEndPoint 会报错。</param>
        /// <returns>如果输入的曲线可以形成连续的多段线，则返回重新排序后的多段线集合；
        /// 如果输入的曲线不能形成连续的多段线，则返回Nothing！</returns>
        /// <remarks>GetContiguousCurvesFromCurves2与函数GetContiguousCurvesFromCurves2的功能完全相同，只是GetContiguousCurvesFromCurves1是
        /// 通过数值的方法来实现，而GetContiguousCurvesFromCurves2是通过类与逻辑的判断来实现。所以GetContiguousCurvesFromCurves1的执行速度可能会快一点点，
        /// 而GetContiguousCurvesFromCurves2的扩展性会好得多。</remarks>
        public static IList<Curve> GetContiguousCurvesFromCurves(IList<Curve> curves)
        {
            Dictionary<int, Curve> CurvesLeft = new Dictionary<int, Curve>();
            ContiguousCurveChain cc = null;
            //
            if (curves.Count == 1)
            {
                // 只有一条曲线，不管其是否封闭，它肯定是一条连续曲线。
                // 如果是封闭的圆或者椭圆，则将其脱开
                Curve c = curves[0];
                if ((!c.IsBound) && ((c is Arc) || (c is Ellipse)))
                {
                    c.MakeBound(0, 2 * Math.PI);
                }
                // 将改造后的结果直接返回
                List<Curve> l = new List<Curve>();
                l.Add(c);
                return l;
            }
            else if (curves.Count > 1)
            {
                cc = new ContiguousCurveChain(curves[0]);
                for (var i = 1; i <= curves.Count - 1; i++)
                {
                    // 所有曲线集合中，除了已经构造为连续曲线链的曲线外，还剩下的待进行匹配的曲线。
                    // 其key值为曲线在所有曲线集合中的下标值。
                    CurvesLeft.Add(i, curves[Convert.ToInt32(i)]);
                }
            }
            else
            {
                return null;
            }
            //
            int? foundedIndex = null;
            // 先向右端延伸搜索
            var leftCount = CurvesLeft.Count;  // 将CurvesLeft.Count值固定下来，因为在后面的匹配中，可能会执行 CurvesLeft.Remove，这样的话 CurvesLeft.Count 的值会发生变化。
            for (var i = 0; i < leftCount; i++)
            {
                foundedIndex = cc.CheckForward(CurvesLeft);
                if (foundedIndex != null) // 说明找到了相连的曲线
                {
                    cc.ConnectForward(CurvesLeft[foundedIndex.Value]);
                    CurvesLeft.Remove(foundedIndex.Value);  // 此时 CurvesLeft.Count 的值变小了
                }
                else // 说明剩下的曲线中，没有任何一条曲线能与当前连续链的右端相连了
                {
                    break;
                }
            }

            // 再向左端延伸搜索
            leftCount = CurvesLeft.Count;  // 将CurvesLeft.Count值固定下来，因为在后面的匹配中，可能会执行 CurvesLeft.Remove，这样的话 CurvesLeft.Count 的值会发生变化。
            for (var i = 0; i <= leftCount - 1; i++)
            {
                foundedIndex = cc.CheckBackward(CurvesLeft);
                if (foundedIndex != null) // 说明找到了相连的曲线
                {
                    cc.ConnectBackward(CurvesLeft[foundedIndex.Value]);
                    CurvesLeft.Remove(foundedIndex.Value);
                }
                else // 说明剩下的曲线中，没有任何一条曲线能与当前连续链的右端相连了
                {
                    break;
                }
            }

            //
            if (cc.Curves.Count != curves.Count)
            {
                return default(IList<Curve>);
            }
            return cc.Curves;
        }
    }

    /// <summary>
    /// 模拟一段从左向右的连续性曲线链集合，集合中的第一个元素表示最左边的曲线；end0 与 end1 分别代表整个连续曲线段的最左端点与最右端点。
    /// </summary>
    public class ContiguousCurveChain
    {
        private List<Curve> CurvesChain;

        /// <summary> 连续性曲线链，此集合中的曲线肯定是首尾相连的。且第一个元素表示最左边的那条曲线。 </summary>
        public List<Curve> Curves
        {
            get { return CurvesChain; }
        }

        /// <summary> 整个连续性曲线链的最左端点的坐标 </summary>
        private XYZ end0;

        /// <summary> 整个连续性曲线链的最右端点的坐标 </summary>
        private XYZ end1;

        /// <summary>
        /// 从一条曲线开始构造连续曲线链
        /// </summary>
        /// <param name="BaseCurve"></param>
        public ContiguousCurveChain(Curve BaseCurve)
        {
            CurvesChain = new List<Curve>();
            CurvesChain.Add(BaseCurve);
            if (BaseCurve.IsBound)
            {
                this.end0 = BaseCurve.GetEndPoint(0);
                this.end1 = BaseCurve.GetEndPoint(1);
            }
            else
            {
                throw new InvalidOperationException("此曲线为无界曲线，不能用以构造曲线链！");
            }
        }

        #region 检测连续性

        /// <summary>
        /// 从一组曲线中找到一条与连续链右端点相接的曲线，并且在适当的情况下，对搜索到的曲线进行反转。
        /// </summary>
        /// <param name="curves">进行搜索的曲线集合。在此函数中，可能会对连接到的那条曲线进行反转。
        /// IDictionary中的键值表示每一条Curve的Id值，这个值并不一定是从1开始递增的。
        /// </param>
        /// <returns>
        /// 与连续曲线链的最右端相连的那一条曲线在输入的曲线集合中所对应的Id键值。
        /// 如果没有找到连接的曲线，则返回Nothing！</returns>
        public Nullable<int> CheckForward(Dictionary<int, Curve> curves)
        {
            // 搜索到的那一条曲线所对应的Id值
            Nullable<int> ConnectedCurveIndex = new Nullable<int>();
            ConnectedCurveIndex = null; // 如果没有找到，则返回Nothing

            //
            var Ids = curves.Keys.ToArray();
            var Cvs = curves.Values;
            //
            int tempId = 0;
            Curve tempCurve = default(Curve);

            // 从曲线集合中找出起点与上面的终点重合的曲线 。 find curve with start point = end point
            for (int i = 0; i <= Ids.Length - 1; i++)
            {
                tempId = Ids[i];
                tempCurve = curves[tempId];

                // Is there a match end->start, if so this is the next curve
                if (GeoHelper.IsAlmostEqualTo(tempCurve.GetEndPoint(0), this.end1, GeoHelper.VertexTolerance))
                {
                    return tempId;
                    // Is there a match end->end, if so, reverse the next curve
                }
                else if (GeoHelper.IsAlmostEqualTo(tempCurve.GetEndPoint(1), this.end1, GeoHelper.VertexTolerance))
                {
                    // 将曲线进行反转
                    curves[tempId] = tempCurve.CreateReversed(); // 将反转后的曲线替换掉原来的曲线
                    return tempId;
                }
            }
            //
            return ConnectedCurveIndex;
        }

        /// <summary>
        /// 从一组曲线中找到一条与连续链左端点相接的曲线，并且在适当的情况下，对搜索到的曲线进行反转。
        /// </summary>
        /// <param name="curves">进行搜索的曲线集合。在此函数中，可能会对连接到的那条曲线进行反转。
        /// IDictionary中的键值表示每一条Curve的Id值，这个值并不一定是从1开始递增的。</param>
        /// <returns>与连续曲线链的最右端相连的那一条曲线在输入的曲线集合中所对应的Id键值。
        /// 如果没有找到连接的曲线，则返回Nothing！</returns>
        public int? CheckBackward(Dictionary<int, Curve> curves)
        {
            // 搜索到的那一条曲线所对应的Id值
            int? ConnectedCurveIndex = new int?();
            ConnectedCurveIndex = null; // 如果没有找到，则返回Nothing
            //
            var Ids = curves.Keys.ToArray();
            var Cvs = curves.Values;
            //
            int tempId = 0;
            Curve tempCurve = default(Curve);

            // 从曲线集合中找出起点与上面的终点重合的曲线 。 find curve with start point = end point
            for (int i = 0; i <= Ids.Length - 1; i++)
            {
                tempId = Ids[i];
                tempCurve = curves[tempId];

                // Is there a match end->start, if so this is the next curve
                if (GeoHelper.IsAlmostEqualTo(tempCurve.GetEndPoint(0), this.end0, GeoHelper.VertexTolerance))
                {
                    // 将曲线进行反转
                    curves[tempId] = tempCurve.CreateReversed();
                    return tempId;
                }
                else if (GeoHelper.IsAlmostEqualTo(tempCurve.GetEndPoint(1), this.end0, GeoHelper.VertexTolerance))
                {
                    return tempId;
                }
            }
            //
            return ConnectedCurveIndex;
        }

        #endregion

        #region 连接

        /// <summary>
        /// 将曲线添加到连续曲线链的右端
        /// </summary>
        /// <param name="c">请自行确保添加的曲线是可以与当前的连续链首尾相接的。
        /// 如果不能确保，请通过CheckForward函数进行检测。</param>
        public void ConnectForward(Curve c)
        {
            CurvesChain.Add(c);
            end1 = c.GetEndPoint(1);
        }

        /// <summary>
        /// 将曲线添加到连续曲线链的左端
        /// </summary>
        /// <param name="c">请自行确保添加的曲线是可以与当前的连续链首尾相接的。
        /// 如果不能确保，请通过CheckBackward函数进行检测。</param>
        public void ConnectBackward(Curve c)
        {
            CurvesChain.Insert(0, c);
            end0 = c.GetEndPoint(0);
        }

        #endregion
    }

}