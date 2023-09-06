using System.Net;
using System.Net.Sockets;

namespace GomokuOnlineDummyClient.Library
{
    public class Listener
    {
        Socket listenSocket;
        Func<Session> sessionFactory;

        public void Start(IPEndPoint endPoint, int backlog, Func<Session> sessionFactory)
        {
            this.listenSocket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            this.sessionFactory += sessionFactory;

            listenSocket.Bind(endPoint);
            listenSocket.Listen(backlog);

            AcceptLoop();
        }

        async Task AcceptLoop()
        {
            while (true)
            {
                try
                {
                    Socket clientSocket = await listenSocket.AcceptAsync();
                    OnAccepted(clientSocket);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                }
            }
        }

        void OnAccepted(Socket clientSocket)
        {
            Session session = sessionFactory.Invoke();
            session.Start(clientSocket);
        }
    }
}
