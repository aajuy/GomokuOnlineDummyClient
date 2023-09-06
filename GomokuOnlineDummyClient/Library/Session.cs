using System.Net;
using System.Net.Sockets;

namespace GomokuOnlineDummyClient.Library
{
    public abstract class Session
    {
        public Socket socket;
        ReceiveBuffer recvBuffer = new ReceiveBuffer(65535);
        Queue<ArraySegment<byte>> sendQueue = new Queue<ArraySegment<byte>>();
        List<ArraySegment<byte>> bufferList = new List<ArraySegment<byte>>();

        public int disconnected = 0;
        bool isSending = false;
        object sendLock = new object();

        public int SessionId { get; set; }

        //public abstract Task<int> Authorize();
        public abstract void OnConnected(EndPoint endPoint);
        public abstract int OnReceived(ArraySegment<byte> buffer);
        public abstract void OnSent(int bytesTransferred);
        public abstract void OnDisconnected(EndPoint endPoint);

        public void Start(Socket socket)
        {
            this.socket = socket;
            //SessionId = await Authorize();
            //if (SessionId > 0)
            //{
            //    OnConnected(socket.RemoteEndPoint);
            //    ReceiveLoop();
            //}
            //else
            //{
            //    Disconnect();
            //}
            OnConnected(socket.RemoteEndPoint);
            ReceiveLoop();
        }

        async Task ReceiveLoop()
        {
            while (true)
            {
                if (disconnected == 1)
                {
                    return;
                }

                recvBuffer.Clean();
                ArraySegment<byte> segment = recvBuffer.WriteSegment;

                try
                {
                    int bytesTransferred = await socket.ReceiveAsync(segment, SocketFlags.None);
                    if (bytesTransferred <= 0)
                    {
                        Disconnect();
                        return;
                    }

                    // Core logic
                    recvBuffer.OnWrite(bytesTransferred);
                    int processedLen = OnReceived(recvBuffer.ReadSegment);
                    recvBuffer.OnRead(processedLen);
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    Disconnect();
                    return;
                }
            }
        }

        public void Send(ArraySegment<byte> sendBuffer)
        {
            lock (sendLock)
            {
                sendQueue.Enqueue(sendBuffer);
                if (isSending == false)
                {
                    SendLoop();
                }
            }
        }

        async Task SendLoop()
        {
            while (true)
            {
                if (disconnected == 1)
                {
                    return;
                }

                lock (sendLock)
                {
                    CreateBufferListFromQueue();
                    sendQueue.Clear();
                    isSending = true;
                }

                try
                {
                    int bytesTransferred = await socket.SendAsync(bufferList, SocketFlags.None);
                    if (bytesTransferred <= 0)
                    {
                        Disconnect();
                        return;
                    }

                    // Core logic
                    OnSent(bytesTransferred);

                    lock (sendLock)
                    {
                        if (sendQueue.Count == 0)
                        {
                            isSending = false;
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Console.WriteLine(ex.ToString());
                    Disconnect();
                    return;
                }
            }
        }

        void CreateBufferListFromQueue()
        {
            bufferList.Clear();
            while (sendQueue.Count > 0)
            {
                ArraySegment<byte> buff = sendQueue.Dequeue();
                bufferList.Add(buff);
            }
        }

        public void Disconnect()
        {
            // Check if the connection is already disconnected
            if (Interlocked.Exchange(ref disconnected, 1) == 1)
            {
                return;
            }

            try
            {
                OnDisconnected(socket.RemoteEndPoint);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                Clear();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
            }
        }

        void Clear()
        {
            lock (sendLock)
            {
                sendQueue.Clear();
                bufferList.Clear();
            }
        }
    }

    public abstract class PacketSession : Session
    {
        public static readonly int HEADER_SIZE = 2;

        public abstract void OnPacketReceived(ArraySegment<byte> buffer);

        // [size(2)][packetId(2)][...]
        public sealed override int OnReceived(ArraySegment<byte> buffer)
        {
            int processedLen = 0;

            while (true)
            {
                // 헤더 파싱 가능한지 확인
                if (buffer.Count < HEADER_SIZE)
                {
                    break;
                }

                // 패킷이 완전히 도착했는지 확인
                ushort packetSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (buffer.Count < packetSize)
                {
                    break;
                }

                // 패킷 처리
                OnPacketReceived(new ArraySegment<byte>(buffer.Array, buffer.Offset, packetSize));
                processedLen += packetSize;

                // 다음 패킷 처리 준비
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + packetSize, buffer.Count - packetSize);
            }

            return processedLen;
        }
    }
}
