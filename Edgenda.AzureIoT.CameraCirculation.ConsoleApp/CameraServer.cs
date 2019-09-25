using Edgenda.AzureIoT.Common;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Edgenda.AzureIoT.CameraCirculation.ConsoleApp
{
    /// <summary>
    /// Camera server
    /// </summary>
    public class CameraServer : IDisposable
    {
        private readonly GetByCoordinatesHandler _getByCoordinatesHandler;
        private PublisherSocket _publisherSocket;
        private ResponseSocket _serverSocket;
        private ServerStateEnum _serverStatus;
        private Task _task;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly string _hostname;
        private readonly uint _basePort;

        /// <summary>
        /// Cancellation token source for process shutdown
        /// </summary>
        CancellationTokenSource CancellationTokenSource { get => _cancellationTokenSource; set => _cancellationTokenSource = value; }
        /// <summary>
        /// Gets or sets current server state
        /// </summary>
        public ServerStateEnum Status { get => this._serverStatus; private set => this._serverStatus = value; }
        /// <summary>
        /// Gets or set server task
        /// </summary>
        public Task Task { get => _task; private set => _task = value; }
        /// <summary>
        /// Default constructor for camera server
        /// </summary>
        /// <param name="hostname">Hostname used for 0mq binding</param>
        /// <param name="basePort">Base port used by the req/resp socket publish socket is @basePort+1</param>
        public CameraServer(string hostname = "*", uint basePort = 15000)
        {
            this._getByCoordinatesHandler = new GetByCoordinatesHandler();
            this._publisherSocket = new PublisherSocket();
            this._serverSocket = new ResponseSocket();
            this._hostname = hostname;
            this._basePort = basePort;
        }
        /// <summary>
        /// Handles get camera details by coordinates
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        private string HandleGetByCoordinatesCommand(double longitude, double latitude)
        {
            var data = this._getByCoordinatesHandler.GetByCoordinates(longitude, latitude);
            return JsonConvert.SerializeObject(data);
        }
        /// <summary>
        /// Processes command received from the request socket
        /// </summary>
        /// <param name="cmd">command as a string serializable into Command object</param>
        /// <returns></returns>
        public string ProcessCommand(string cmd)
        {
            var command = JsonConvert.DeserializeObject<Command>(cmd);
            switch (command.Name)
            {
                case Command.GET_BY_COORDINATES_COMMAND:
                    {
                        string retVal = string.Empty;
                        var gbcCommand = JsonConvert.DeserializeObject<GetByCoordinatesCommand>(cmd);
                        retVal = this.HandleGetByCoordinatesCommand(gbcCommand.Parameters[0], gbcCommand.Parameters[1]);
                        return retVal;
                    }
                case Command.SHUTDOWN_COMMAND:
                    {
                        this.Stop();
                        return "OK";
                    }
                default: return string.Empty;
            }
        }
        /// <summary>
        /// Disposition handler
        /// </summary>
        public void Dispose()
        {
            this._publisherSocket.Dispose();
            this._serverSocket.Dispose();
            this._publisherSocket = null;
            this._serverSocket = null;
        }
        /// <summary>
        /// Starts ZMQ Server
        /// </summary>
        public void Start()
        {
            this._serverSocket.Bind($"tcp://{this._hostname}:{this._basePort}");
            this._publisherSocket.Bind($"tcp://{this._hostname}:{this._basePort + 1}");
            this.CancellationTokenSource = new CancellationTokenSource();
            this.Task = Task.Run(() =>
            {
                this.Status = ServerStateEnum.Running;
                this.Listen();
            }, this.CancellationTokenSource.Token);

        }
        /// <summary>
        /// Stops Camera Server
        /// </summary>
        public void Stop()
        {
            this.CancellationTokenSource.Cancel();
        }
        /// <summary>
        /// Listen to server socket
        /// </summary>
        private void Listen()
        {
            bool doWork = true;
            while (doWork)
            {
                if (this.CancellationTokenSource.IsCancellationRequested)
                {
                    this._serverSocket.Unbind($"tcp://{this._hostname}:{this._basePort}");
                    this._publisherSocket.Unbind($"tcp://{this._hostname}:{this._basePort + 1}");
                    this.Status = ServerStateEnum.Stopped;
                    doWork = false;
                    return;
                }
                var message = this._serverSocket.ReceiveFrameString();
                Console.WriteLine("Received request from client: {0}", message);
                string result = string.Empty;
                try
                {
                    result = this.ProcessCommand(message);
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(error);
                }
                this._serverSocket.SendFrame(result);
            }
        }
    }
}
