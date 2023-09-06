using GomokuOnlineDummyClient.Library;
using Google.Protobuf;
using Google.Protobuf.GameProtocol;

namespace GomokuOnlineDummyClient.Game.Packet
{
    class GamePacketManager
    {
        // Singleton
        static GamePacketManager instance = new GamePacketManager();
        public static GamePacketManager Instance { get { return instance; } }

        Dictionary<ushort, Func<ArraySegment<byte>, IMessage>> packetMakers = new Dictionary<ushort, Func<ArraySegment<byte>, IMessage>>();
        Dictionary<ushort, Action<PacketSession, IMessage>> packetHandlers = new Dictionary<ushort, Action<PacketSession, IMessage>>();

        private GamePacketManager()
        {
            packetMakers.Add((ushort)PacketId.SStart, MakePacket<S_Start>);
            packetHandlers.Add((ushort)PacketId.SStart, GamePacketHandler.S_StartHandler);
            packetMakers.Add((ushort)PacketId.SEnd, MakePacket<S_End>);
            packetHandlers.Add((ushort)PacketId.SEnd, GamePacketHandler.S_EndHandler);
            packetMakers.Add((ushort)PacketId.SMove, MakePacket<S_Move>);
            packetHandlers.Add((ushort)PacketId.SMove, GamePacketHandler.S_MoveHandler);
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
                GamePacketQueue.Instance.Push(packetMessage);
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
