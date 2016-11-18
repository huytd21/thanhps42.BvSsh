using AutoIt;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace thanhps42.BvSsh
{
    public enum ConnectStatus
    {
        Connected,
        Unconnected
    }

    public class BvSshHelper
    {
        #region -- Fields -

        private Process _bvssh = null;
        private string _bvsshExe = null;
        private string _bvsshProfile = null;
        private int _lastListenPort;
        private ConnectStatus _connectStatus = ConnectStatus.Unconnected;
        protected string status = null;

        #endregion -- Fields -

        #region -- Events --

        public delegate void DisconnecHandler();
        public event DisconnecHandler OnDisconnect;

        #endregion -- Events --

        #region -- Constructor --

        public BvSshHelper(string exe, string profile, object oAutoIt)
        {
            _bvsshExe = exe;
            _bvsshProfile = profile;
            Sync.oAutoIt = oAutoIt;
        }

        #endregion -- Constructor --

        #region -- Properties --

        public bool IsConnected
        {
            get
            {
                return IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners()
                    .Any(x => x.Port == _lastListenPort);
            }
        }

        #endregion -- Properties --

        #region -- Protected Methods --

        protected virtual void ShowStatus()
        {
        }

        #endregion -- Protected Methods --

        #region -- Public Methods --

        public bool Start()
        {
            try
            {
                status = "Starting BvSsh...";
                ShowStatus();
                _bvssh = new Process();
                _bvssh.StartInfo.FileName = _bvsshExe;
                //bvssh.StartInfo.Arguments = string.Format("-profile=\"{0}\" -hide=main -hide=auth,popups,banner,trayIcon", bvsshProfile);
                _bvssh.StartInfo.Arguments = string.Format("-profile=\"{0}\" -hide=auth,popups,banner,trayIcon", _bvsshProfile);
                _bvssh.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                _bvssh.Start();
                _bvssh.WaitForInputIdle();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Login(string host, string username, string password, int portFw, int timeOut = 20)
        {
            _lastListenPort = portFw;
            if (_bvssh == null || _bvssh.HasExited)
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
                hwndHost = GetHandle(Controlnfo.Host),
                hwndUsername = GetHandle(Controlnfo.Username),
                hwndPassword = GetHandle(Controlnfo.Password),
                hwndListenPort = GetHandle(Controlnfo.ListenPort),
                hwndLogin = GetHandle(Controlnfo.Login);

            // set host
            SetControlText(hwndHost, host);

            // set username
            SetControlText(hwndUsername, username);

            // set password
            lock (Sync.oAutoIt)
            {
                AutoItX.ControlFocus(_bvssh.MainWindowHandle, hwndPassword);
                AutoItX.ControlSend(_bvssh.MainWindowHandle, hwndPassword, "{END}");
                AutoItX.ControlSend(_bvssh.MainWindowHandle, hwndPassword, "+{HOME}");
                AutoItX.ControlSend(_bvssh.MainWindowHandle, hwndPassword, password, 1);
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
                _connectStatus = ConnectStatus.Connected;
                status = string.Format("LIVE {0} | {1} | {2}", host, username, password);
                ShowStatus();
                new Thread(DisconnectChecker) { IsBackground = true }.Start();
                return true;
            }

            if (btnLoginText != StringHelper.Login)
                Logout();

            status = string.Format("DIE {0} | {1} | {2}", host, username, password);
            ShowStatus();
            _connectStatus = ConnectStatus.Unconnected;
            return false;
        }

        public void Kill()
        {
            try
            {
                if (_bvssh != null)
                {
                    _bvssh.Kill();
                    _bvssh.Dispose();
                    _bvssh = null;
                }
            }
            catch
            {
            }
        }

        public void Logout()
        {
            try
            {
                status = "Logging out...";
                ShowStatus();
                IntPtr hwndLogin = GetHandle(Controlnfo.Login);
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
                status = "Logged out";
                ShowStatus();
            }
            catch
            {
                Kill();
            }
            finally
            {
                _connectStatus = ConnectStatus.Unconnected;
            }
        }

        public Process Connect(string host, string username, string password, int portFw, int timeOut = 20)
        {
            try
            {
                string btnLoginText = "";

                var bvssh = new Process();
                bvssh.StartInfo.FileName = _bvsshExe;
                bvssh.StartInfo.Arguments = string.Format("-profile=\"{0}\" -hide=banner -hide=trayIcon -host={1} -user={2} -password={3}", _bvsshProfile, host, username, password);
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
                Kill();
                return null;
            }
            catch
            {
                Kill();
                return null;
            }
        }

        #endregion -- Public Methods --

        #region -- Private Methods --

        private void DisconnectChecker()
        {
            while (true)
            {
                if (!IsConnected)
                {
                    if (_connectStatus == ConnectStatus.Connected) OnDisconnect?.Invoke();
                    break;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private void SetControlText(IntPtr hwnd, object text)
        {
            lock (Sync.oAutoIt)
                AutoItX.ControlSetText(_bvssh.MainWindowHandle, hwnd, text.ToString());
        }

        private void Click(IntPtr hwnd)
        {
            lock (Sync.oAutoIt)
            {
                AutoItX.ControlFocus(_bvssh.MainWindowHandle, hwnd);
                AutoItX.ControlClick(_bvssh.MainWindowHandle, hwnd);
            }
        }

        private string GetControlText(IntPtr hwnd)
        {
            string controlText = "";
            lock (Sync.oAutoIt)
                controlText = AutoItX.ControlGetText(_bvssh.MainWindowHandle, hwnd);
            return controlText;
        }

        private IntPtr GetHandle(string controlInfo)
        {
            IntPtr hwnd;
            lock (Sync.oAutoIt)
                hwnd = AutoItX.ControlGetHandle(_bvssh.MainWindowHandle, controlInfo);
            return hwnd;
        }

        #endregion -- Private Methods --
    }
}