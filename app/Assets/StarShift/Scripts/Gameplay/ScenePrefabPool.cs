// ScenePrefabPool — Kho tái sử dụng địch và vật phẩm.
using System.Collections.Generic;
using UnityEngine;

namespace StarShift.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ScenePrefabPool : MonoBehaviour
    {
        private Transform storage;
        private readonly List<(GameObject Root, GameObject Prefab)> entries = new List<(GameObject, GameObject)>();
        private void EnsureStorage()
        {
            if (storage != null) return;
            storage = new GameObject("EnemyPool").transform;
            storage.SetParent(transform, false);
            storage.gameObject.SetActive(false);
        }
        public void ReclaimInactive()
        {
            EnsureStorage();
            foreach (var e in entries) if (e.Root != null && !e.Root.activeSelf) e.Root.transform.SetParent(storage, false);
        }
        // Tìm đối tượng đang cất đúng prefab; chỉ Instantiate khi kho chưa có đối tượng phù hợp.
        public GameObject Rent(GameObject prefab)
        {
            EnsureStorage();
            foreach (var e in entries)
                if (e.Root != null && e.Prefab == prefab && e.Root.transform.parent == storage && !e.Root.activeSelf) return e.Root;
            var root = Instantiate(prefab, storage);
            root.SetActive(false);
            entries.Add((root, prefab));
            return root;
        }
        // Tắt và cất đối tượng để tái sử dụng; không Destroy sau mỗi lần dùng.
        public void Return(GameObject root)
        {
            if (root == null) return;
            root.SetActive(false);
            // Unity không cho reparent giữa parent activation traversal.
            if (gameObject.activeInHierarchy) root.transform.SetParent(storage, false);
        }
    }
}
