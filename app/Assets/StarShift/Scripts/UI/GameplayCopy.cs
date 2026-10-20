using StarShift.Combat;

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
        public const string MainMenu = "MÀN HÌNH CHÍNH";
        public const string MissionClear = "NHIỆM VỤ HOÀN THÀNH";
        public const string GameOver = "THẤT BẠI";
        public const string Placeholder = "—";
        public const string PulseLaser = "Pulse Laser";
        public const string HeatOverheated = "KHÓA";
        public const string HeatHot = "NÓNG";
        public static string Encounter(int number) => $"ĐỢT {number}";
        public static string Weapon(int level) => $"{PulseLaser} · CẤP {level}";
        public static string WeaponStatus(int level) => $"Pulse · CẤP {level}";
        public static string WeaponLevel(int level) => $"LV{level}";
        public static string WeaponName(WeaponBranch branch) => branch == WeaponBranch.Pulse ? "Pulse" : branch == WeaponBranch.Plasma ? "Plasma" : "Homing";
        public static string Lives(int count) => $"×{count}";
    }
}
