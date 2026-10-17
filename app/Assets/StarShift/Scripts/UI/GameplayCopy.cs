namespace StarShift.UI
{
    public static class GameplayCopy
    {
        public const string MissionStart = "NHIỆM VỤ 01";
        public const string Pause = "TẠM DỪNG";
        public const string Resume = "TIẾP TỤC";
        public const string RestartEncounter = "CHƠI LẠI ĐỢT";
        public const string Settings = "CÀI ĐẶT";
        public const string MainMenu = "VỀ MENU CHÍNH";
        public const string MissionClear = "NHIỆM VỤ HOÀN THÀNH";
        public const string GameOver = "THẤT BẠI";
        public const string Continue = "TIẾP THEO";
        public const string Unavailable = "Chưa có trong bản thử nghiệm";
        public const string Placeholder = "—";
        public const string PulseLaser = "Pulse Laser";
        public static string Mission(int number) => $"NHIỆM VỤ {number:00}";
        public static string Weapon(int level) => $"{PulseLaser} · CẤP {level}";
        public static string Lives(int count) => $"×{count}";
    }
}
