using Edgenda.AzureIoT.Common;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;

namespace Edgenda.AzureIoT.CameraCirculation.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            uint basePort = 15000;
            var hostname = Environment.GetEnvironmentVariable("CAMERASERVER_ZMQ_SERVERHOSTNAME") ?? "localhost";
            UInt32.TryParse(Environment.GetEnvironmentVariable("CAMERASERVER_ZMQ_SERVERBASEPORT") ?? "15000", out basePort);
            using (var server = new CameraServer(hostname, basePort))
            {
                Console.WriteLine("Starting");
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("Cancel Key Press");
                    using (var testClientSocket = new RequestSocket($"tcp://localhost:{basePort}"))
                    {
                        testClientSocket.SendFrame(JsonConvert.SerializeObject(new Command() { Name = Command.SHUTDOWN_COMMAND }));
                        var response = testClientSocket.ReceiveFrameString();
                    }
                    Console.WriteLine("Shutdown ack");
                };
                server.Start();
                Console.WriteLine("Running");
                server.Task.Wait();
                Console.WriteLine("Shutdown");
            }
        }
    }
}
