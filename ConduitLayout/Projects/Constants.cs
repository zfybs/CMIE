using System;
using System.IO;
using System.Reflection;

namespace ConduitLayout
{
    /// <summary>
    /// Revit中的共享参数等全局数据
    /// </summary>
    internal static class GlobalParameters
    {
       
        #region ---   程序集参数

        public const string AddinTabName = "电气设备";
        public const string panelName_DrawFace = "线管";

        #endregion

        /// <summary>
        /// Application的Dll所对应的路径，也就是“bin”文件夹的目录。
        /// </summary>
        public static string PathDlls
        {
            get
            {
                string path = @"D:\GithubProjects\FaceWall\FaceWall\FaceWall\bin\Debug";
                return Directory.Exists(path) ? path : new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            }
        }

        /// <summary>
        /// 共享参数文本文件的绝对路径
        /// </summary>
        //public static string Path_SharedParameters = Path.Combine(PathDlls, "参数.txt");
    }

}
