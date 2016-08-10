using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Color = System.Windows.Media.Color;

namespace FaceWall
{
    public static class Utils
    {
        /// <summary>
        /// 在调试阶段，为每一种报错显示对应的报错信息及出错位置。
        /// 在软件发布前，应将此方法中的内容修改为常规的报错提醒。
        /// </summary>
        /// <param name="ex"> Catch 块中的 Exception 对象</param>
        /// <param name="message">报错信息提示</param>
        /// <param name="title"> 报错对话框的标题 </param>
        public static void ShowDebugCatch(Exception ex, string message, string title = "出错")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(message);
            sb.AppendLine(ex.Message);

            // 一直向下提取InnerException
            Exception exInner = ex.InnerException;
            Exception exStack = ex;
            while (exInner != null)
            {
                exStack = exInner;
                sb.AppendLine(exInner.Message);
                exInner = exInner.InnerException;
            }
            // 最底层的出错位置
            sb.AppendLine("\r\n" + exStack.StackTrace);

            MessageBox.Show(sb.ToString(), title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #region ---   Color 颜色的格式转换

        public static void ConvertColor(System.Windows.Media.Color color1, out Autodesk.Revit.DB.Color color2)
        {
            color2 = new Autodesk.Revit.DB.Color(color1.R, color1.G, color1.B);
        }


        public static void ConvertColor(Autodesk.Revit.DB.Color color1, out System.Windows.Media.Color color2)
        {
            color2 = new System.Windows.Media.Color()
            {
                R = color1.Red,
                G = color1.Green,
                B = color1.Blue,
            };
        }

        public static void ConvertColor(System.Drawing.Color color1, out System.Windows.Media.Color color2)
        {
            color2 = new System.Windows.Media.Color()
            {
                R = color1.R,
                G = color1.G,
                B = color1.B,
            };
        }

        public static void ConvertColor(System.Windows.Media.Color color1, out System.Drawing.Color color2)
        {
            color2 = System.Drawing.Color.FromArgb(255, color1.R, color1.G, color1.B);

        }


        #endregion


        /// <summary> 以矩阵的形式返回变换矩阵，仅作显示之用 </summary>
        /// <param name="Trans"></param>
        public static string ShowTransforMatrix(Transform Trans)
        {
            string str = "";
            Transform with_1 = Trans;
            str = "(" + with_1.BasisX.X.ToString("0.000") + "  ,  " + with_1.BasisY.X.ToString("0.000") + "  ,  " +
                  with_1.BasisZ.X.ToString("0.000") + "  ,  " + with_1.Origin.X.ToString("0.000") + ")" + "\r\n" +
                  "(" + with_1.BasisX.Y.ToString("0.000") + "  ,  " + with_1.BasisY.Y.ToString("0.000") + "  ,  " +
                  with_1.BasisZ.Y.ToString("0.000") + "  ,  " + with_1.Origin.Y.ToString("0.000") + ")" + "\r\n" +
                  "(" + with_1.BasisX.Z.ToString("0.000") + "  ,  " + with_1.BasisY.Z.ToString("0.000") + "  ,  " +
                  with_1.BasisZ.Z.ToString("0.000") + "  ,  " + with_1.Origin.Z.ToString("0.000") + ")";
            return str;
        }

        #region ---   弹框显示集合中的某些属性或者字段的值

        /// <summary>
        /// 将集合中的每一个元素的ToString函数的结果组合到一个字符串中进行显示
        /// </summary>
        /// <param name="V"></param>
        /// <param name="title"></param>
        /// <param name="newLineHandling"> 如果元素之间是以换行分隔，则为True，否则是以逗号分隔。 </param>
        /// <remarks></remarks>
        public static void ShowEnumerable(IEnumerable V, string title = "集合中的元素", bool newLineHandling = true)
        {
            StringBuilder sb = new StringBuilder();
            if (newLineHandling)
            {
                foreach (object o in V)
                {
                    sb.AppendLine(o.ToString());
                }
            }
            else
            {
                foreach (object o in V)
                {
                    sb.Append(o.ToString() + ",\t");
                }
            }

            MessageBox.Show(sb.ToString(), title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 将集合中的每一个元素的指定属性的ToString函数的结果组合到一个字符串中进行显示
        /// </summary>
        /// <param name="V"></param>
        /// <param name="PropertyName">要读取的属性的名称，注意，此属性不能带参数。</param>
        /// <param name="newLineHandling"> 如果元素之间是以换行分隔，则为True，否则是以逗号分隔。 </param>
        /// <remarks></remarks>
        public static void ShowEnumerableProperty(IEnumerable V, string PropertyName, string Title = "集合中的元素", bool newLineHandling = true)
        {
            List<string> strings = new List<string>();

            Type tp = default(Type);
            MethodInfo MdInfo = default(MethodInfo);
            string res = "";
            foreach (object obj in V)
            {
                tp = obj.GetType();
                MdInfo = tp.GetProperty(PropertyName).GetMethod;
                res = MdInfo.Invoke(obj, null).ToString();
                //
                strings.Add(res);
            }
            ShowEnumerable(strings, Title, newLineHandling);
        }

        /// <summary>
        /// 将集合中的每一个元素的指定字段的ToString函数的结果组合到一个字符串中进行显示
        /// </summary>
        /// <param name="V"></param>
        /// <param name="FieldName">要读取的字段的名称。</param>
        /// <param name="newLineHandling"> 如果元素之间是以换行分隔，则为True，否则是以逗号分隔。 </param>
        /// <remarks></remarks>
        public static void ShowEnumerableField(IEnumerable V, string FieldName, string Title = "集合中的元素", bool newLineHandling = true)
        {
            List<string> strings = new List<string>();
            Type tp = default(Type);

            string res = "";
            foreach (object obj in V)
            {
                tp = obj.GetType();
                res = tp.GetField(FieldName).GetValue(obj).ToString();
                //
                strings.Add(res);
            }
            ShowEnumerable(strings, Title, newLineHandling);
        }

        #endregion

    }
}
