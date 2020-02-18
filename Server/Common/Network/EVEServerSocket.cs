using System;
using System.Net;
using System.Net.Sockets;
using Common.Logging;

namespace Common.Network
{
    public class EVEServerSocket : EVESocket
    {
        public Channel Log { get; set; }
        public int Port { get; private set; }
        
        public EVEServerSocket(int port, Channel logChannel) : base()
        {
            this.Log = logChannel;
            this.Port = port;
        }

        public void Listen()
        {
            // bind the socket to the correct endpoint
            this.Socket.Bind(new IPEndPoint(IPAddress.Any, this.Port));
            this.Socket.Listen(20);
        }

        public void BeginAccept(AsyncCallback callback)
        {
            this.Socket.BeginAccept(callback, this);
        }

        public EVEClientSocket EndAccept(IAsyncResult asyncResult)
        {
            return new EVEClientSocket (this.Socket.EndAccept(asyncResult), Log);
        }

        public override void GracefulDisconnect()
        {
            // graceful disconnect is the same as forcefully in a listening socket
            this.ForcefullyDisconnect();
        }

        protected override void DefaultExceptionHandler(Exception ex)
        {
            Log.Error("Unhandled exception on underlying socket:");
            Log.Error(ex.Message);
        }
    }
}