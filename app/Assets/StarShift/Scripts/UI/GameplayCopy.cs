namespace StarShift.UI
{
    public static class GameplayCopy
    {
        public const string SpaceTitle = "HÀNH TINH PHÍA TRƯỚC";
        public const string EstablishingDescription = "Một hành tinh hiện ra ở phía xa.";
        public const string TapToContinue = "Chạm để tiếp tục";
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
        public const string HeatOverheated = "NÓNG";
        public static string Encounter(int number) => $"ĐỢT {number}";
        public static string Weapon(int level) => $"{PulseLaser} · CẤP {level}";
        public static string WeaponStatus(int level) => $"T1 L{level}";
        public static string Lives(int count) => $"×{count}";
    }
}
