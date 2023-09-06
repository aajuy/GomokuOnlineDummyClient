namespace GomokuOnlineDummyClient
{
    public class MyInfo
    {
        private static MyInfo instance = new MyInfo();
        public static MyInfo Instance { get { return instance; } }

        public int UserId { get; set; }
        public string SessionId { get; set; }
        public string Username { get; set; }
        public DateTime LastStaminaUpdateTime { get; set; }
        public int Stamina { get; set; } = -1;
        public bool Waiting { get; set; } = false;
        public int RoomId { get; set; }
        public int Turn { get; set; }
    }
}
