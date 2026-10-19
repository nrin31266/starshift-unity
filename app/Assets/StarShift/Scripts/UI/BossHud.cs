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
        private BossPhaseRunner boss;
        private int generation = -1;
        private void Update()
        {
            if (generation != runner.Generation) { generation = runner.Generation; boss = null; }
            if (runner.Stage != EncounterStage.Running || runner.Current.Completion != EncounterCompletion.Boss)
            { group.alpha = 0; return; }
            if (boss == null) boss = runner.GetComponentInChildren<BossPhaseRunner>();
            var health = boss != null ? boss.Health : null;
            group.alpha = health != null && health.IsAlive ? 1 : 0;
            if (health == null) return;
            label.text = boss.Definition.DisplayName + (boss.IsPhaseB ? " · GIAI ĐOẠN 2" : "");
            fill.fillAmount = health.CurrentHp / (float)health.MaxHp;
        }
    }
}
