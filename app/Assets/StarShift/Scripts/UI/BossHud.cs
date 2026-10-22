// BossHud — Hiển thị máu và giai đoạn boss.
using StarShift.Encounters;
using StarShift.Enemies;
using UnityEngine;
using UnityEngine.UI;
namespace StarShift.UI
{
    public sealed class BossHud : MonoBehaviour
    {
        [SerializeField] private EncounterRunner runner;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private Text label;
        [SerializeField] private Image fill;
        [SerializeField] private Text percentage;
        private BossPhaseRunner boss;
        private int generation = -1;
        // Đọc HP khiên hoặc lõi để vẽ thanh máu; UI không quyết định sát thương hay chuyển pha.
        private void Update()
        {
            if (generation != runner.Generation) { generation = runner.Generation; boss = null; }
            if (runner.Stage != EncounterStage.Running || runner.Current.Completion != EncounterCompletion.Boss)
            { group.alpha = 0; return; }
            if (boss == null) boss = runner.GetComponentInChildren<BossPhaseRunner>();
            var health = boss != null ? boss.Health : null;
            group.alpha = health != null && health.IsAlive ? 1 : 0;
            if (health == null) return;
            var shields = boss.Shields;
            bool shielded = shields != null && !shields.IsExposed;
            var phase = shielded ? (shields.RemainingNodes > 0 ? " · NÚT KHIÊN " + shields.RemainingNodes : " · KHIÊN ĐANG VỠ")
                : boss.IsPhaseB ? (shields != null ? " · LÕI LỘ DIỆN" : " · HỎA LỰC TĂNG CƯỜNG") : "";
            label.text = boss.Definition.DisplayName + phase;
            fill.fillAmount = shielded ? shields.CurrentShieldHp / (float)Mathf.Max(1, shields.MaximumShieldHp) : health.CurrentHp / (float)health.MaxHp;
            fill.color = shielded ? new Color(.2f, .8f, 1) : new Color(1, .35f, .2f);
            if (percentage != null) percentage.text = Mathf.CeilToInt(100f * fill.fillAmount) + "%";
        }
    }
}
