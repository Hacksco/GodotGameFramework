//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using Godot;

namespace GodotGameFramework
{
    /// <summary>
    /// 游戏框架组件抽象基类。
    ///
    /// 所有 GGF 框架组件都必须继承此类。
    /// 它继承自 Godot 的 Node，并在 _Ready() 时自动将自身注册到 GGFEntry。
    ///
    /// 工作原理：
    /// 1. 每个组件作为 GGFEntry 的子节点存在
    /// 2. _Ready() 被调用时，组件自动注册到 GGFEntry 的组件列表
    /// 3. 用户通过 GF.ComponentName 获取需要的组件（如 GF.Event、GF.Fsm 等）
    ///
    /// 对应 Unity 版本中的 GameFrameworkComponent（MonoBehaviour）。
    /// </summary>
    public abstract partial class GGFComponent : Node
    {
        /// <summary>
        /// Godot 节点初始化回调。
        /// 当节点进入场景树时自动调用。
        /// 在这里将自身注册到 GGFEntry 组件列表中。
        /// </summary>
        public override void _Ready()
        {
            GGFEntry.RegisterComponent(this);
        }
    }
}
