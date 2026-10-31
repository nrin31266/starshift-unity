// EncounterCleanup — Đăng ký và thu hồi đối tượng theo đợt; cho vật phẩm khoảng chờ nhặt trước khi dọn.
using System.Collections.Generic;
using UnityEngine;

namespace StarShift.Encounters
{
    public enum EncounterObjectCategory { Hostile, Temporary, Reward }
    public interface IEncounterBoundaryObject
    {
        EncounterObjectCategory Category { get; }
        bool IsActive { get; }
        void StopHostility();
        void PreserveForGrace(float seconds);
        void Release();
    }

    [DisallowMultipleComponent]
    public sealed class EncounterCleanup : MonoBehaviour
    {
        private readonly List<IEncounterBoundaryObject> objects = new List<IEncounterBoundaryObject>();
        public bool HasRewards
        {
            get { foreach (var item in objects) if (item.Category == EncounterObjectCategory.Reward && item.IsActive) return true; return false; }
        }
        public int Token { get; private set; }
        public int RewardToken { get; private set; }
        // Đổi mã phạm vi đợt và dọn đối tượng cũ; tùy luồng có thể giữ vật phẩm sang khoảng chờ.
        public void BeginScope(int token, bool preserveRewards = false)
        {
            if (preserveRewards) CloseHostiles();
            else
            {
                ReleaseAll();
                RewardToken = token;
            }
            Token = token;
        }
        // Chỉ nhận đối tượng đúng mã đợt để callback cũ không đăng ký vào đợt mới.
        public bool Register(IEncounterBoundaryObject item, int token)
        {
            if (token != Token || item == null) return false;
            if (!objects.Contains(item)) objects.Add(item);
            return true;
        }
        public void Unregister(IEncounterBoundaryObject item) => objects.Remove(item);
        // Ngừng nguồn nguy hiểm trước; vật phẩm còn có thể được giữ cho người chơi nhặt.
        public void CloseHostiles()
        {
            // Reverse traversal cho phép participant tự unregister trong Release.
            for (int i = objects.Count-1; i >= 0; i--)
            {
                var item = objects[i];
                if (item.Category == EncounterObjectCategory.Reward) continue;
                item.StopHostility();
                item.Release();
            }
        }
        public void BeginGrace(float seconds)
        {
            for (int i = objects.Count-1; i >= 0; i--)
                if (objects[i].Category == EncounterObjectCategory.Reward && objects[i].IsActive) objects[i].PreserveForGrace(seconds);
        }
        // Thu hồi theo chiều ngược vì mỗi đối tượng có thể tự gỡ khỏi danh sách khi được trả pool.
        public void ReleaseAll()
        {
            while (objects.Count > 0)
            {
                var index = objects.Count - 1;
                var item = objects[index];
                objects.RemoveAt(index);
                item.Release();
            }
        }
    }
}
