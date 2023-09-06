using GomokuOnlineDummyClient.Library;
using Google.Protobuf;
using Google.Protobuf.MatchProtocol;

namespace GomokuOnlineDummyClient.Match.Packet
{
    class PacketHandler
    {
        public static void S_ReadyHandler(PacketSession session, IMessage packet)
        {
            S_Ready sReadyPacket = packet as S_Ready;
            ServerSession serverSession = session as ServerSession;

            MyInfo.Instance.RoomId = sReadyPacket.RoomId;

            session.Disconnect();
            //LoadingSceneController.LoadScene("GameScene");
        }

        public static void S_ResponseHandler(PacketSession session, IMessage packet)
        {
            S_Response sResponsePacket = packet as S_Response;
            ServerSession serverSession = session as ServerSession;

            if (sResponsePacket.Successed)
            {
                MyInfo.Instance.Waiting = true;
            }
        }
    }
}
