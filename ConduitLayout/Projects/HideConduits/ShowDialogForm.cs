using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ConduitLayout.Windows
{
    /// <summary>
    /// 用在 ShowDialogWin 或者 ShowDialogForm 类中。
    /// 当 DialogForm 被隐藏并执行完与Revit交互的hideProc方法后被触发。此事件响应完会即会立即执行  System.Windows.Window.ShowDialog();
    /// </summary>
    /// <param name="returnedValue"> hideProc 方法执行完成后的返回值，如果 hideProc 方法没有返回值，则 returnedValue 为 null。</param>
    public delegate void HideMethodReturnedProc(object returnedValue);

    /// <summary>
    /// 对于ShowDialog打开的 WinForm 或 WPF window 进行隐藏，并在隐藏后执行相关的界面交互操作。
    /// </summary>
    public interface IShowDialogThread
    {
        /// <summary>
        /// 一个伪异步方法，此方法会在执行Hide之后立即返回。
        /// </summary>
        /// <param name="hideProc"> 要与 Revit 进行交互的方法的委托。请在此方法中自行设置异常处理。
        /// 此方法如果有返回值，则通过响应 reternProc 事件来获取返回值。 </param>
        /// <param name="reternProc"> 当 hideProc 执行完成并得到了返回值后接着执行reternProc方法，请在此方法中自行设置异常处理。
        /// 执行完reternProc后，即会立即执行 Form.ShowDialog(); </param>
        /// <param name="hideProcArgs"> 方法 hideProc 中的输入参数，如果没有参数，则输入 null </param>
        /// <remarks>此方法后面不要再有任何的代码语句，如果要处理 hideProc 返回的结果，请在 reternProc 中进行操作。</remarks>
        void HideAndOperate(
            Delegate hideProc, 
            HideMethodReturnedProc reternProc = null,
            params object[] hideProcArgs);
    }

    /// <summary>
    /// 一个抽象类，用来对以Form.ShowDialog()方法开启的窗口进行操作。
    /// 对于此类的派生类而言，可以通过 <see cref="HideAndOperate"/> 方法来将窗口进行隐藏，
    /// 并在隐藏的状态下进行一些常规隐藏窗口下不能进行的操作（比如与 Revit 进行 UI 上的 PickObject() 交互）。
    /// </summary>
    /// <remarks> ModelDialog 与 Revit 进行UI交互的原理：在Form打开时，不支持与Revit的UI交互，
    /// 所以，此类通过 HideAndOperate 先将窗口进行隐藏，以跳出当前的ShowDialog的线程阻塞，
    /// 跳出后的线程即返回到 Standard Revit API Context，此时便可以与Revit进行UI交互了。
    /// 在 交互操作 _hideProc 执行完成并通过 _hideMethodReturnedProc 处理完其返回值后，再次通过 Form.ShowDialog() 将窗口显示出来。</remarks>
    public class ShowDialogForm : System.Windows.Forms.Form, IShowDialogThread
    {
        #region ---   Properties
        private bool _HideToOperate;
        //
        /// <summary> 在窗口隐藏状态下与 Revit 进行交互 的方法 </summary>
        private Delegate _hideProc;
        /// <summary> _hideProc 方法的输入参数 </summary>
        private object[] _hideProcArgs;        //
        /// <summary> _hideProc 方法的返回值 </summary>
        private object ReturnValue;

        //
        /// <summary> 在 _hideProc 执行完成后对其返回值进行处理 </summary>
        private HideMethodReturnedProc _hideMethodReturnedProc;

        #endregion

        /// <summary>
        /// 以 可隐藏的方式显示窗口
        /// </summary>
        /// <returns></returns>
        public new DialogResult ShowDialog()
        {
            DialogResult res;
            res = base.ShowDialog();

            while (_HideToOperate)
            {
                // MessageBox.Show("进入 while ");

                // 执行 隐藏后 所要进行的操作
                try
                {
                    ReturnValue = _hideProc.DynamicInvoke(_hideProcArgs);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("A exception in the hide-method was unhandled.", ex.InnerException);
                }

                // 在 _hideProc 执行完成后对其返回值进行处理
                try
                {
                    if (_hideMethodReturnedProc != null)
                    {
                        // 使用Invoke完成一个委托方法的封送，就类似于使用SendMessage方法来给界面线程发送消息，是一个同步方法。
                        // 也就是说在Invoke封送的方法被执行完毕前，Invoke方法不会返回，从而调用者线程将被阻塞。
                        _hideMethodReturnedProc.Invoke(ReturnValue);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("A exception in the hideMethodReturnedProc was unhandled.", ex);
                }

                //  MessageBox.Show("DynamicInvoke 结束");

                // 重新初始化
                _HideToOperate = false;
                _hideMethodReturnedProc = null;
                _hideProc = null;
                _hideProcArgs = null;


                // 在上次关闭时的位置重新启动
                StartPosition = FormStartPosition.Manual;
                res = base.ShowDialog();

                // MessageBox.Show("第 " + index.ToString() + " 个 ShowDialog 结束");

            }

            // MessageBox.Show("ShowDialog 完全结束");

            return res;
        }

        /// <summary>
        /// 一个伪异步方法，此方法会在执行Hide之后立即返回。
        /// </summary>
        /// <param name="hideProc"> 要与 Revit 进行交互的方法的委托。请在此方法中自行设置异常处理。
        /// 此方法如果有返回值，则通过响应 reternProc 事件来获取返回值。 </param>
        /// <param name="reternProc"> 当 hideProc 执行完成并得到了返回值后接着执行reternProc方法，请在此方法中自行设置异常处理。
        /// 执行完reternProc后，即会立即执行 Form.ShowDialog(); </param>
        /// <param name="hideProcArgs"> 方法 hideProc 中的输入参数，如果没有参数，则输入 null </param>
        /// <remarks>此方法后面不要再有任何的代码语句，如果要处理 hideProc 返回的结果，请在 reternProc 中进行操作。</remarks>
        public void HideAndOperate(Delegate hideProc, HideMethodReturnedProc reternProc = null,
            params object[] hideProcArgs)
        {

            //if (proc.Method.ReturnType == typeof(void)){};

            // 
            _hideProc = hideProc;
            _hideProcArgs = hideProcArgs;
            _hideMethodReturnedProc = reternProc;
            //
            _HideToOperate = true;

            // 将此 ShowDialog 窗口关闭，否则不能进行 Revit 中的 PickObject() 这种要与 Revit UI 进行交互的操作。
            this.Hide(); // 如果是ShowDialog，则这里用 Close() 或者 Hide() 都可以

            //注意线程执行的顺序： Hide() 执行完成后，线程会立即跳出HideAndOperate而继续执行，在 HideAndOperate 执行完成后再跳出 ShowDialog()并继续执行。
        }
    }
}
