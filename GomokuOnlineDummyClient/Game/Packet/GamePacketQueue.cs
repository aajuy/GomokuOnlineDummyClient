using Google.Protobuf;

namespace GomokuOnlineDummyClient.Game.Packet
{
    public class PacketMessage
    {
        public ushort packetId;
        public IMessage packet;

        public PacketMessage(ushort packetId, IMessage packet)
        {
            this.packetId = packetId;
            this.packet = packet;
        }
    }

    public class GamePacketQueue
    {
        private static GamePacketQueue instance = new GamePacketQueue();
        public static GamePacketQueue Instance { get { return instance; } }

        object queueLock = new object();
        Queue<PacketMessage> packetQueue = new Queue<PacketMessage>();

        private GamePacketQueue()
        {

        }

        public void Push(PacketMessage packetMessage)
        {
            lock (queueLock)
            {
                packetQueue.Enqueue(packetMessage);
            }
        }

        public PacketMessage Pop()
        {
            lock (queueLock)
            {
                if (packetQueue.Count == 0)
                {
                    return null;
                }
                else
                {
                    return packetQueue.Dequeue();
                }
            }
        }
    }
}
