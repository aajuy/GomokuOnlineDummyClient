using GomokuOnlineDummyClient.Data.DTOs;
using GomokuOnlineDummyClient.Game;
using GomokuOnlineDummyClient.Game.Packet;
using GomokuOnlineDummyClient.Match;
using GomokuOnlineDummyClient.Match.Packet;
using Google.Protobuf.MatchProtocol;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;

namespace GomokuOnlineDummyClient
{
    class Program
    {
        static Stopwatch stopWatch = new Stopwatch();
        static JsonSerializerOptions options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        static void Main(string[] args)
        {
            int iterations = 10;
            Test(iterations).Wait();
        }

        static async Task Test(int iterations)
        {
            // Create new account
            Guid guid = Guid.NewGuid();
            string username = guid.ToString().Substring(0, 12);
            string password = guid.ToString().Substring(12, 12);

            // Create stopwatch
            stopWatch.Start();
            stopWatch.Stop();

            // Start time
            Console.WriteLine($"Start time: {DateTime.UtcNow}");

            // Register
            RegisterRequestDto registerRequestDto = new RegisterRequestDto()
            {
                Username = username,
                Password = password
            };
            await Register(registerRequestDto);

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

            // Ranking
            await Rankings();

            // Stamina
            await Stamina();

            // Match / Game
            for (int i = 0; i < iterations; i++)
            {
                Console.WriteLine($"Match / Game {i}:");
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
        }

        static async Task Register(RegisterRequestDto registerRequestDto)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(ServerConfig.LoginServerAddress);
                stopWatch.Restart();
                await client.PostAsJsonAsync("register", registerRequestDto);
                stopWatch.Stop();
                Console.WriteLine($"Register:{stopWatch.ElapsedMilliseconds}");
            }
        }

        static async Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(ServerConfig.LoginServerAddress);
                HttpResponseMessage response = await client.PostAsJsonAsync("login", loginRequestDto, options);
                stopWatch.Restart();
                LoginResponseDto loginResponseDto = await response.Content.ReadFromJsonAsync<LoginResponseDto>(options);
                stopWatch.Stop();
                Console.WriteLine($"Login:{stopWatch.ElapsedMilliseconds}");
                return loginResponseDto;
            }
        }

        static async Task Stamina()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://" + ServerConfig.MatchServerAddress);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SessionId", MyInfo.Instance.SessionId);
                stopWatch.Restart();
                await client.GetAsync($"stamina/{MyInfo.Instance.UserId}");
                stopWatch.Stop();
                Console.WriteLine($"Stamina:{stopWatch.ElapsedMilliseconds}");
            }
        }

        static async Task Rankings()
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://" + ServerConfig.MatchServerAddress);
                stopWatch.Restart();
                HttpResponseMessage response = await client.GetAsync("ranking?from=1&to=99");
                stopWatch.Stop();
                Console.WriteLine($"Ranking:{stopWatch.ElapsedMilliseconds}");
                RankingEntry[] rankingEntries = await response.Content.ReadFromJsonAsync<RankingEntry[]>(options);
            }
        }

        static void Match()
        {
            IPAddress iPAddress = IPAddress.Parse(ServerConfig.MatchServerAddress);
            IPEndPoint endPoint = new IPEndPoint(iPAddress, 6789);

            stopWatch.Restart();
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
                    stopWatch.Stop();
                    Console.WriteLine($"Match:{stopWatch.ElapsedMilliseconds}");
                    return;
                }
            }
        }

        static void Game()
        {
            IPAddress iPAddress = IPAddress.Parse(ServerConfig.GameServerAddress);
            IPEndPoint endPoint = new IPEndPoint(iPAddress, 5678);

            stopWatch.Restart();
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
                    stopWatch.Stop();
                    Console.WriteLine($"Game:{stopWatch.ElapsedMilliseconds}");
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
                    stopWatch.Stop();
                    Console.WriteLine($"Game:{stopWatch.ElapsedMilliseconds}");
                    return;
                }
            }
        }

        static async Task Logout(LogoutRequestDto logoutRequestDto)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(ServerConfig.LoginServerAddress);
                stopWatch.Restart();
                await client.PostAsJsonAsync("logout", logoutRequestDto);
                stopWatch.Stop();
                Console.WriteLine($"Logout:{stopWatch.ElapsedMilliseconds}");
            }
        }
    }
}