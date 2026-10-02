using System;
using System.Collections.Generic;
using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    /// <summary>
    /// 当前客户端可进入世界的目录。
    ///
    /// UI / Coordinator 使用稳定 WorldId 查找 WorldDefinition。
    /// </summary>
    [CreateAssetMenu(
        menuName = "AI Desktop Petty/World/World Catalog",
        fileName = "WorldCatalog")]
    public sealed class WorldCatalog : ScriptableObject
    {
        [SerializeField]
        private List<WorldDefinition> worlds =
            new List<WorldDefinition>();

        public bool TryGet(
            string worldId,
            out WorldDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(worldId))
                return false;

            for (int i = 0; i < worlds.Count; i++)
            {
                WorldDefinition candidate = worlds[i];

                if (candidate == null)
                    continue;

                if (string.Equals(
                        candidate.WorldId,
                        worldId,
                        StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            return false;
        }

        public bool TryValidate(out string error)
        {
            HashSet<string> ids =
                new HashSet<string>(StringComparer.Ordinal);

            for (int i = 0; i < worlds.Count; i++)
            {
                WorldDefinition definition = worlds[i];

                if (definition == null)
                {
                    error =
                        $"WorldCatalog 第 {i} 项为空。";

                    return false;
                }

                if (!definition.TryValidate(
                        out string definitionError))
                {
                    error =
                        $"WorldCatalog 中存在无效世界：{definitionError}";

                    return false;
                }

                if (!ids.Add(definition.WorldId))
                {
                    error =
                        $"WorldCatalog 中存在重复 WorldId：" +
                        $"{definition.WorldId}";

                    return false;
                }
            }

            error = null;
            return true;
        }
    }
}