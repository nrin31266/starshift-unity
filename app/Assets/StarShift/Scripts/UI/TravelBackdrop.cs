using UnityEngine;

namespace StarShift.UI
{
    // Nền lặp giữ nguyên kích thước artwork; hai lớp hòa màu trong cùng một hành trình.
    internal sealed class TravelBackdrop
    {
        private readonly SpriteRenderer source;
        private readonly SpriteRenderer[] departure = new SpriteRenderer[25];
        private readonly SpriteRenderer[] arrival = new SpriteRenderer[25];
        private readonly Transform root;
        private readonly bool sourceWasEnabled;
        private Vector2 offset;

        public TravelBackdrop(SpriteRenderer source)
        {
            this.source = source;
            sourceWasEnabled = source.enabled;
            root = new GameObject("TravelBackdrop").transform;
            root.SetParent(source.transform.parent, false);
            root.localPosition = source.transform.localPosition;
            root.localRotation = source.transform.localRotation;
            root.localScale = source.transform.localScale;
            for (int i = 0; i < departure.Length; i++)
            {
                departure[i] = Create("Departure", source.sortingOrder);
                arrival[i] = Create("Arrival", source.sortingOrder + 1);
            }
        }

        private SpriteRenderer Create(string name, int order)
        {
            var tile = new GameObject(name).AddComponent<SpriteRenderer>();
            tile.transform.SetParent(root, false);
            tile.sharedMaterial = source.sharedMaterial;
            tile.sortingLayerID = source.sortingLayerID;
            tile.sortingOrder = order;
            tile.gameObject.layer = source.gameObject.layer;
            return tile;
        }

        public void Tick(Sprite from, Sprite to, float blend, Vector2 velocity, float delta)
        {
            if (from == null || to == null) return;
            source.enabled = false;
            var size = (Vector2)from.bounds.size;
            var localVelocity = root.InverseTransformVector(velocity);
            offset += new Vector2(localVelocity.x, localVelocity.y) * delta;
            offset.x = Mathf.Repeat(offset.x + size.x / 2, size.x) - size.x / 2;
            offset.y = Mathf.Repeat(offset.y + size.y / 2, size.y) - size.y / 2;
            var tint = source.color;
            var arrivalTint = tint; arrivalTint.a *= Mathf.SmoothStep(0, 1, blend);
            // Vòng 5×5 phủ cả framing 20 ở landscape rộng khi tâm tile đang cuộn.
            for (int y = -2, i = 0; y <= 2; y++) for (int x = -2; x <= 2; x++, i++)
            {
                var position = new Vector3(x * size.x + offset.x, y * size.y + offset.y, 0);
                departure[i].transform.localPosition = position;
                departure[i].sprite = from; departure[i].color = tint;
                arrival[i].transform.localPosition = position;
                arrival[i].sprite = to; arrival[i].color = arrivalTint;
                arrival[i].enabled = blend > 0;
            }
        }

        public void Reset() => offset = Vector2.zero;
        public void Dispose()
        {
            if (source != null) source.enabled = sourceWasEnabled;
            if (root != null) Object.Destroy(root.gameObject);
        }
    }
}
