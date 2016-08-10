using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace ConduitLayout
{
    public static class ExtensionMethods
    {
        #region   ---  elementId

        /// <summary> 从ElementId返回其所在的Document中的Element对象 </summary>
        /// <param name="elementId"></param>
        /// <param name="Doc">此elementId所位于的文档</param>
        public static Element Element(this ElementId elementId, Document Doc)
        {
            return Doc.GetElement(elementId);
        }

        #endregion

        #region   ---  DB.Document

        /// <summary> 返回项目文档中指定名称的族Family对象 </summary>
        /// <param name="FamilyName">在此文档中，所要搜索的族对象的名称</param>
        /// <param name="Category">此族所属的 BuiltInCategory 类别，如果不确定，就不填。</param>
        public static Family FindFamily(this Document Doc, string FamilyName,
            BuiltInCategory Category = BuiltInCategory.INVALID)
        {
            Family fam = null;
            // 文档中所有的族对象
            FilteredElementCollector cols = new FilteredElementCollector(Doc);
            IList<Element> Familys = cols.OfClass(typeof(Family)).ToElements();
            // 按名称搜索族（Linq语句）
            IEnumerable<Family> Q = default(IEnumerable<Family>);
            if (Category == BuiltInCategory.INVALID) // 只搜索族的名称
            {
                Q = from Family ff in Familys
                    where ff.Name == FamilyName
                    select ff;
            }
            else // 同时搜索族对象的类别，注意，族的类别信息保存在属性中。
            {
                Q = from Family ff in Familys
                    where (ff.Name == FamilyName) && (ff.FamilyCategory.Id == new ElementId(BuiltInCategory.OST_Site))
                    select ff;
            }
            if (Q.Count() > 0)
            {
                fam = Q.First();
            }
            return fam;
        }

        /// <summary> 返回项目文档中指定类别的族对象。在函数中会对所有族对象的FamilyCategory进行判断。</summary>
        /// <param name="Category">此族所属的 BuiltInCategory 类别，即FamilyCategory属性所对应的类别。</param>
        public static List<Family> FindFamilies(this Document Doc, BuiltInCategory Category)
        {
            List<Family> fams = new List<Family>();
            // 文档中所有的族对象
            FilteredElementCollector cols = new FilteredElementCollector(Doc);
            IList<Element> Familys = cols.OfClass(typeof(Family)).ToElements();
            // 按类别搜索族（Linq语句）
            if (Category != BuiltInCategory.INVALID) // 只搜索族类别
            {
                IEnumerable<Family> Q = from Family ff in Familys
                                        where ff.FamilyCategory.Id == new ElementId(BuiltInCategory.OST_Site)
                                        select ff;
                fams = Q.ToList();
            }
            return fams;
        }

        #region    ---   FindElements 搜索文档中的元素

        /// <summary>
        /// 根据指定的类型、类别与名称来搜索Revit文档所有元素中的第一个有效对象
        /// </summary>
        /// <param name="rvtDoc"></param>
        /// <param name="targetType"></param>
        /// <param name="targetCategory"> INVALID 表示不进行类别限制 </param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public static IList<Element> FindElements(this Document rvtDoc, Type
            targetType, BuiltInCategory targetCategory = BuiltInCategory.INVALID, string targetName = null)
        {

            //'  first, narrow down to the elements of the given type and category
            var collector = new FilteredElementCollector(rvtDoc).OfClass(targetType);

            // 是否要按类别搜索
            if (!(targetCategory == BuiltInCategory.INVALID))
            {
                collector.OfCategory(targetCategory);
            }

            // 是否要按名称搜索
            if (targetName != null)
            {
                //'  using LINQ query here.
                var elems = from element in collector
                            where element.Name.Equals(targetName)
                            select element;

                //'  put the result as a list of element for accessibility.
                return elems.ToList();
            }
            return collector.ToElements();
        }

        /// <summary>
        ///  根据指定的类型、类别与名称来搜索Revit文档指定元素集合中的第一个有效对象
        /// </summary>
        /// <param name="rvtDoc">要进行搜索的Revit文档</param>
        /// <param name="SourceElements">要从文档中的哪个集合中来进行搜索</param>
        /// <param name="targetType"></param>
        /// <param name="targetCategory"> INVALID 表示不进行类别限制 </param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        /// <remarks></remarks>
        public static IList<Element> FindElements(this Document rvtDoc, ICollection<ElementId> SourceElements, Type
            targetType, BuiltInCategory targetCategory = BuiltInCategory.INVALID, string targetName = null)
        {


            var collector = new FilteredElementCollector(rvtDoc, SourceElements);

            // 搜索类型
            collector.OfClass(targetType);

            // 是否要搜索类别
            if (!(targetCategory == BuiltInCategory.INVALID))
            {
                collector.OfCategory(targetCategory);
            }

            // 是否要搜索名称
            if (targetName != null)
            {
                IEnumerable<Element> elems = default(IEnumerable<Element>);
                //'  parse the collection for the given names
                //'  using LINQ query here.
                elems = from element in collector
                        where element.Name.Equals(targetName)
                        select element;
                return elems.ToList();
            }

            return collector.ToElements();
        }

        /// <summary>
        /// 根据指定的类型、类别与名称来搜索Revit文档的所有元素中的第一个有效对象
        /// </summary>
        /// <param name="rvtDoc"></param>
        /// <param name="targetType"></param>
        /// <param name="targetCategory"> INVALID 表示不进行类别限制 </param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public static Element FindElement(this Document rvtDoc, Type
            targetType, BuiltInCategory targetCategory = BuiltInCategory.INVALID, string targetName = null)
        {

            //'  find a list of elements using the overloaded method.
            IList<Element> elems = FindElements(rvtDoc, targetType, targetCategory, targetName);

            //'  return the first one from the result.
            if (elems.Count > 0)
            {
                return elems[0];
            }
            return null;
        }

        /// <summary>
        /// 根据指定的类型、类别与名称来搜索Revit文档指定元素集合中的第一个有效对象
        /// </summary>
        /// <param name="rvtDoc"></param>
        /// <param name="SourceElements"></param>
        /// <param name="targetType"></param>
        /// <param name="targetCategory"> INVALID 表示不进行类别限制 </param>
        /// <param name="targetName"></param>
        /// <returns></returns>
        public static Element FindElement(this Document rvtDoc, ICollection<ElementId> SourceElements, Type
            targetType, BuiltInCategory targetCategory = BuiltInCategory.INVALID, string targetName = null)
        {
            //'  find a list of elements using the overloaded method.
            IList<Element> elems = FindElements(rvtDoc, targetType, targetCategory, targetName);

            //'  return the first one from the result.
            if (elems.Count > 0)
            {
                return elems[0];
            }
            return null;
        }

        #endregion

        #endregion

        #region   ---  Family

        /// <summary> 返回项目文档中某族Family的所有实例 </summary>
        /// <param name="Category">此族所属的 BuiltInCategory 类别，如果不确定，就不填。</param>
        /// <param name="Family"></param>
        public static FilteredElementCollector Instances(this Family Family,
            BuiltInCategory Category = BuiltInCategory.INVALID)
        {
            Document doc = Family.Document;
            List<ElementId> SymbolsId = Family.GetFamilySymbolIds().ToList();
            FilteredElementCollector Collector1 = new FilteredElementCollector(doc);

            if (SymbolsId.Count > 0)
            {
                // 创建过滤器
                FamilyInstanceFilter Filter = new FamilyInstanceFilter(doc, SymbolsId[0]);
                // 执行过滤条件
                if (Category != BuiltInCategory.INVALID)
                {
                    Collector1 = Collector1.OfCategory(Category);
                }
                Collector1.WherePasses(Filter);
            }
            // 当族类型多于一个时，才进行相交
            if (SymbolsId.Count > 1)
            {
                for (int index = 1; index <= SymbolsId.Count - 1; index++)
                {
                    // 创建过滤器
                    FamilyInstanceFilter Filter = new FamilyInstanceFilter(doc, SymbolsId[index]);
                    FilteredElementCollector Collector2 = new FilteredElementCollector(doc);
                    // 执行过滤条件
                    if (Category != BuiltInCategory.INVALID)
                    {
                        Collector2 = Collector2.OfCategory(Category);
                    }
                    Collector2.WherePasses(Filter);

                    // 将此FamilySymbol的实例添加到集合中
                    Collector1.UnionWith(Collector2);
                }
            }
            return Collector1;
        }

        /// <summary> 对Document中加载的族进行重命名 </summary>
        /// <param name="Family"></param>
        /// <param name="NewName">要重新命名的新名称</param>
        public static void ReName(this Family Family, string NewName)
        {
            Document doc = Family.Document;
            using (Transaction tran = new Transaction(doc, "Rename family"))
            {
                tran.Start();
                Family.Name = NewName;
                tran.Commit();
            }
        }

        #endregion

        #region   ---  FamilySymbol

        /// <summary> 返回项目文档中某族类型FamilySymbol的所有实例 </summary>
        /// <param name="FamilySymbol"></param>
        public static FilteredElementCollector Instances(this FamilySymbol FamilySymbol)
        {
            Document doc = FamilySymbol.Document;
            ElementId FamilySymbolId = FamilySymbol.Id;
            FilteredElementCollector InsancesColl = new FilteredElementCollector(doc);
            FamilyInstanceFilter FIFilter = new FamilyInstanceFilter(doc, FamilySymbolId);
            InsancesColl.WherePasses(FIFilter);
            return InsancesColl;
        }

        #endregion

        #region   ---  Transform

        /// <summary> 以矩阵的形式返回变换矩阵，仅作显示之用 </summary>
        /// <param name="Trans"></param>
        public static string ToString_Matrix(this Transform Trans)
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

        #endregion

        #region   ---  Double

        /// <summary> 长度单位转换：将英尺转换为毫米 1英尺=304.8mm </summary>
        /// <param name="value_foot"></param>
        /// <remarks> 1 foot = 12 inches = 304.8 mm</remarks>
        public static double Foot2mm(this double value_foot)
        {
            // 1 foot = 12 inches = 304.8 mm
            return value_foot * 304.8;
        }

        #endregion
    }
}
