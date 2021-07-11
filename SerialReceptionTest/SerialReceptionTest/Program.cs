using AppCommon.Net;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;

namespace UsartReceptionTest
{
    class Program
    {
        static Mpu6050ReceptionTestManager _receptionTest1 = new Mpu6050ReceptionTestManager();
        static System.Timers.Timer _generalTimer = new System.Timers.Timer(AppConstant.TEST_DURATION);

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().ReadFrom.AppSettings().CreateLogger();

            Dictionary<string, Action> _appMenu = new Dictionary<string, Action>()
            {
                { AppConstant.STR_START, Start },
                { AppConstant.STR_STOP, Stop },
                { AppConstant.STR_LAST_TEST_INFO, PrintLastTestInfo },
                { AppCommonConstant.STR_QUIT, null }
            };

            _generalTimer.Elapsed += GeneralTimer_Tick;

            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                case PlatformID.Win32NT:
                    break;

                default:
                    Console.WriteLine("Unsupported OS");
                    return;
            }

            while (true)
            {
                AppCommonUtil.PrintMenu(_appMenu);
                string command = Console.ReadLine().ToLower();

                if (command == AppCommonConstant.STR_QUIT)
                {
                    break;
                }
                if (_appMenu.ContainsKey(command) == true)
                {
                    if (_appMenu[command] != null)
                    {
                        _appMenu[command]();
                    }
                    else
                    {
                        Log.Warning($"Invalid example for {command}");
                        Console.WriteLine($"Invalid example for {command}");
                    }
                }
                else
                {
                    Log.Warning("Unknown command!!!");
                    Console.WriteLine("Unknown command!!!");
                }
            }

            Stop();

            Log.Information("Main done");
            Log.CloseAndFlush();

            Console.WriteLine("Main done");
        }

        private static void PrintLastTestInfo()
        {
            _receptionTest1.PrintLastTestInfo();
        }

        private static void GeneralTimer_Tick(object sender, ElapsedEventArgs e)
        {
            Stop();
        }

        private static void Stop()
        {
            _generalTimer.Stop();

            if (_receptionTest1.IsAccelGyroEventStarted() == true)
            {
                _receptionTest1.Mpu6050EventStop();
                Thread.Sleep(500);
            }
            
            _receptionTest1.Stop();
            _receptionTest1.Commit();
            Log.Information("Timer stop");
        }

        private static void Start()
        {
            _receptionTest1.Start();
            _receptionTest1.Mpu6050EventStart();
            Log.Information("Timer start");
            _generalTimer.Start();
        }
    }
}
