using GomokuOnlineDummyClient.Data.DTOs;
using GomokuOnlineDummyClient.Game;
using GomokuOnlineDummyClient.Game.Packet;
using GomokuOnlineDummyClient.Match;
using GomokuOnlineDummyClient.Match.Packet;
using Google.Protobuf.MatchProtocol;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

namespace GomokuOnlineDummyClient
{
    class Program
    {
        static JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }

            int iterations = Int32.Parse(args[0]);
            int result = Test(iterations).Result;
            Console.WriteLine(result);
        }

        static async Task<int> Test(int iterations)
        {
            // Start stopwatch
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            // New account
            Guid guid = Guid.NewGuid();
            string username = guid.ToString().Substring(0, 12);
            string password = guid.ToString().Substring(12, 12);

            // Register
            RegisterRequestDto registerRequestDto = new RegisterRequestDto()
            {
                Username = username,
                Password = password
            };
            await Register(registerRequestDto);
            //Console.WriteLine("Register finished");

            // Login
            LoginRequestDto loginRequestDto = new LoginRequestDto()
            {
                Username = username,
                Password = password
            };
            LoginResponseDto loginResponseDto = await Login(loginRequestDto);
            MyInfo.Instance.UserId = loginResponseDto.UserId;
            MyInfo.Instance.SessionId = loginResponseDto.SessionId;
            ServerConfig.MatchServerAddress = loginResponseDto.MatchServerAddress;
            ServerConfig.GameServerAddress = loginResponseDto.GameServerAddress;
            //Console.WriteLine("Login finished");

            // Ranking
            await Rankings();

            // Match / Game
            for (int i = 0; i < iterations; i++)
            {
                Match();
                Game();
            }

            // Logout
            LogoutRequestDto logoutRequestDto = new LogoutRequestDto()
            {
                UserId = MyInfo.Instance.UserId,
                SessionId = MyInfo.Instance.SessionId
            };
            await Logout(logoutRequestDto);

            // Stop stopwatch
            stopWatch.Stop();

            // Add result to executionTimes
            TimeSpan ts = stopWatch.Elapsed;
            return (int)ts.TotalSeconds;
        }

        static async Task Register(RegisterRequestDto registerRequestDto)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(ServerConfig.LoginServerAddress);
                await client.PostAsJsonAsync("register", registerRequestDto);
            }
        }

        static async Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(ServerConfig.LoginServerAddress);
                HttpResponseMessage response = await client.PostAsJsonAsync("login", loginRequestDto, options);
                LoginResponseDto loginResponseDto = await response.Content.ReadFromJsonAsync<LoginResponseDto>(options);
                return loginResponseDto;
            }
        }

        static async Task Stamina()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://" + ServerConfig.MatchServerAddress);
                await client.GetAsync($"match/stamina/{MyInfo.Instance.UserId}");
            }
        }

        static async Task Rankings()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://" + ServerConfig.MatchServerAddress);
                HttpResponseMessage response = await client.GetAsync("ranking?from=1&to=99");
                RankingEntry[] rankingEntries = await response.Content.ReadFromJsonAsync<RankingEntry[]>(options);
            }
        }

        static void Match()
        {
            IPAddress iPAddress = IPAddress.Parse(ServerConfig.MatchServerAddress);
            IPEndPoint endPoint = new IPEndPoint(iPAddress, 6789);

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);

            ServerSession Session = new ServerSession();
            Session.Start(socket);
            Session.Authorize();

            C_Join cJoinPacket = new C_Join();
            Session.Send(cJoinPacket);
            //Console.WriteLine("Join request finished");

            while (true)
            {
                Thread.Sleep(100);

                Match.Packet.PacketMessage packetMessage = PacketQueue.Instance.Pop();
                if (packetMessage == null)
                {
                    continue;
                }

                var action = PacketManager.Instance.GetPacketHandler(packetMessage.packetId, packetMessage.packet);
                if (action == null)
                {
                    continue;
                }

                action.Invoke(Session, packetMessage.packet);
                if (Session.disconnected == 1)
                {
                    return;
                }
            }
        }

        static void Game()
        {
            IPAddress iPAddress = IPAddress.Parse(ServerConfig.GameServerAddress);
            IPEndPoint endPoint = new IPEndPoint(iPAddress, 5678);

            Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(endPoint);

            GameServerSession Session = new GameServerSession();
            Session.Start(socket);
            Session.Authorize();
            Session.EnterRoom();

            while (true)
            {
                Thread.Sleep(100);
                if (Session.disconnected == 1)
                {
                    return;
                }

                Game.Packet.PacketMessage packetMessage = GamePacketQueue.Instance.Pop();
                if (packetMessage == null)
                {
                    continue;
                }

                var action = GamePacketManager.Instance.GetPacketHandler(packetMessage.packetId, packetMessage.packet);
                if (action == null)
                {
                    continue;
                }

                action.Invoke(Session, packetMessage.packet);
                if (Session.disconnected == 1)
                {
                    return;
                }
            }
        }

        static async Task Logout(LogoutRequestDto logoutRequestDto)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(ServerConfig.LoginServerAddress);
                await client.PostAsJsonAsync("logout", logoutRequestDto);
            }
        }
    }
}