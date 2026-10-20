using StarShift.Player;
using UnityEngine;
using UnityEngine.UI;

namespace StarShift.UI
{
    [DisallowMultipleComponent]
    public sealed class ShipPreview : MonoBehaviour
    {
        public Image Hull;
        public Image[] Engines, Ribbons;
        private CosmeticCatalog catalog;
        private CosmeticOption chassis, trail;
        private float age;
        public void Apply(CosmeticCatalog source, CosmeticOption ship, CosmeticOption skin, CosmeticOption exhaust)
        {
            catalog = source; chassis = ship; trail = exhaust;
            Hull.sprite = skin.Id == "skin-amber" && ship.AmberSprite != null ? ship.AmberSprite : ship.Sprite;
            Hull.color = Color.white;
            Hull.rectTransform.localScale = Vector3.one;
            var size = Hull.sprite.rect.size;
            var fit = Mathf.Min(Hull.rectTransform.rect.width / size.x, Hull.rectTransform.rect.height / size.y);
            for (int i = 0; i < Engines.Length; i++)
            {
                var socket = ship.EngineSockets[i % ship.EngineSockets.Length] * (56.25f * fit);
                Engines[i].rectTransform.anchoredPosition = socket + Vector2.down * (20 * fit * ship.PlumeScale);
                Engines[i].rectTransform.sizeDelta = new Vector2(32, 40) * (fit * ship.PlumeScale);
            }
            if (Ribbons != null)
            {
                for (int i = 0; i < Ribbons.Length; i++)
                    if (Ribbons[i] != null) Ribbons[i].gameObject.SetActive(false);
            }
        }
        private void OnEnable() => age = 0;
        private void LateUpdate()
        {
            if (chassis == null) return;
            age += Time.unscaledDeltaTime;
            for (int i = 0; i < Engines.Length; i++)
            {
                // 2 tia lửa phản lực của tàu cố định, không đổi khi đổi vệt động cơ
                if (catalog.EngineFrames.Length > 0) Engines[i].sprite = catalog.EngineFrames[(int)(age / .075f + i) % catalog.EngineFrames.Length];
            }
            Hull.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(age * 1.1f) * 1.4f);
        }
    }
}
