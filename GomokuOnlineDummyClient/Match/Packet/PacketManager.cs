using GomokuOnlineDummyClient.Library;
using Google.Protobuf;
using Google.Protobuf.MatchProtocol;

namespace GomokuOnlineDummyClient.Match.Packet
{
    class PacketManager
    {
        // Singleton
        static PacketManager instance = new PacketManager();
        public static PacketManager Instance { get { return instance; } }

        Dictionary<ushort, Func<ArraySegment<byte>, IMessage>> packetMakers = new Dictionary<ushort, Func<ArraySegment<byte>, IMessage>>();
        Dictionary<ushort, Action<PacketSession, IMessage>> packetHandlers = new Dictionary<ushort, Action<PacketSession, IMessage>>();

        private PacketManager()
        {
            packetMakers.Add((ushort)PacketId.SReady, MakePacket<S_Ready>);
            packetHandlers.Add((ushort)PacketId.SReady, PacketHandler.S_ReadyHandler);
            packetMakers.Add((ushort)PacketId.SResponse, MakePacket<S_Response>);
            packetHandlers.Add((ushort)PacketId.SResponse, PacketHandler.S_ResponseHandler);
        }

        // [size(2)][packetId(2)][...]
        public void OnPacketReceived(PacketSession session, ArraySegment<byte> buffer)
        {
            ushort count = 0;
            ushort packetSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            count += 2;
            ushort packetId = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
            count += 2;

            // Make packet
            Func<ArraySegment<byte>, IMessage> packetMaker = null;
            if (packetMakers.TryGetValue(packetId, out packetMaker))
            {
                IMessage packet = packetMaker.Invoke(buffer);
                PacketMessage packetMessage = new PacketMessage(packetId, packet);
                PacketQueue.Instance.Push(packetMessage);
            }
            else
            {
                // TODO: 로그
                //Console.WriteLine("Disconnected");
                session.Disconnect();
            }
        }

        public Action<PacketSession, IMessage> GetPacketHandler(ushort packetId, IMessage packet)
        {
            Action<PacketSession, IMessage> packetHandler = null;
            packetHandlers.TryGetValue(packetId, out packetHandler);
            return packetHandler;
        }

        T MakePacket<T>(ArraySegment<byte> buffer) where T : IMessage, new()
        {
            T packet = new T();
            packet.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);
            return packet;
        }
    }
}
