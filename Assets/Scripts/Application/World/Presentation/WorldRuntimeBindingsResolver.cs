using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIDesktopPetty.Application.World
{
    public static class WorldRuntimeBindingsResolver
    {
        public static bool TryResolve(
            Scene scene,
            out WorldRuntimeBindings bindings,
            out string error)
        {
            bindings = null;

            if (!scene.IsValid()
                || !scene.isLoaded)
            {
                error =
                    "目标 World Scene 无效或尚未加载。";

                return false;
            }

            GameObject[] roots =
                scene.GetRootGameObjects();

            int foundCount = 0;

            for (int i = 0;
                 i < roots.Length;
                 i++)
            {
                WorldRuntimeBindings candidate =
                    roots[i]
                        .GetComponentInChildren<
                            WorldRuntimeBindings>(
                            true);

                if (candidate == null)
                    continue;

                foundCount++;

                if (foundCount == 1)
                {
                    bindings = candidate;
                }
            }

            if (foundCount == 0)
            {
                error =
                    $"Scene '{scene.name}' 中没有 " +
                    $"WorldRuntimeBindings。";

                return false;
            }

            if (foundCount > 1)
            {
                error =
                    $"Scene '{scene.name}' 中存在多个 " +
                    $"WorldRuntimeBindings。";

                bindings = null;
                return false;
            }

            if (!bindings.TryValidate(
                    out string bindingError))
            {
                error =
                    $"WorldRuntimeBindings 无效：" +
                    $"{bindingError}";

                bindings = null;
                return false;
            }

            error = null;
            return true;
        }
    }
}