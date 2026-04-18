//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using Godot;
using System;
using System.Collections.Generic;

namespace GodotGameFramework
{
    /// <summary>
    /// 游戏框架入口类。
    ///
    /// 这是整个 GGF 框架的核心入口点，作为 Godot 的 Autoload（自动加载）节点运行。
    /// 它负责管理所有框架组件的生命周期，并驱动核心框架的 Update 循环。
    ///
    /// 架构说明：
    /// ┌─────────────────────────────────────┐
    /// │  GGFEntry (Autoload Node)           │
    /// │  - 管理 GGFComponent 列表           │
    /// │  - 驱动 GameFrameworkEntry.Update() │
    /// │  ┌─────────────────────────────────┐│
    /// │  │ GGFBaseComponent               ││  ← 初始化 Helper
    /// │  ├─────────────────────────────────┤│
    /// │  │ EventComponent                  ││  ← 事件系统
    /// │  ├─────────────────────────────────┤│
    /// │  │ FsmComponent                    ││  ← 状态机
    /// │  ├─────────────────────────────────┤│
    /// │  │ ProcedureComponent              ││  ← 流程管理
    /// │  └─────────────────────────────────┘│
    /// └─────────────────────────────────────┘
    ///
    /// 使用方式：
    /// 1. 将 GGFEntry 配置为 Godot 的 Autoload（在项目设置中添加）
    /// 2. 将各 GGFComponent 作为 GGFEntry 的子场景或子节点添加
    /// 3. 通过 GGFEntry.GetComponent&lt;T&gt;() 获取组件
    ///
    /// 对应 Unity 版本中的 GameEntry。
    /// </summary>
    public partial class GGFEntry : Node
    {
        /// <summary>
        /// 存储所有已注册的框架组件的链表。
        /// 使用 GameFrameworkLinkedList 以保持与核心框架的一致性。
        /// </summary>
        private static readonly GameFrameworkLinkedList<GGFComponent> s_GGFComponents =
            new GameFrameworkLinkedList<GGFComponent>();

        /// <summary>
        /// 引用 GGFBaseComponent，用于框架关闭时的特殊处理。
        /// </summary>
        private static GGFBaseComponent s_BaseComponent = null;

        /// <summary>
        /// 框架是否已经关闭的标记。
        /// 防止在关闭过程中重复执行关闭逻辑。
        /// </summary>
        private static bool s_Shutdown = false;

        /// <summary>
        /// 节点进入场景树时自动调用。
        /// 这是 Godot 生命周期中的初始化阶段。
        ///
        /// Godot 的 _Ready 调用顺序是：先子节点，后父节点。
        /// 所以当 GGFEntry._Ready 被调用时，所有子组件已经完成了注册。
        /// </summary>
        public override void _Ready()
        {
            // 所有子组件已完成 _Ready 和注册
        }

        /// <summary>
        /// 每帧自动调用。
        /// 这是驱动核心框架 Update 循环的关键方法。
        ///
        /// 工作流程：
        /// 1. 获取 Godot 的帧间隔时间（delta）
        /// 2. 调用 GameFrameworkEntry.Update() 驱动所有核心模块更新
        /// 3. 核心模块（如 EventPool、Fsm、Procedure）会在 Update 中处理各自逻辑
        /// </summary>
        /// <param name="delta">上一帧到当前帧的耗时（秒）</param>
        public override void _Process(double delta)
        {
            if (s_Shutdown)
            {
                return;
            }

            // 将 Godot 的 double 转为 float，与核心框架保持一致
            // Godot 的 _Process delta 已被 Engine.TimeScale 缩放，对齐 Unity 的 Time.deltaTime
            float elapseSeconds = (float)delta;
            // 计算未缩放的真实经过时间，对齐 Unity 的 Time.unscaledDeltaTime
            float realElapseSeconds = (float)Engine.TimeScale > 0f
                ? elapseSeconds / (float)Engine.TimeScale
                : 0f;

            // 驱动核心框架所有模块的 Update
            GameFrameworkEntry.Update(elapseSeconds, realElapseSeconds);
        }

        /// <summary>
        /// 获取指定类型的框架组件。
        ///
        /// 这是用户获取框架组件的主要方式。
        ///
        /// 示例：
        /// <code>
        /// EventComponent eventComp = GF.Event;
        /// FsmComponent fsmComp = GF.Fsm;
        /// </code>
        /// </summary>
        /// <typeparam name="T">要获取的组件类型</typeparam>
        /// <returns>找到的组件实例，如果不存在返回 null</returns>
        public static T GetComponent<T>() where T : GGFComponent
        {
            return (T)GetComponent(typeof(T));
        }

        /// <summary>
        /// 通过类型获取框架组件。
        /// </summary>
        /// <param name="type">组件的类型</param>
        /// <returns>找到的组件实例，如果不存在返回 null</returns>
        public static GGFComponent GetComponent(Type type)
        {
            LinkedListNode<GGFComponent> current = s_GGFComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    return current.Value;
                }

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 通过类型全名获取框架组件。
        /// </summary>
        /// <param name="typeName">组件类型的全名（含命名空间）或简名</param>
        /// <returns>找到的组件实例，如果不存在返回 null</returns>
        public static GGFComponent GetComponent(string typeName)
        {
            LinkedListNode<GGFComponent> current = s_GGFComponents.First;
            while (current != null)
            {
                Type type = current.Value.GetType();
                if (type.FullName == typeName || type.Name == typeName)
                {
                    return current.Value;
                }

                current = current.Next;
            }

            return null;
        }

        /// <summary>
        /// 关闭游戏框架。
        ///
        /// 根据不同的关闭类型执行不同的操作：
        /// - None: 仅关闭框架，不退出游戏
        /// - Restart: 关闭框架并重新加载入口场景
        /// - Quit: 关闭框架并退出游戏
        /// </summary>
        /// <param name="shutdownType">关闭类型</param>
        public static void Shutdown(ShutdownType shutdownType)
        {
            s_Shutdown = true;

            // 输出日志
            GD.Print($"[GGF] Shutdown Game Framework ({shutdownType})...");

            // 调用 BaseComponent 的关闭方法，触发核心框架的 Shutdown
            if (s_BaseComponent != null)
            {
                s_BaseComponent.Shutdown();
                s_BaseComponent = null;
            }

            // 清空组件列表
            s_GGFComponents.Clear();

            if (shutdownType == ShutdownType.None)
            {
                return;
            }

            if (shutdownType == ShutdownType.Restart)
            {
                // 重新加载当前场景（重新开始游戏）
                // 通过获取 SceneTree 来切换场景
                var sceneTree = (SceneTree)Engine.GetMainLoop();
                sceneTree.ReloadCurrentScene();
                return;
            }

            if (shutdownType == ShutdownType.Quit)
            {
                // 退出游戏
                var sceneTree = (SceneTree)Engine.GetMainLoop();
                sceneTree.Quit();
                return;
            }
        }

        /// <summary>
        /// 注册框架组件。
        ///
        /// 此方法由 GGFComponent._Ready() 自动调用，将组件添加到管理列表中。
        /// 通常不需要手动调用。
        ///
        /// 注意：每种类型的组件只能注册一个实例，重复注册会被忽略并输出错误日志。
        /// </summary>
        /// <param name="component">要注册的框架组件</param>
        internal static void RegisterComponent(GGFComponent component)
        {
            if (component == null)
            {
                GD.PrintErr("[GGF] Game Framework component is invalid.");
                return;
            }

            Type type = component.GetType();

            // 检查是否已经有同类型的组件注册
            LinkedListNode<GGFComponent> current = s_GGFComponents.First;
            while (current != null)
            {
                if (current.Value.GetType() == type)
                {
                    GD.PrintErr($"[GGF] Game Framework component type '{type.FullName}' is already exist.");
                    return;
                }

                current = current.Next;
            }

            s_GGFComponents.AddLast(component);

            // 如果是 BaseComponent，保存特殊引用
            if (component is GGFBaseComponent baseComponent)
            {
                s_BaseComponent = baseComponent;
            }
        }
    }
}
