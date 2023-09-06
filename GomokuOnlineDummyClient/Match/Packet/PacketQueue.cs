using Google.Protobuf;

namespace GomokuOnlineDummyClient.Match.Packet
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

    public class PacketQueue
    {
        private static PacketQueue instance = new PacketQueue();
        public static PacketQueue Instance { get { return instance; } }

        object queueLock = new object();
        Queue<PacketMessage> packetQueue = new Queue<PacketMessage>();

        private PacketQueue()
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
