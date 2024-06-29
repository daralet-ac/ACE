using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ACE.Server.Network.Managers;
using Serilog;
using Serilog.Events;

namespace ACE.Server.Network
{
    // Reference: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.beginreceivefrom?view=net-7.0
    public class ConnectionListener
    {
        private readonly ILogger _log = Log.ForContext<ConnectionListener>();

        public Socket Socket { get; private set; }

        public IPEndPoint ListenerEndpoint { get; private set; }

        private readonly uint listeningPort;

        private readonly byte[] buffer = new byte[ClientPacket.MaxPacketSize];

        private readonly IPAddress listeningHost;

        public ConnectionListener(IPAddress host, uint port)
        {
            _log.Debug("ConnectionListener ctor, host {Host} port {Port}", host, port);

            listeningHost = host;
            listeningPort = port;
        }

        public void Start()
        {
            _log.Debug("Starting ConnectionListener, host {ListeningHost} port {ListeningPort}", listeningHost, listeningPort);

            try
            {
                ListenerEndpoint = new IPEndPoint(listeningHost, (int)listeningPort);
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                //if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                //{
                //    var sioUdpConnectionReset = -1744830452;
                //    var inValue = new byte[] { 0 };
                //    var outValue = new byte[] { 0 };
                //    Socket.IOControl(sioUdpConnectionReset, inValue, outValue);
                //}

                Socket.Bind(ListenerEndpoint);
                Listen();
            }
            catch (Exception exception)
            {
                _log.Fatal(exception, "Network Socket has thrown");
            }
        }

        public void Shutdown()
        {
            _log.Debug("Shutting down ConnectionListener, host {ListeningHost} port {ListeningPort}", listeningHost, listeningPort);

            if (Socket != null && Socket.IsBound)
                Socket.Close();
        }

        private void Listen()
        {
            try
            {
                EndPoint clientEndPoint = new IPEndPoint(listeningHost, 0);
                Socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref clientEndPoint, OnDataReceive, Socket);
            }
            catch (SocketException socketException)
            {
                _log.Debug(socketException, "ConnectionListener({ListeningHost}, {ListeningPort}).Listen() has thrown {SocketErrorCode}", listeningHost, listeningPort, socketException.SocketErrorCode);
                Listen();
            }
            catch (Exception exception)
            {
                _log.Fatal(exception, "ConnectionListener({ListeningHost}, {ListeningPort}).Listen() has thrown", listeningHost, listeningPort);
            }
        }

        private void OnDataReceive(IAsyncResult result)
        {
            EndPoint clientEndPoint = null;

            try
            {
                clientEndPoint = new IPEndPoint(listeningHost, 0);
                int dataSize = Socket.EndReceiveFrom(result, ref clientEndPoint);

                IPEndPoint ipEndpoint = (IPEndPoint)clientEndPoint;

                // TO-DO: generate ban entries here based on packet rates of endPoint, IP Address, and IP Address Range

                if (_log.IsEnabled(LogEventLevel.Verbose))
                {
                    byte[] data = new byte[dataSize];
                    Buffer.BlockCopy(buffer, 0, data, 0, dataSize);

                    _log.Verbose("Received Packet (Len: {DataLength}) [{IpEndpointAddress}:{IpEndpointPort}=>{ListenerEndpointAddress}:{ListenerEndpointPort}] {PacketString}", data.Length, ipEndpoint.Address, ipEndpoint.Port, ListenerEndpoint.Address, ListenerEndpoint.Port, data.BuildPacketString());
                }

                var packet = new ClientPacket();

                if (packet.Unpack(buffer, dataSize))
                    NetworkManager.ProcessPacket(this, packet, ipEndpoint);

                packet.ReleaseBuffer();
            }
            catch (SocketException socketException)
            {
                // If we get "Connection has been forcibly closed..." error, just eat the exception and continue on
                // This gets sent when the remote host terminates the connection (on UDP? interesting...)
                // TODO: There might be more, should keep an eye out. Logged message will help here.
                if (socketException.SocketErrorCode == SocketError.MessageSize ||
                    socketException.SocketErrorCode == SocketError.NetworkReset ||
                    socketException.SocketErrorCode == SocketError.ConnectionReset)
                {
                    _log.Debug(socketException, "ConnectionListener({ListeningHost}, {ListeningPort}).OnDataReceive() has thrown {SocketErrorCode}: from client {ClientEndpoint}", listeningHost, listeningPort, socketException.SocketErrorCode, clientEndPoint != null ? clientEndPoint.ToString() : "Unknown");
                }
                else
                {
                    _log.Fatal(socketException, "ConnectionListener({ListeningHost}, {ListeningPort}).OnDataReceive() has thrown {SocketErrorCode}:  from client {ClientEndpoint}", listeningHost, listeningPort, socketException.SocketErrorCode, clientEndPoint != null ? clientEndPoint.ToString() : "Unknown");
                    return;
                }
            }

            if (result.CompletedSynchronously)
                Task.Run(() => Listen());
            else
                Listen();
        }
    }
}
