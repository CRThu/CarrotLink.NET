﻿using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Impl;
using CarrotLink.Core.Protocols.Configuration;
using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Session;
using CarrotLink.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Client
{
    public static class NiVisaDemo
    {
        public static void Test()
        {
            var visaConfig = new NiVisaConfiguration
            {
                DeviceId = "NI-VISA-Device",
                ResourceString = "USB0::0x0699::0x0413::C012473::INSTR",
                Timeout = 5000, // 5秒超时
            };
            var dev = new NiVisaDevice(visaConfig);
            dev.Connect();
            var logger = new CommandStorage();

            var session = DeviceSession.Create()
                .WithDevice(dev)
                .WithProtocol(new CarrotAsciiProtocol(null))
                .WithLogger(logger)
                .WithPollTask(false)
                .WithProcessTask(true)
                .Build();

            session.SendAscii("*IDN?");
            session.ManualReadAsync();

            Thread.Sleep(1000);
            if (logger.TryRead(out string? data))
                Console.WriteLine(data);

            //Console.WriteLine("enter commands or 'exit' to end transfer");
            //while (true)
            //{
            //    var line = Console.ReadLine();
            //    if (line == "exit")
            //    {
            //        break;
            //    }

            //    session.SendAscii(line!.ToString());
            //    Console.WriteLine($"Sent: {(line == "" ? "<empty>" : line)}");

            //    if (line.Contains('?'))
            //        session.ManualReadAsync();
            //}

            session.Dispose();
            session.Device.Disconnect();
        }
    }
}
