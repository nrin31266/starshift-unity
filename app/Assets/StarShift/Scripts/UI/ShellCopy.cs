using System;
using StarShift.Persistence;

namespace StarShift.UI
{
    public static class ShellCopy
    {
        public const string Continue = "TIẾP TỤC";
        public const string NewGame = "CHƠI MỚI";
        public const string Missions = "NHIỆM VỤ";
        public const string Help = "CÁCH CHƠI";
        public const string Credits = "GIỚI THIỆU";
        public const string Back = "QUAY LẠI";
        public const string Replay = "CHƠI LẠI NHIỆM VỤ";
        public const string Next = "NHIỆM VỤ TIẾP THEO";
        public const string Victory = "CHIẾN THẮNG";
        public const string ReleaseComplete = "Đã hoàn thành Region A và Region B";
        public const string ConfirmNew = "Bắt đầu hành trình mới?\nTiến độ chiến dịch hiện tại sẽ được thay thế. Shard, kỷ lục, ngoại hình và cài đặt được giữ lại.";
        public const string ConfirmExit = "Rời nhiệm vụ?\nKhi tiếp tục, bạn sẽ trở về đầu đợt đã lưu. Phần đang chơi sẽ được bỏ qua.";
        public const string ConfirmRetry = "Chơi lại từ đầu đợt đã lưu?\nĐiểm, thời gian và trang bị sẽ trở về checkpoint.";
        public const string Confirm = "ĐỒNG Ý";
        public const string Cancel = "HỦY";
        public const string Loading = "ĐANG CHUẨN BỊ…";
        public const string RetrySave = "THỬ LƯU LẠI";
        public const string SaveSettings = "LƯU VÀ QUAY LẠI";
        public const string SaveFailed = "Chưa lưu được tiến độ. Hãy thử lưu lại trước khi tiếp tục.";
        public const string StartFailed = "Chưa thể mở nhiệm vụ. Bạn có thể quay lại và thử lại.";
        public const string SettingsFailed = "Chưa lưu được cài đặt. Giá trị trước đó vẫn được giữ. Hãy thử lại.";
        public const string Hangar = "XƯỞNG TÀU";
        public const string ChangeShip = "ĐỔI TÀU";
        public const string Equipped = "ĐANG TRANG BỊ";
        public const string Equip = "TRANG BỊ";
        public const string Owned = "ĐÃ SỞ HỮU";
        public const string CosmeticFailed = "Chưa lưu được lựa chọn. Shard và trang bị trước đó vẫn được giữ. Hãy thử lại.";
        public static readonly string[] CosmeticTabs = { "TÀU / CHASSIS", "SƠN / SKIN", "ĐỘNG CƠ / TRAIL" };
        public static string Purchase(int cost) => $"MUA & TRANG BỊ · {cost} SHARD";
        public static string ConfirmPurchase(string name, int cost) => $"Mua {name} với {cost} Stellar Shard?\nChỉ thay đổi ngoại hình; không thay chỉ số chiến đấu.";
        public static string NeedShards(int cost) => $"CẦN THÊM {cost} SHARD";
        public const string RetryRead = "THỬ ĐỌC LẠI";
        public const string RecoverSave = "TẠO CHIẾN DỊCH MỚI";
        public const string ConfirmRecovery = "Tạo lại chiến dịch từ dữ liệu lỗi?\nTiến độ chiến dịch sẽ bắt đầu lại. Hồ sơ và cài đặt còn hợp lệ được giữ; phần không đọc được sẽ được tạo mới. Bản dữ liệu lỗi được giữ để khôi phục về sau.";
        public const string UnsupportedSave = "Bản lưu thuộc phiên bản không tương thích. Hãy mở bằng phiên bản phù hợp; dữ liệu được giữ nguyên.";
        public const string ReadFailed = "Chưa đọc được dữ liệu đã lưu. Dữ liệu được giữ nguyên. Hãy thử đọc lại.";
        public const string Recovered = "Đã khôi phục bản lưu dự phòng. Một phần tiến độ gần nhất có thể chưa được lưu.";
        public const string EmptyHub = "Chọn CHƠI MỚI ở Menu để mở nhiệm vụ đầu tiên.";
        public const string NoCampaign = "Chọn CHƠI MỚI để bắt đầu hành trình.";
        public const string Music = "NHẠC";
        public const string Sfx = "HIỆU ỨNG ÂM THANH";
        public const string Vibration = "RUNG";
        public const string Shake = "RUNG MÀN HÌNH";
        public const string HelpText = "DI CHUYỂN VÀ BẮN\nKéo tương đối ở vùng trống để di chuyển. Giữ ngón tay để bắn; nhấc tay để ngừng bắn.\n\nVŨ KHÍ VÀ NHIỆT\nChạm ô vũ khí để đổi nhánh đã mở. Các nhánh dùng chung nhiệt. Quá nóng sẽ tạm ngừng bắn; chờ hạ nhiệt để bắn lại.\n\nSỐNG SÓT VÀ TIẾP TẾ\nMất mạng làm giảm cấp vũ khí đang dùng. Sau hồi sinh có bảo vệ ngắn. Hạ tiếp tế rồi nhặt Core, hỗ trợ hoặc Shard. M là tên lửa dự trữ.\n\nTẠM DỪNG\nChạm Ⅱ để dừng, đổi cài đặt hoặc chơi lại đợt đã lưu.";
        public const string CreditsText = "STARSHIFT\n\nBản chơi hiện tại: 8 nhiệm vụ · Region A + Region B.\nĐược xây dựng bằng Unity.\n\nHình ảnh và giao diện hiện tại là bản tạm phục vụ gameplay.\nThông tin tác giả, giấy phép và nguồn tài sản sẽ được bổ sung khi tài sản phát hành được chọn.";
        public static string Mission(int index) => $"NHIỆM VỤ {index + 1:00}";
        public static string Time(float seconds)
        {
            int total = (int)Math.Min(int.MaxValue, Math.Max(0d, seconds));
            return total >= 3600 ? $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60:00}:{total % 60:00}";
        }
        public static string Stats(RunStatsData s) => $"ĐIỂM  {s.Score:N0}       HẠ GỤC  {s.EnemiesDestroyed}\nTHỜI GIAN  {Time(s.PlaySeconds)}\n\nShard đã nhặt  {s.Shards}     Core  {s.CoresCollected}     Mạng đã mất  {s.LivesLost}";
        public static string Best(MissionRecord r) => r == null ? "Chưa có kỷ lục hoàn thành" : $"KỶ LỤC  {r.BestScore:N0} điểm  ·  {Time(r.BestTimeSeconds)}";
        public static string Wallet(int value) => $"STELLAR SHARD  {value:N0}";
        public static string MissionState(bool unlocked, bool current, bool cleared) => !unlocked ? "ĐÃ KHÓA" : current ? "ĐANG TIẾN HÀNH" : cleared ? "ĐÃ HOÀN THÀNH" : "ĐÃ MỞ";
    }
}
