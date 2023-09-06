using GomokuOnlineDummyClient.Library;
using Google.Protobuf;
using Google.Protobuf.GameProtocol;

namespace GomokuOnlineDummyClient.Game.Packet
{
    class GamePacketHandler
    {
        static int cnt = 0;
        static readonly int[,] movePosY =
        {
            { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4 },
            { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4 }
        };
        static readonly int[,] movePosX =
        {
            { 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 4 },
            { 5, 6, 7, 8, 5, 6, 7, 8, 5, 6, 7, 8, 5, 6, 7, 8, 5, 6, 7, 8, 9 }
        };

        public static void S_StartHandler(PacketSession session, IMessage packet)
        {
            S_Start sStartPacket = packet as S_Start;
            GameServerSession serverSession = session as GameServerSession;

            MyInfo.Instance.Turn = sStartPacket.Turn;
            //Console.WriteLine($"My turn: {MyInfo.Instance.Turn}");
            if (MyInfo.Instance.Turn == 1)
            {
                Thread.Sleep(2000);
                C_Move p = new C_Move()
                {
                    Y = movePosY[0, cnt],
                    X = movePosX[0, cnt]
                };
                cnt++;
                serverSession.Send(p);
            }
        }

        public static void S_EndHandler(PacketSession session, IMessage packet)
        {
            S_End sEndPacket = packet as S_End;
            GameServerSession serverSession = session as GameServerSession;

            cnt = 0;
            session.Disconnect();
        }

        public static void S_MoveHandler(PacketSession session, IMessage packet)
        {
            S_Move sMovePacket = packet as S_Move;
            GameServerSession serverSession = session as GameServerSession;

            if (sMovePacket.Turn != MyInfo.Instance.Turn)
            {
                Thread.Sleep(2000);
                C_Move p = new C_Move()
                {
                    Y = movePosY[MyInfo.Instance.Turn - 1, cnt],
                    X = movePosX[MyInfo.Instance.Turn - 1, cnt]
                };
                cnt++;
                serverSession.Send(p);
            }
        }
    }
}
