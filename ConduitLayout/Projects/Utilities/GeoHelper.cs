﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using Autodesk.Revit.DB;

namespace ConduitLayout
{
    /// <summary>
    /// A object to help locating with geometry data.
    /// </summary>
    public static class GeoHelper
    {
        /// <summary>
        /// 进行点的距离比较时的容差。
        /// Revit中，Application.VertexTolerance 属性值返回的值为：0.0005233832795，
        /// 也就是说，如果两个点的距离小于这个值，就认为这两个点是重合的。
        /// </summary>
        public const double VertexTolerance = 0.0005;

        /// <summary>
        /// 进行点的距离比较时的容差。
        /// Revit中，Application.AngleTolerance 属性值返回的值为：0.00174532925199433，
        /// 也就是说，如果两个角度的区别小于这个值，就认为这两个角度是相同的的。
        /// Two angle measurements closer than this value are considered identical.
        /// </summary>
        public const double AngleTolerance = 0.0015;

        #region ---   Solid 或 Face 的搜索

        /// <summary>
        /// 获取一个单元中的所有体积大于 0 的实体对象（坐标为相对于单元所在的模型空间）
        /// </summary>
        /// <param name="elem"></param>
        /// 
        /// <returns>一个键值对字典，其中键表示element 中的实体对象，值指示此Solid是此 Element 所特有的（true），
        /// 还是对于此element 所属的族的Solid进行“索引+变换”后的结果（false）。
        /// 换句话说，true表示此 Element 是被切割过的，false表示此 Element 是被未被切割过的。
        /// 但是要注意，不论Solid所对应的值为true还是false，此solid的位置都是与项目中的element所在的位置相一致的。</returns>
        /// 
        /// <remarks>但是要注意，不论Solid所对应的值为true还是false，此solid的位置都是与项目中的element所在的位置相一致的。</remarks>
        public static Dictionary<Solid, bool> GetSolidsInModel(Element elem)
        {
            // 提取单元的几何信息
            Options opt = new Options
            {
                ComputeReferences = false,
                DetailLevel = ViewDetailLevel.Medium
            };

            GeometryElement geoElem = elem.get_Geometry(opt);

            Dictionary<Solid, bool> solids = new Dictionary<Solid, bool>();


            // 柱子中的各种图形
            foreach (GeometryObject obj in geoElem)  // GeometryElement 集合中只有 Solid 或者 GeometryInstance 对象，而没有 Face 等对象。
            {
                // 如果Element被切割了，那么此Element的几何信息与定义此Element的族的几何信息就不一样了，所以，被切割的Element的GeometryElement之中就直接包含有Solid
                if (obj is Solid && ((Solid)obj).Volume > 0)
                {
                    // 此时的 Solid 是 Familyinstance 特有的 Solid，后期不需要进行变换
                    // 如果此 Element is FamilyInstance，则其 HasModifiedGeometry 方法会返回 true。
                    solids.Add((Solid)obj, true);
                }

                // 如果 Element 未被切割，则GeometryElement之中就包含的就是GeometryInstance
                else if (obj is GeometryInstance)
                {
                    GeometryInstance geoInstance = obj as GeometryInstance;
                    // 返回族实例的几何数据
                    GeometryElement geoElement = geoInstance.GetInstanceGeometry();
                    //
                    foreach (GeometryObject obj2 in geoElement)
                    {
                        if (obj2 is Solid && ((Solid)obj2).Volume > 0)
                        {
                            // 此时的solid 是 Family中的solid 直接经过变换后得到的索引实体对象。Solid的坐标位置与其在项目中的位置重合，后期不需要进行变换。
                            // 如果此 Element is FamilyInstance，则其 HasModifiedGeometry 方法会返回 false。
                            solids.Add((Solid)obj2, false);
                        }
                    }
                }
            }
            return solids;
        }

        /// <summary>
        /// 获取 Solid 集合中所有面积大于0 的 Face 对象
        /// </summary>
        /// <param name="solids"></param>
        /// <returns> 返回的Face集合中的面都是位于实体的表面的面 </returns>
        public static IList<Face> GetSurfaces(IEnumerable<Solid> solids)
        {
            IList<Face> faces = new List<Face>();
            foreach (Solid solid in solids)
            {
                foreach (Face face in solid.Faces)
                {
                    if (face.Area > 0)
                    {
                        faces.Add(face);
                    }
                }
            }

            return faces;
        }


        /// <summary>
        /// Find the bottom face of a face array.
        /// </summary>
        /// <param name="faces">A face array.</param>
        /// <returns>The bottom face of a face array.</returns>
        public static Face GetBottomFace(FaceArray faces)
        {
            Face face = null;
            double elevation = 0;
            double tempElevation = 0;
            Mesh mesh = null;

            foreach (Face f in faces)
            {
                if (IsVerticalFace(f))
                {
                    // If this is a vertical face, it cannot be a bottom face to a certainty.
                    continue;
                }

                tempElevation = 0;
                mesh = f.Triangulate();

                foreach (XYZ xyz in mesh.Vertices)
                {
                    tempElevation = tempElevation + xyz.Z;
                }

                tempElevation = tempElevation / mesh.Vertices.Count;

                if (elevation > tempElevation || null == face)
                {
                    // Update the bottom face to which's elevation is the lowest.
                    face = f;
                    elevation = tempElevation;
                }
            }

            // The bottom face is consider as which's average elevation is the lowest, except vertical face.
            return face;
        }

        public static PlanarFace GetBottomPlanarFace(Element element)
        {
            Dictionary<Solid, bool> solids = GetSolidsInModel(element);
            IList<Face> faces = GetSurfaces(solids.Keys);
            PlanarFace bottomPf = null;
            double lowestElevation = double.MaxValue;
            foreach (Face f in faces)
            {
                if (f is PlanarFace)
                {
                    PlanarFace pf = (PlanarFace)f;

                    // 法向向下，而且标高最低
                    if (IsInOneDirectioin(pf.FaceNormal, new XYZ(0, 0, -1)) && pf.Origin.Z < lowestElevation)
                    {
                        lowestElevation = pf.Origin.Z;
                        bottomPf = pf;
                    }
                }
            }
            return bottomPf;
        }

        /// <summary>
        /// 找到Extrusion中指定法向的平面。如果有多个平面的法向都是指定的法向，则返回第一个找到的平面。
        /// Given a solid, find a planar face with the given normal (version 2)
        /// this is a slightly enhanced version which checks if the face is on the given reference plane.
        /// </summary>
        /// <param name="refPlane">除了验证平面的法向外，还可以额外验证一下指定法向的平面是否是在指定的参考平面上。即要同时满足normal与ReferencePlane两个条件。
        /// additionally, we want to check if the face is on the reference plane</param>
        /// <remarks></remarks>
        public static PlanarFace FindFace(Extrusion aSolid, XYZ normal, ReferencePlane refPlane = null)
        {
            //' get the geometry object of the given element
            //'
            Options op = new Options();
            op.ComputeReferences = true;
            // Dim geomObjs As GeometryObjectArray = aSolid.Geometry(op).Objects

            //' loop through the array and find a face with the given normal
            //'
            foreach (GeometryObject geomObj in aSolid.get_Geometry(op))
            {
                if (geomObj is Solid) //'  solid is what we are interested in.
                {
                    Solid pSolid = (Solid)geomObj;
                    FaceArray faces = pSolid.Faces;

                    foreach (Face pFace in faces)
                    {
                        if (pFace is PlanarFace)
                        {
                            PlanarFace pPlanarFace = (PlanarFace)pFace;
                            if (!(pPlanarFace == null))
                            {
                                //'  check to see if they have same normal
                                if (pPlanarFace.ComputeNormal(new UV(0, 0)).IsAlmostEqualTo(normal))
                                {
                                    if (refPlane == null)
                                    {
                                        return pPlanarFace; //' we found the face.
                                    }
                                    else
                                    {
                                        //'  additionally, we want to check if the face is on the reference plane
                                        //'  get a point on the face. Any point will do.
                                        Edge pEdge = pPlanarFace.EdgeLoops.get_Item(0).get_Item(0);
                                        XYZ pt = pEdge.Evaluate(0.0);
                                        //'  is the point on the reference plane
                                        bool res = Convert.ToBoolean(IsPointOnPlane(pt, refPlane.GetPlane()));
                                        if (res)
                                        {
                                            return pPlanarFace; //' we found the face
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (geomObj is GeometryInstance)
                {
                    //' will come back later as needed.
                }
                else if (geomObj is Curve)
                {
                    //' will come nack later as needed.
                }
                else if (geomObj is Mesh)
                {
                    //' will come back later as needed.
                }
                else
                {
                    //' what else do we have
                }
            }

            //' if we come here, we did not find any.
            return null;
        }

        #endregion

        #region ---   XYZ  点 或 向量的操作

        /// <summary>
        /// 在三维空间中，将一个点 point 沿指定的方向 direction 延伸指定的距离 length
        /// </summary>
        /// <param name="point"></param>
        /// <param name="direction"></param>
        /// <param name="length"></param>
        /// <returns>延伸后的新的坐标点</returns>
        public static XYZ Extend(this XYZ point, XYZ direction, double length)
        {
            return point + direction.Normalize() * length;

            //return new XYZ(
            //    x: point.X + length * Math.Cos(direction.AngleTo(new XYZ(1, 0, 0))),
            //    y: point.Y + length * Math.Cos(direction.AngleTo(new XYZ(0, 1, 0))),
            //    z: point.Z + length * Math.Cos(direction.AngleTo(new XYZ(0, 0, 1))));

        }

        /// <summary>
        /// Calculate the length between two points.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        /// <returns>The length between two points.</returns>
        public static double GetLength(XYZ startPoint, XYZ endPoint)
        {
            return
                Math.Sqrt(Math.Pow(Convert.ToDouble(endPoint.X - startPoint.X), 2) +
                          Math.Pow(Convert.ToDouble(endPoint.Y - startPoint.Y), 2) +
                          Math.Pow(Convert.ToDouble(endPoint.Z - startPoint.Z), 2));
        }
        
        /// <summary>
        /// Get the vector between two points.
        /// </summary>
        /// <param name="startPoint">The start point.</param>
        /// <param name="endPoint">The end point.</param>
        /// <returns>The vector between two points.</returns>
        public static XYZ GetVector(XYZ startPoint, XYZ endPoint)
        {
            return new XYZ(endPoint.X - startPoint.X, endPoint.Y - startPoint.Y, endPoint.Z - startPoint.Z);
        }
        /// <summary>
        /// Determines whether two vector are equal in x and y axis.
        /// </summary>
        /// <param name="vectorA">The vector A.</param>
        /// <param name="vectorB">The vector B.</param>
        /// <returns>Return true if two vector are equals, or else return false.</returns>
        private static bool Equal(XYZ vectorA, XYZ vectorB)
        {
            bool isNotEqual = (VertexTolerance < Math.Abs(vectorA.X - vectorB.X)) ||
                              (VertexTolerance < Math.Abs(vectorA.Y - vectorB.Y));
            return isNotEqual ? false : true;
        }
        /// <summary> 比较两个点之间的距离是否小于指定的容差 </summary>
        /// <remarks>对于Revit中的XYZ对象，其也有一个IsAlmostEqualTo函数，但是要注意，
        /// 那个函数是用来比较两个向量的方向是否小于指定的弧度容差。</remarks>
        public static bool IsAlmostEqualTo(XYZ Point1, XYZ Point2, double Precision)
        {
            double D = Math.Sqrt(Convert.ToDouble(Math.Pow(Point1.X - Point2.X, 2) + Math.Pow(
                Point1.Y - Point2.Y, 2) + Math.Pow(
                    Point1.Z - Point2.Z, 2)));
            if (D <= Precision)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary> 比较两个方向向量是否指向同一个方向 </summary>
        /// <remarks></remarks>
        public static bool IsInOneDirectioin(XYZ vec1, XYZ vec2)
        {
            return vec1.IsAlmostEqualTo(vec2, AngleTolerance);
        }

        #endregion

        /// <summary>
        /// Find out the three points which made of a plane.
        /// </summary>
        /// <param name="mesh">A mesh contains many points.</param>
        /// <param name="startPoint">Create a new instance of ReferencePlane.</param>
        /// <param name="endPoint">The free end apply to reference plane.</param>
        /// <param name="thirdPnt">A third point needed to define the reference plane.</param>
        public static void Distribute(Mesh mesh, ref XYZ startPoint, ref XYZ endPoint, ref XYZ thirdPnt)
        {
            int count = Convert.ToInt32(mesh.Vertices.Count);
            startPoint = mesh.Vertices[0];
            endPoint = mesh.Vertices[count / 3];
            thirdPnt = mesh.Vertices[count / 3 * 2];
        }

        /// <summary>
        /// Determines whether a face is vertical.
        /// </summary>
        /// <param name="face">The face to be determined.</param>
        /// <returns>Return true if this face is vertical, or else return false.</returns>
        private static bool IsVerticalFace(Face face)
        {
            foreach (EdgeArray ea in face.EdgeLoops)
            {
                foreach (Edge e in ea)
                {
                    if (IsVerticalEdge(e))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a edge is vertical.
        /// </summary>
        /// <param name="edge">The edge to be determined.</param>
        /// <returns>Return true if this edge is vertical, or else return false.</returns>
        private static bool IsVerticalEdge(Edge edge)
        {
            List<XYZ> polyline = edge.Tessellate() as List<XYZ>;
            XYZ verticalVct = new XYZ(0, 0, 1);
            XYZ pointBuffer = polyline[0];

            for (int i = 1; i <= polyline.Count - 1; i++)
            {
                XYZ temp = polyline[i];
                XYZ vector = GetVector(pointBuffer, temp);
                if (Equal(vector, verticalVct))
                {
                    return true;
                }
                else
                {
                    continue;
                }
            }

            return false;
        }
        

        /// <summary>
        /// 判断一个三维点是否在指定的平面上
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static bool IsPointOnPlane(XYZ p1, Plane plane)
        {
            // 指定点到原点法向向量的投影长度，即此点到对应平面距离
            // 说明此点到指定平面的距离很小
            return Convert.ToDouble(plane.Normal.DotProduct(p1 - plane.Origin)) < VertexTolerance;
        }

        /// <summary>
        /// 判断一个三维点是否在指定的参考平面上
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="plane">参考平面，ReferencePlane.GetPlane方法也可以返回Plane对象。</param>
        /// <returns></returns>
        public static bool IsPointOnPlane(XYZ p1, ReferencePlane plane)
        {
            // 指定点到原点法向向量的投影长度，即此点到对应平面距离
            // 说明此点到指定平面的距离很小
            return Convert.ToDouble(plane.Normal.DotProduct(p1 - plane.BubbleEnd)) < VertexTolerance;
        }

        /// <summary> 查看曲线集合中每一条曲线的端点坐标 </summary>
        /// <param name="curves"> 要查看端点坐标的曲线集合 </param>
        /// <returns>用来进行Messagebox.Show或者Debug.Print的字符串。</returns>
        public static string GetCurvesEnd(IEnumerable<Curve> curves)
        {
            StringBuilder sb = new StringBuilder();
            foreach (Curve c in curves)
            {
                if (c.IsBound)
                {
                    sb.AppendLine(c.GetEndPoint(0) + "\n\r" + c.GetEndPoint(1));
                }
                else
                {
                    sb.AppendLine("无界曲线：" + c.GetType().Name);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}