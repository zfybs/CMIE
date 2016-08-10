using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace ConduitLayout
{

    /// <summary>
    /// 对曲线集合的形式进行修改，但是不检查曲线集合是否连续或者共面等限制条件，如果出错，自行负责。
    /// </summary>
    public static class CurvesConverter
    {

        public static void Convert(IList<Curve> sourceCurves, out CurveArray targetCurves)
        {
            targetCurves = new CurveArray();
            foreach (Curve c in sourceCurves)
            {
                targetCurves.Append(c);
            }
        }

        public static void Convert(CurveArray sourceCurves, out IList<Curve> targetCurves)
        {
            targetCurves = new List<Curve>();
            foreach (Curve c in sourceCurves)
            {
                targetCurves.Add(c);
            }
        }

        public static void Convert(EdgeArray sourceCurves, out IList<Curve> targetCurves)
        {
            targetCurves = new List<Curve>();
            foreach (Edge ed in sourceCurves)
            {
                targetCurves.Add(ed.AsCurve());
            }
        }

        public static void Convert(EdgeArray sourceCurves, out CurveArray targetCurves)
        {
            targetCurves = new CurveArray();
            foreach (Edge ed in sourceCurves)
            {
                targetCurves.Append(ed.AsCurve());
            }
        }


        public static void Convert(CurveLoop sourceCurves, out CurveArray targetCurves)
        {
            targetCurves = new CurveArray();
            foreach (Curve c in sourceCurves)
            {
                targetCurves.Append(c);
            }
        }

        public static void Convert(IList<CurveLoop> sourceCurves, out CurveArray targetCurves)
        {
            targetCurves = new CurveArray();
            foreach (CurveLoop cl in sourceCurves)
            {
                foreach (var c in cl)
                {
                    targetCurves.Append(c);
                }
            }
        }

    }
}
