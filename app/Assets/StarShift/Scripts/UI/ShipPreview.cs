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
        private Vector2 restPosition;
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
                    if (Ribbons[i] != null)
                    {
                        Ribbons[i].gameObject.SetActive(i == 0 && exhaust.TrailSeconds > 0);
                        float tail=ship.EngineSockets[0].y;
                        foreach(var engine in ship.EngineSockets) tail=Mathf.Min(tail,engine.y);
                        Ribbons[i].rectTransform.anchoredPosition = new Vector2(ship.MovementTrailSocket.x*56.25f,tail*56.25f-16)*fit;
                        Ribbons[i].rectTransform.sizeDelta = new Vector2(5, 28) * fit;
                        Ribbons[i].color = new Color(1, 1, 1, .9f);
                    }
            }
        }
        private void OnEnable() { age = 0; restPosition = Hull.rectTransform.anchoredPosition; }
        private void OnDisable() { Hull.rectTransform.anchoredPosition = restPosition; Hull.rectTransform.localRotation = Quaternion.identity; }
        private void LateUpdate()
        {
            if (chassis == null) return;
            age += Time.unscaledDeltaTime;
            for (int i = 0; i < Engines.Length; i++)
            {
                // Plume giữ nguyên theo chassis khi đổi cosmetic trail.
                if (catalog.EngineFrames.Length > 0) Engines[i].sprite = catalog.EngineFrames[(int)(age / .075f + i) % catalog.EngineFrames.Length];
            }
            if (Ribbons != null && Ribbons.Length > 0 && Ribbons[0] != null && trail.AnimationFrames.Length > 0)
                Ribbons[0].sprite = trail.AnimationFrames[(int)(age / .1f) % trail.AnimationFrames.Length];
            Hull.rectTransform.anchoredPosition = restPosition + new Vector2(Mathf.Sin(age * .9f) * 2, Mathf.Sin(age * 1.2f) * 2);
            Hull.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(age * 1.1f) * 1.4f);
        }
    }
}
