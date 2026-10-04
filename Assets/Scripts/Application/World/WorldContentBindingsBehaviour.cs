using UnityEngine;

namespace AIDesktopPetty.Application.World
{
    public abstract class
        WorldContentBindingsBehaviour
        : MonoBehaviour
    {
        /// <summary>
        /// 只验证结构和静态依赖。
        ///
        /// 不应该在这里取得 World 控制权。
        /// </summary>
        public abstract bool TryValidate(
            WorldRuntimeBindings runtimeBindings,
            out string error);


        /// <summary>
        /// Scene 已经准备成为当前 World 后，
        /// 在进入 Explore 前调用。
        /// </summary>
        public virtual bool TryActivate(
            WorldRuntimeBindings runtimeBindings,
            out string error)
        {
            error = null;
            return true;
        }


        /// <summary>
        /// World 退出时释放该 World
        /// 对应用级表现状态的占用。
        /// </summary>
        public virtual void Deactivate()
        {
        }
    }
}