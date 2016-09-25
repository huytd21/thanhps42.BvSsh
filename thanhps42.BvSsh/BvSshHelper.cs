using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AutoIt;
using System.Diagnostics;
using System.Threading;

namespace thanhps42.BvSsh
{
    public class BvSshHelper
    {
        #region -- Fields -
        private Process bvssh = null;
        private string bvsshExe = null;
        private string bvsshProfile = null;
        protected string status = null;
        #endregion

        #region -- Constructor --
        public BvSshHelper(string exe, string profile, object oAutoIt)
        {
            bvsshExe = exe;
            bvsshProfile = profile;
            Sync.oAutoIt = oAutoIt;
        }
        #endregion

        #region -- Protected Methods --
        protected virtual void ShowStatus()
        {

        }
        #endregion

        #region -- Public Methods --
        public bool Start()
        {
            try
            {
                status = "Starting BvSsh...";
                ShowStatus();
                bvssh = new Process();
                bvssh.StartInfo.FileName = bvsshExe;
                //bvssh.StartInfo.Arguments = string.Format("-profile=\"{0}\" -hide=main -hide=auth,popups,banner,trayIcon", bvsshProfile);
                bvssh.StartInfo.Arguments = string.Format("-profile=\"{0}\" -hide=auth,popups,banner,trayIcon", bvsshProfile);
                bvssh.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                bvssh.Start();
                bvssh.WaitForInputIdle();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool Login(string host, string username, string password, int portFw, int timeOut = 20)
        {
            if (bvssh == null || bvssh.HasExited)
                Start();
            Counter.Login++;
            if (Counter.Login % 50 == 0)
            {
                Kill();
                Start();
                Counter.Login = 1;
            }
            string btnLoginText = "";

            IntPtr
                hwndHost = GetHandle(Controlnfo.HOST),
                hwndUsername = GetHandle(Controlnfo.USERNAME),
                hwndPassword = GetHandle(Controlnfo.PASSWORD),
                hwndListenPort = GetHandle(Controlnfo.LISTEN_PORT),
                hwndLogin = GetHandle(Controlnfo.LOGIN);

            // set host
            SetControlText(hwndHost, host);

            // set username
            SetControlText(hwndUsername, username);

            // set password
            lock(Sync.oAutoIt)
            {
                AutoItX.ControlFocus(bvssh.MainWindowHandle, hwndPassword);
                AutoItX.ControlSend(bvssh.MainWindowHandle, hwndPassword, "{END}");
                AutoItX.ControlSend(bvssh.MainWindowHandle, hwndPassword, "+{HOME}");
                AutoItX.ControlSend(bvssh.MainWindowHandle, hwndPassword, password);
            }

            // set listen port
            SetControlText(hwndListenPort, portFw);

            // click Login
            Click(hwndLogin);

            // waiting for logging
            int countdown = timeOut;
            while (countdown != 0)
            {
                status = string.Format("[{0}] Connecting {1} | {2} | {3}", countdown, host, username, password);
                ShowStatus();
                btnLoginText = GetControlText(hwndLogin);
                if (btnLoginText == StringHelper.Logout || btnLoginText == StringHelper.Login)
                    break;
                Thread.Sleep(1000);
                countdown--;
            }

            btnLoginText = GetControlText(hwndLogin);
            if (btnLoginText == StringHelper.Logout)
            {
                status = string.Format("LIVE {0} | {1} | {2}", host, username, password);
                ShowStatus();
                return true;
            }

            if (btnLoginText != StringHelper.Login)
                Logout();
         
            status = string.Format("DIE {0} | {1} | {2}", host, username, password);
            ShowStatus();
            return false;
        }
        public void Kill()
        {
            try
            {
                if (bvssh != null)
                {
                    bvssh.Kill();
                    bvssh.Dispose();
                    bvssh = null;
                }
            }
            catch
            {

            }
        }
        public void Logout()
        {
            status = "Logging out...";
            ShowStatus();
            IntPtr hwndLogin = GetHandle(Controlnfo.LOGIN);
            string btnLoginText = GetControlText(hwndLogin);
            if (btnLoginText == StringHelper.Login)
                return;
            Click(hwndLogin);
            int timeOut = 10;
            while (timeOut > 0)
            {
                if (GetControlText(hwndLogin) == StringHelper.Login)
                    break;
                Thread.Sleep(1000);
                timeOut--;
            }
            if (GetControlText(hwndLogin) != StringHelper.Login)
                Logout();
        }
        public Process Connect(string host, string username, string password, int portFw, int timeOut = 20)
        {
            string btnLoginText = "";

            var bvssh = new Process();
            bvssh.StartInfo.FileName = bvsshExe;
            bvssh.StartInfo.Arguments = string.Format("-profile=\"{0}\" -hide=banner -hide=trayIcon -host={1} -user={2} -password={3}", bvsshProfile, host, username, password);
            bvssh.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            bvssh.Start();
            bvssh.WaitForInputIdle();

            IntPtr _1144, _1;

            lock (Sync.oAutoIt)
            {
                _1144 = AutoItX.ControlGetHandle(bvssh.MainWindowHandle, "[ID: 1144]");
                _1 = AutoItX.ControlGetHandle(bvssh.MainWindowHandle, "[CLASS:Button; INSTANCE:1]");
                AutoItX.ControlSetText(bvssh.MainWindowHandle, _1144, portFw.ToString());
                AutoItX.ControlClick(bvssh.MainWindowHandle, _1);
            }

            int countdown = timeOut;
            while (countdown != 0)
            {
                status = string.Format("[{0}] Connecting {1} | {2} | {3}", countdown, host, username, password);
                ShowStatus();

                lock (Sync.oAutoIt) btnLoginText = AutoItX.ControlGetText(bvssh.MainWindowHandle, _1);
                if (btnLoginText == "Logout" || btnLoginText == "Login")
                {
                    break;
                }
                Thread.Sleep(1000);
                countdown--;
            }

            lock (Sync.oAutoIt) btnLoginText = AutoItX.ControlGetText(bvssh.MainWindowHandle, _1);
            if (btnLoginText == "Logout")
            {
                status = string.Format("LIVE {0} | {1} | {2}", host, username, password);
                ShowStatus();
                return bvssh;
            }
            status = string.Format("DIE {0} | {1} | {2}", host, username, password);
            ShowStatus();
            try
            {
                bvssh.Kill();
                bvssh.Dispose();
                bvssh = null;
            }
            catch { }
            return null;
        }
        #endregion

        #region -- Private Methods --
        private void SetControlText(IntPtr hwnd, object text)
        {
            lock (Sync.oAutoIt)
                AutoItX.ControlSetText(bvssh.MainWindowHandle, hwnd, text.ToString());
        }
        private void Click(IntPtr hwnd)
        {
            lock (Sync.oAutoIt)
                AutoItX.ControlClick(bvssh.MainWindowHandle, hwnd);
        }
        private string GetControlText(IntPtr hwnd)
        {
            string controlText = "";
            lock (Sync.oAutoIt)
                controlText = AutoItX.ControlGetText(bvssh.MainWindowHandle, hwnd);
            return controlText;
        }
        private IntPtr GetHandle(string controlInfo)
        {
            IntPtr hwnd;
            lock (Sync.oAutoIt)
                hwnd = AutoItX.ControlGetHandle(bvssh.MainWindowHandle, controlInfo);
            return hwnd;
        }
        #endregion
    }
}
