using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace wackydatabase.VisualEditor
{
    internal sealed class WackyDbObjectSelector
    {
        private readonly List<WackyDbObjectCandidate> _candidates = new List<WackyDbObjectCandidate>();

        internal List<WackyDbObjectCandidate> GetCandidates()
        {
            return _candidates;
        }

        internal void Refresh()
        {
            Dictionary<string, WackyDbObjectCandidate> candidates =
                new Dictionary<string, WackyDbObjectCandidate>(StringComparer.OrdinalIgnoreCase);

            if (ZNetScene.instance)
            {
                foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
                {
                    AddOrUpdate(candidates, prefab, WackyDbObjectType.Prefab, string.Empty);
                }
            }

            if (ObjectDB.instance)
            {
                foreach (GameObject item in ObjectDB.instance.m_items)
                {
                    AddOrUpdate(candidates, item, WackyDbObjectType.Item, string.Empty);
                }
            }

            PieceTable[] pieceTables = WMRecipeCust.MaybePieceStations;
            if (pieceTables == null || pieceTables.Length == 0)
            {
                pieceTables = Resources.FindObjectsOfTypeAll<PieceTable>();
            }

            foreach (PieceTable pieceTable in pieceTables)
            {
                if (!pieceTable)
                {
                    continue;
                }

                string hammerName = FindHammerName(pieceTable);
                foreach (GameObject piece in pieceTable.m_pieces)
                {
                    AddOrUpdate(candidates, piece, WackyDbObjectType.Piece, hammerName);
                }
            }

            _candidates.Clear();
            _candidates.AddRange(candidates.Values.OrderBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase));
        }

        internal List<WackyDbObjectCandidate> Search(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<WackyDbObjectCandidate>(_candidates);
            }

            string query = text.Trim();
            return _candidates
                .Where(candidate => candidate.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || candidate.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(candidate => candidate.Name.Equals(query, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(candidate => candidate.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        internal WackyDbObjectCandidate Resolve(string prefabName)
        {
            return _candidates.FirstOrDefault(candidate =>
                candidate.Name.Equals(prefabName, StringComparison.OrdinalIgnoreCase));
        }

        private static void AddOrUpdate(
            IDictionary<string, WackyDbObjectCandidate> candidates,
            GameObject prefab,
            WackyDbObjectType type,
            string pieceHammer)
        {
            if (!prefab)
            {
                return;
            }

            string name = Utils.GetPrefabName(prefab);
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            if (!candidates.TryGetValue(name, out WackyDbObjectCandidate candidate))
            {
                candidate = new WackyDbObjectCandidate
                {
                    Name = name,
                    Prefab = prefab,
                    Type = type
                };
                candidates.Add(name, candidate);
            }

            if (type == WackyDbObjectType.Piece ||
                type == WackyDbObjectType.Item && candidate.Type == WackyDbObjectType.Prefab)
            {
                candidate.Type = type;
                candidate.Prefab = prefab;
            }

            if (type == WackyDbObjectType.Piece)
            {
                candidate.PieceHammer = pieceHammer;
            }

            candidate.DisplayName = GetDisplayName(candidate.Prefab);
        }

        private static string GetDisplayName(GameObject prefab)
        {
            Piece piece = prefab.GetComponent<Piece>();
            if (piece && !string.IsNullOrEmpty(piece.m_name))
            {
                return piece.m_name;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop && itemDrop.m_itemData != null && itemDrop.m_itemData.m_shared != null)
            {
                return itemDrop.m_itemData.m_shared.m_name ?? string.Empty;
            }

            return string.Empty;
        }

        private static string FindHammerName(PieceTable pieceTable)
        {
            if (ObjectDB.instance)
            {
                foreach (GameObject item in ObjectDB.instance.m_items)
                {
                    ItemDrop itemDrop = item ? item.GetComponent<ItemDrop>() : null;
                    if (itemDrop && itemDrop.m_itemData?.m_shared?.m_buildPieces == pieceTable)
                    {
                        return Utils.GetPrefabName(item);
                    }
                }
            }

            return pieceTable.name;
        }
    }
}
