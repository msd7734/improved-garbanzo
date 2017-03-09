using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text;

namespace WindowsService
{
    class KeyHookManager : IDisposable
    {
        #region Extern Methods
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

        #endregion

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(WindowsService));

        //ID for: Hook procedure that monitors low-level keyboard input events. 
        private const int WH_KEYBOARD_LL = 13;
        //ID for: Keydown event
        private const int WM_KEYDOWN = 0x0100;
        //ID for: Keyup event
        private const int WM_KEYUP = 0x0101;
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private bool mainKeyIsDown;
        private bool modKeyIsDown;

        public delegate void MessageDelegate(string msg);

        private MessageDelegate _externAction;

        public KeyHookManager(MessageDelegate externalCallback)
        {
            //AllocConsole();
            mainKeyIsDown = false;
            modKeyIsDown = false;
            _proc = HookCallback;
            _hookID = SetHook(_proc);
            _externAction = externalCallback;
            log.Info("Registered KeyHookManager");
            //Application.Run();
        }

        private string KeyCodeToUnicode(Keys key)
        {
            byte[] keyboardState = new byte[255];
            bool keyboardStateStatus = GetKeyboardState(keyboardState);

            if (!keyboardStateStatus)
            {
                return "";
            }

            uint virtualKeyCode = (uint)key;
            uint scanCode = MapVirtualKey(virtualKeyCode, 0);
            IntPtr inputLocaleIdentifier = GetKeyboardLayout(0);

            StringBuilder result = new StringBuilder();
            ToUnicodeEx(virtualKeyCode, scanCode, keyboardState, result, (int)5, (uint)0, inputLocaleIdentifier);

            return result.ToString();
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(
            int nCode, IntPtr wParam, IntPtr lParam)
        {
            log.Info("Hit the hook callback.");
            if (wParam == (IntPtr)WM_KEYDOWN)
            {
                log.Info("There was a key down.");
                //KeysConverter kc = new KeysConverter();
                int vkCode = Marshal.ReadInt32(lParam);
                string keystr = KeyCodeToUnicode((Keys)vkCode);
                _externAction(keystr);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);

            /*
            //Less than 0 is invalid and must be passed through.
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);

                    
                    if (Settings.Default["Shortcut"].ToString() == ShortcutEnum.WinQ.ToString())
                    {
                        if ((Keys)vkCode == Keys.LWin)
                            modKeyIsDown = true;

                        if ((Keys)vkCode == Keys.Q)
                        {
                            mainKeyIsDown = true;

                            if (modKeyIsDown)
                            {
                                _externAction();
                                //stop propagation of key event
                                return (IntPtr)1;
                            }
                        }
                    }

                    else if (Settings.Default["Shortcut"].ToString() == ShortcutEnum.CtrlQ.ToString())
                    {
                        if ((Keys)vkCode == Keys.LControlKey)
                            modKeyIsDown = true;

                        if ((Keys)vkCode == Keys.Q)
                        {
                            mainKeyIsDown = true;

                            if (modKeyIsDown)
                            {
                                _externAction();
                                //stop propagation of key event
                                return (IntPtr)1;
                            }
                        }
                    }
                    
                }

                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    int vkCode = Marshal.ReadInt32(lParam);

                    
                    if (Settings.Default["Shortcut"].ToString() == ShortcutEnum.WinQ.ToString())
                    {
                        if ((Keys)vkCode == Keys.LWin)
                        {
                            modKeyIsDown = false;

                            if (mainKeyIsDown)
                            {
                                //need to do this to release windows key without opening start menu
                                InputSimulator sim = new InputSimulator();
                                sim.Keyboard.KeyDown(VirtualKeyCode.CONTROL);
                                sim.Keyboard.KeyUp(VirtualKeyCode.LWIN);
                                sim.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
                                return (IntPtr)1;
                            }
                        }
                        else if ((Keys)vkCode == Keys.Q)
                            mainKeyIsDown = false;
                    }

                    else if (Settings.Default["Shortcut"].ToString() == ShortcutEnum.CtrlQ.ToString())
                    {
                        if ((Keys)vkCode == Keys.LControlKey)
                        {
                            modKeyIsDown = false;
                        }
                        else if ((Keys)vkCode == Keys.Q)
                            mainKeyIsDown = false;
                    }
                    
                }
            }*/

            // return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
            //FreeConsole();
        }
    }
}