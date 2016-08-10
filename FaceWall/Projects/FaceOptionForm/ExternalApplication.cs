using System;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace FaceWall
{

    //插件的 XML 调用方式：
    //<?xml version="1.0" encoding="utf-8"?>
    //<RevitAddIns>
    //<AddIn Type="Application">
    //  <Name>ExternalApplication</Name>
    //  <Assembly>D:\GithubProjects\FaceWall\FaceWall\FaceWall\bin\Debug\FaceWall.dll</Assembly>
    //  <ClientId>dbb30c8f-65c9-4b9b-8e77-1fd252dc377b</ClientId>
    //  <FullClassName>FaceWall.ExternalApplication</FullClassName>
    //  <VendorId>ADSK</VendorId>
    //  <VendorDescription>Autodesk, www.autodesk.com</VendorDescription>
    //</AddIn>
    //</RevitAddIns>

    public class ExternalApplication : IExternalApplication
    {

        string Dll_Projects = "FaceWall.dll";

        public Result OnShutdown(UIControlledApplication application)
        {
            //TaskDialog.Show("Revit", "ExternalApplication")
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {

            //Create a custom ribbon tab
            string tabName = GlobalParameters.AddinTabName;
            application.CreateRibbonTab(tabName);

            // 建模面板
            RibbonPanel ribbonPanelModeling = application.CreateRibbonPanel(tabName, GlobalParameters.panelName_DrawFace);
            AddPushButtonShowPane(ribbonPanelModeling);

            // 注册 DockablePane 面板
            //  RegisterPanel(application);

            return Result.Succeeded;
        }

        public void RegisterPanel(UIControlledApplication app)
        {
            MpFaceOptions mpf = MpFaceOptions.UniqueObject(app);

            if (true)  // if (!_isRegistered)
            {
                app.RegisterDockablePane(MpFaceOptions.DockablePaneId_FaceWall, "面层参数", mpf);
            }
        }

        #region ---   添加按钮

        /// <summary> 添加“打开面层面板”的按钮 </summary>
        private void AddPushButtonShowPane(RibbonPanel panel)
        {
            // Create a new push button
            PushButton pushButton =
                panel.AddItem(new PushButtonData("ShowDrawFaceWindow", "面层", Path.Combine(GlobalParameters.PathDlls, Dll_Projects),
                    "FaceWall.cmds_DrawFaceWindow")) as PushButton;
            pushButton.ToolTip = "打开面层面板。";
            // Set Contextual help
            ContextualHelp contextHelp = new ContextualHelp(ContextualHelpType.Url, "http://www.cmie.cn");
            pushButton.SetContextualHelp(contextHelp);
            // Set Icon
            pushButton.LargeImage = new BitmapImage(new Uri(Path.Combine(GlobalParameters.PathDlls, "DrawFace.png")));
        }


        #endregion


    }
}
