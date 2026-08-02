using UnityEngine;
using UnityEngine;
using wackydatabase.Datas;
using wackydatabase.OBJimporter;

namespace wackydatabase.SetData
{
    internal class ObjectSetMock
    {
        private static Transform mockRoot;

        internal static GameObject Create(WItemData data, ObjectDB objectDb)
        {
            GameObject template = ObjModelLoader.MockItemBase;
            if (!string.IsNullOrEmpty(data.mockBasePrefab))
            {
                GameObject donor = objectDb.GetItemPrefab(data.mockBasePrefab);
                if (donor != null && donor.GetComponent<ItemDrop>() != null)
                {
                    template = donor;
                }
                else
                {
                    WMRecipeCust.WLog.LogWarning($"Mock base prefab {data.mockBasePrefab} was not found; using RootCube for {data.name}");
                }
            }

            EnsureRoot();
            GameObject mock = Object.Instantiate(template, mockRoot);
            mock.SetActive(false);
            mock.name = data.name;
            return mock;
        }

        internal static void ReplaceVisual(GameObject mock, GameObject model)
        {
            Transform oldAttach = mock.transform.Find("attach");
            if (oldAttach != null)
            {
                oldAttach.name = "_wdb_original_attach";
                oldAttach.gameObject.SetActive(false);
            }

            Transform cube = mock.transform.Find("Cube");
            if (cube != null)
            {
                cube.gameObject.SetActive(false);
            }

            GameObject newModel = Object.Instantiate(model, mock.transform);
            newModel.SetActive(true);
            newModel.name = "attach";
            newModel.transform.localScale = Vector3.one;

            int itemLayer = LayerMask.NameToLayer("item");
            foreach (Transform child in newModel.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = itemLayer;
            }
        }

        private static void EnsureRoot()
        {
            if (mockRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("WackyDatabase_MockPrefabs");
            root.SetActive(false);
            Object.DontDestroyOnLoad(root);
            mockRoot = root.transform;
        }
    }
}
