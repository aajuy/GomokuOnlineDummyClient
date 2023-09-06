using GomokuOnlineDummyClient.Library;
using GomokuOnlineDummyClient.Match.Packet;
using Google.Protobuf;
using Google.Protobuf.MatchProtocol;
using System.Net;
using System.Text;

namespace GomokuOnlineDummyClient.Match
{
    public class ServerSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            //Console.WriteLine($"OnConnected : {endPoint}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            //Console.WriteLine($"OnDisconnected : {endPoint}");
        }

        public override void OnPacketReceived(ArraySegment<byte> buffer)
        {
            PacketManager.Instance.OnPacketReceived(this, buffer);
        }

        public override void OnSent(int bytesTransferred)
        {
            //Console.WriteLine($"OnSent : {bytesTransferred}");
        }

        public void Send(IMessage packet)
        {
            string msgName = packet.Descriptor.Name.Replace("_", string.Empty);
            PacketId msgId = (PacketId)Enum.Parse(typeof(PacketId), msgName);
            ushort size = (ushort)packet.CalculateSize();
            byte[] sendBuffer = new byte[size + 4];
            Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
            Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);
            Send(new ArraySegment<byte>(sendBuffer));
        }

        public void Authorize()
        {
            int userId = MyInfo.Instance.UserId;
            string sessionId = MyInfo.Instance.SessionId;

            byte[] array = new byte[40];
            BitConverter.GetBytes(userId).CopyTo(array, 0);
            Encoding.Default.GetBytes(sessionId).CopyTo(array, 4);

            ArraySegment<byte> segment = new ArraySegment<byte>(array, 0, 40);
            Send(segment);
        }
    }
}
