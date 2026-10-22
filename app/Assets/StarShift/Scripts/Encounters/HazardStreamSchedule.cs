using System.Collections.Generic;
using UnityEngine;
namespace StarShift.Encounters
{
    public static class HazardStreamSchedule
    {
        // Lập lịch thiên thạch theo seed: chọn lúc xuất hiện, hướng, kích thước và biến thể; chưa sinh GameObject.
        public static List<SpawnAction> Build(HazardStreamDefinition config, float duration)
        {
            // Cùng cấu hình và seed cho cùng lịch để có thể tái hiện khi kiểm thử.
            var random = new System.Random(config.Seed);
            float Sample(Vector2 range) => Mathf.Lerp(range.x,range.y,(float)random.NextDouble());
            var result = new List<SpawnAction>(config.Count);
            float time = .2f;
            while (result.Count < config.Count && time < duration - .7f)
            {
                int burst = random.Next(1,3);
                for (int i=0; i<burst && result.Count<config.Count; i++)
                {
                    bool right = config.DominantDirection.x >= 0;
                    float x = Mathf.Lerp(right ? -.12f : .25f,right ? .75f : 1.12f,(float)random.NextDouble());
                    float deviation = Mathf.Lerp(-config.AngularVariance,config.AngularVariance,(float)random.NextDouble());
                    var direction = (Vector2)(Quaternion.Euler(0,0,deviation)*config.DominantDirection.normalized);
                    var from = new Vector2(x,1.15f);
                    var entry = from + direction * (.17f / -direction.y);
                    var exit = entry + direction * (1.18f / -direction.y);
                    var variant = config.Variants.Length > 0 ? config.Variants[random.Next(config.Variants.Length)] : null;
                    float scale = variant != null ? variant.Scale : Sample(config.Scale);
                    float weight = Mathf.InverseLerp(config.Scale.x,config.Scale.y,scale);
                    // Mỗi thiên thạch thành một spawn action; EncounterRunner tới lịch mới lấy đối tượng từ pool.
                    result.Add(new SpawnAction { Count=1,StartTime=time+i*.12f,Required=false,EnemyPrefab=config.Prefab,
                        ContinuousTransit=true,SpawnScale=scale,HpOverride=variant != null ? variant.Hp : 0,VisualSpin=Mathf.Lerp(-70,70,(float)random.NextDouble()),
                        EntryAnchor=from,HoldAnchor=entry,ExitAnchor=exit,EntryDuration=.65f,HoldDuration=0,
                        ExitDuration=variant != null ? config.TransitSeconds.x * variant.TransitMultiplier : Mathf.Lerp(config.TransitSeconds.x,config.TransitSeconds.y,weight) });
                }
                time += Sample(config.Interval);
            }
            return result;
        }
    }
}
