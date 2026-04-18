//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.ObjectPool;
using GameFramework.UI;
using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GodotGameFramework
{
    /// <summary>
    /// 界面组件。
    ///
    /// 这是 UI 窗体管理系统的封装组件，提供通过框架动态打开/关闭 UI 窗体的能力。
    /// 支持界面组管理、深度排序、暂停/覆盖、对象池复用。
    ///
    /// 架构说明：
    /// GGF 的 UIComponent 采用与 EntityComponent 相同的策略 — 绕过核心
    /// UIManager 的 OpenUIForm 管道（因为核心 UIManager.OpenUIForm 内部
    /// 调用 ResourceManager.LoadAsset，需要版本列表），直接使用 ResourceComponent
    /// 加载 PackedScene，自行管理 UI 窗体生命周期。
    ///
    /// 核心 UIManager 仍注册为模块（框架一致性），但不使用其 OpenUIForm/CloseUIForm。
    /// UIComponent 在内部实现了完整的 UI 管理功能，包括：
    /// - UI 窗体追踪（serialId → UIFormInfo）
    /// - UI 组管理（Name → UIGroup，使用 CanvasLayer 容器）
    /// - UI 窗体生命周期（OnInit/OnOpen/OnClose/OnRecycle）
    /// - 暂停/覆盖/深度排序（Refresh 算法从 UGF 移植）
    /// - 对象池复用（UIFormInstanceObject + IObjectPool）
    /// - 事件（OpenUIFormSuccess/CloseUIFormComplete/OpenUIFormFailure）
    ///
    /// 对应 Unity 版本中的 UIComponent。
    /// </summary>
    public sealed partial class UIComponent : GGFComponent
    {
        // ================================================================
        //  内部类型
        // ================================================================

        /// <summary>
        /// UI 窗体状态枚举。
        /// </summary>
        private enum UIFormStatus : byte
        {
            Unknown = 0,
            WillInit,
            Inited,
            WillOpen,
            Opened,
            WillClose,
            Closed,
            WillRecycle,
            Recycled
        }

        /// <summary>
        /// UI 窗体信息。
        /// 追踪单个 UI 窗体的状态和链表位置。
        /// 使用 ReferencePool 管理以避免 GC。
        /// </summary>
        private sealed class UIFormInfo : IReference
        {
            private IUIForm m_UIForm;
            private UIFormStatus m_Status;
            private bool m_Paused;
            private bool m_Covered;

            public UIFormInfo()
            {
                m_UIForm = null;
                m_Status = UIFormStatus.Unknown;
                m_Paused = false;
                m_Covered = false;
            }

            public IUIForm UIForm => m_UIForm;
            public UIFormStatus Status
            {
                get => m_Status;
                set => m_Status = value;
            }
            public bool Paused
            {
                get => m_Paused;
                set => m_Paused = value;
            }
            public bool Covered
            {
                get => m_Covered;
                set => m_Covered = value;
            }

            public static UIFormInfo Create(IUIForm uiForm)
            {
                UIFormInfo uiFormInfo = ReferencePool.Acquire<UIFormInfo>();
                uiFormInfo.m_UIForm = uiForm;
                uiFormInfo.m_Status = UIFormStatus.WillInit;
                uiFormInfo.m_Paused = true;
                uiFormInfo.m_Covered = true;
                return uiFormInfo;
            }

            public void Clear()
            {
                m_UIForm = null;
                m_Status = UIFormStatus.Unknown;
                m_Paused = false;
                m_Covered = false;
            }
        }

        /// <summary>
        /// UI 组（内部类）。
        ///
        /// 实现 IUIGroup 接口，与 UGF UIManager.UIGroup 方法级别对齐。
        /// 管理同一组内的 UI 窗体，使用 GameFrameworkLinkedList 实现
        /// 栈式管理（链表头部 = 最顶层 = CurrentUIForm）。
        ///
        /// 每个组维护一个对象池（IObjectPool&lt;UIFormInstanceObject&gt;），
        /// 用于缓存和复用 UI 窗体实例。
        /// </summary>
        private sealed class UIGroup : IUIGroup
        {
            private readonly string m_Name;
            private int m_Depth;
            private bool m_Pause;
            private readonly DefaultUIGroupHelper m_Helper;
            private readonly IObjectPool<UIFormInstanceObject> m_InstancePool;
            private readonly GameFrameworkLinkedList<UIFormInfo> m_UIFormInfos;
            private readonly Dictionary<int, LinkedListNode<UIFormInfo>> m_UIFormInfoNodes;
            private LinkedListNode<UIFormInfo> m_CachedNode;

            public UIGroup(string name, int depth, float instanceAutoReleaseInterval,
                int instanceCapacity, float instanceExpireTime, int instancePriority,
                DefaultUIGroupHelper helper, IObjectPoolManager objectPoolManager)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new GameFrameworkException("UI group name is invalid.");
                }

                if (helper == null)
                {
                    throw new GameFrameworkException("UI group helper is invalid.");
                }

                m_Name = name;
                m_Depth = depth;
                m_Pause = false;
                m_Helper = helper;
                m_Helper.SetDepth(depth);
                m_InstancePool = objectPoolManager.CreateSingleSpawnObjectPool<UIFormInstanceObject>(
                    Utility.Text.Format("UI Instance Pool ({0})", name),
                    instanceCapacity, instanceExpireTime, instancePriority);
                m_InstancePool.AutoReleaseInterval = instanceAutoReleaseInterval;
                m_UIFormInfos = new GameFrameworkLinkedList<UIFormInfo>();
                m_UIFormInfoNodes = new Dictionary<int, LinkedListNode<UIFormInfo>>();
                m_CachedNode = null;
            }

            // ---- IUIGroup 接口实现 ----

            public string Name => m_Name;

            public int Depth
            {
                get => m_Depth;
                set
                {
                    m_Depth = value;
                    m_Helper.SetDepth(value);
                }
            }

            public bool Pause
            {
                get => m_Pause;
                set => m_Pause = value;
            }

            public int UIFormCount => m_UIFormInfos.Count;

            public IUIGroupHelper Helper => m_Helper;

            public IUIForm CurrentUIForm
            {
                get
                {
                    if (m_UIFormInfos.First == null) return null;
                    return m_UIFormInfos.First.Value.UIForm;
                }
            }

            /// <summary>
            /// 界面组中是否存在界面。
            /// </summary>
            public bool HasUIForm(int serialId)
            {
                return m_UIFormInfoNodes.ContainsKey(serialId);
            }

            /// <summary>
            /// 界面组中是否存在界面。
            /// </summary>
            public bool HasUIForm(string uiFormAssetName)
            {
                foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
                {
                    if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName)
                    {
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            public IUIForm GetUIForm(int serialId)
            {
                UIFormInfo info = GetUIFormInfo(serialId);
                return info?.UIForm;
            }

            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            public IUIForm GetUIForm(string uiFormAssetName)
            {
                foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
                {
                    if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName)
                    {
                        return uiFormInfo.UIForm;
                    }
                }
                return null;
            }

            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            public IUIForm[] GetUIForms(string uiFormAssetName)
            {
                List<IUIForm> results = new List<IUIForm>();
                GetUIForms(uiFormAssetName, results);
                return results.ToArray();
            }

            /// <summary>
            /// 从界面组中获取界面。
            /// </summary>
            public void GetUIForms(string uiFormAssetName, List<IUIForm> results)
            {
                foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
                {
                    if (uiFormInfo.UIForm.UIFormAssetName == uiFormAssetName)
                    {
                        results.Add(uiFormInfo.UIForm);
                    }
                }
            }

            /// <summary>
            /// 从界面组中获取所有界面。
            /// </summary>
            public IUIForm[] GetAllUIForms()
            {
                List<IUIForm> results = new List<IUIForm>();
                GetAllUIForms(results);
                return results.ToArray();
            }

            /// <summary>
            /// 从界面组中获取所有界面。
            /// </summary>
            public void GetAllUIForms(List<IUIForm> results)
            {
                foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
                {
                    results.Add(uiFormInfo.UIForm);
                }
            }

            // ---- 内部方法 ----

            /// <summary>
            /// 添加 UI 窗体到链表头部（使其成为最顶层）。
            /// </summary>
            public void AddUIForm(UIFormInfo uiFormInfo)
            {
                LinkedListNode<UIFormInfo> node = m_UIFormInfos.AddFirst(uiFormInfo);
                m_UIFormInfoNodes[uiFormInfo.UIForm.SerialId] = node;
            }

            /// <summary>
            /// 从链表中移除 UI 窗体。
            /// </summary>
            public void RemoveUIForm(UIFormInfo uiFormInfo)
            {
                // 如果当前正在迭代且命中该节点，跳过
                if (m_CachedNode != null && m_CachedNode.Value == uiFormInfo)
                {
                    m_CachedNode = m_CachedNode.Next;
                }

                // 在移除前，先触发 Cover 和 Pause（如果尚未触发）
                if (!uiFormInfo.Covered)
                {
                    uiFormInfo.Covered = true;
                    uiFormInfo.UIForm.OnCover();
                }

                if (!uiFormInfo.Paused)
                {
                    uiFormInfo.Paused = true;
                    uiFormInfo.UIForm.OnPause();
                }

                m_UIFormInfos.Remove(uiFormInfo);
                m_UIFormInfoNodes.Remove(uiFormInfo.UIForm.SerialId);
                ReferencePool.Release(uiFormInfo);
            }

            /// <summary>
            /// 刷新界面组。
            ///
            /// 从 UGF UIManager.UIGroup.Refresh() 方法体级别移植。
            /// 重新计算所有 UI 窗体的暂停/覆盖/深度状态。
            /// </summary>
            public void Refresh()
            {
                LinkedListNode<UIFormInfo> current = m_UIFormInfos.First;
                bool pause = m_Pause;
                bool cover = false;
                int depth = UIFormCount;
                while (current != null && current.Value != null)
                {
                    LinkedListNode<UIFormInfo> next = current.Next;
                    current.Value.UIForm.OnDepthChanged(Depth, depth--);
                    if (current.Value == null)
                    {
                        return;
                    }

                    if (pause)
                    {
                        // 组暂停时：所有窗体都 Covered + Paused
                        if (!current.Value.Covered)
                        {
                            current.Value.Covered = true;
                            current.Value.UIForm.OnCover();
                            if (current.Value == null)
                            {
                                return;
                            }
                        }

                        if (!current.Value.Paused)
                        {
                            current.Value.Paused = true;
                            current.Value.UIForm.OnPause();
                            if (current.Value == null)
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        // 正常模式
                        if (current.Value.Paused)
                        {
                            // 恢复暂停
                            current.Value.Paused = false;
                            current.Value.UIForm.OnResume();
                            if (current.Value == null)
                            {
                                return;
                            }
                        }

                        // 检查是否需要暂停下层窗体
                        if (current.Value.UIForm.PauseCoveredUIForm)
                        {
                            pause = true;
                        }

                        if (cover)
                        {
                            // 被上层窗体覆盖
                            if (!current.Value.Covered)
                            {
                                current.Value.Covered = true;
                                current.Value.UIForm.OnCover();
                                if (current.Value == null)
                                {
                                    return;
                                }
                            }
                        }
                        else
                        {
                            // 第一个窗体：取消覆盖
                            if (current.Value.Covered)
                            {
                                current.Value.Covered = false;
                                current.Value.UIForm.OnReveal();
                                if (current.Value == null)
                                {
                                    return;
                                }
                            }

                            cover = true;
                        }
                    }

                    current = next;
                }
            }

            /// <summary>
            /// 更新 UI 窗体（仅更新未暂停的窗体）。
            /// 遇到第一个暂停的窗体时停止迭代。
            /// </summary>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                LinkedListNode<UIFormInfo> current = m_UIFormInfos.First;
                while (current != null)
                {
                    m_CachedNode = current;
                    if (current.Value.Paused)
                    {
                        break;
                    }

                    current.Value.UIForm.OnUpdate(elapseSeconds, realElapseSeconds);
                    current = current.Next;
                }

                m_CachedNode = null;
            }

            /// <summary>
            /// 获取指定序列号的 UIFormInfo（内部方法）。
            /// </summary>
            public UIFormInfo GetUIFormInfo(int serialId)
            {
                if (m_UIFormInfoNodes.TryGetValue(serialId, out LinkedListNode<UIFormInfo> node))
                {
                    return node.Value;
                }

                return null;
            }

            /// <summary>
            /// 注册 UI 窗体实例对象到对象池。
            /// </summary>
            public void RegisterUIFormInstanceObject(UIFormInstanceObject obj, bool spawned)
            {
                m_InstancePool.Register(obj, spawned);
            }

            /// <summary>
            /// 从对象池中获取指定资源名称的 UI 窗体实例。
            /// 如果池中没有可用实例，返回 null。
            /// </summary>
            public UIFormInstanceObject SpawnUIFormInstanceObject(string uiFormAssetName)
            {
                return m_InstancePool.Spawn(uiFormAssetName);
            }

            /// <summary>
            /// 将 UI 窗体归还到对象池。
            /// UIForm 节点不销毁，只是从活跃列表移除，等待复用或池自动释放。
            /// </summary>
            public void UnspawnUIForm(IUIForm uiForm)
            {
                m_InstancePool.Unspawn(uiForm);
            }

            /// <summary>
            /// 将 UI 窗体移动到链表头部（使其成为 CurrentUIForm）。
            /// </summary>
            public void RefocusUIForm(IUIForm uiForm)
            {
                if (uiForm == null)
                {
                    throw new GameFrameworkException("UI form is invalid.");
                }

                // 从链表中找到并移除
                if (m_CachedNode != null && m_CachedNode.Value.UIForm == uiForm)
                {
                    m_CachedNode = m_CachedNode.Next;
                }

                UIFormInfo targetInfo = null;
                LinkedListNode<UIFormInfo> current = m_UIFormInfos.First;
                while (current != null)
                {
                    if (current.Value.UIForm == uiForm)
                    {
                        targetInfo = current.Value;
                        m_UIFormInfos.Remove(current);
                        break;
                    }

                    current = current.Next;
                }

                if (targetInfo == null)
                {
                    return;
                }

                // 添加到头部
                m_UIFormInfos.AddFirst(targetInfo);
            }

            /// <summary>
            /// 获取所有 UI 窗体信息（内部方法）。
            /// </summary>
            public void GetAllUIFormInfos(List<UIFormInfo> results)
            {
                foreach (UIFormInfo uiFormInfo in m_UIFormInfos)
                {
                    results.Add(uiFormInfo);
                }
            }
        }

        // ================================================================
        //  配置类
        // ================================================================

        /// <summary>
        /// UI 组配置。
        /// 用于 [Export] 暴露到 Godot Inspector，让用户配置 UI 分组。
        /// </summary>
        public class UIGroupConfig
        {
            /// <summary>UI 组名称。</summary>
            [Export]
            public string Name { get; set; }

            /// <summary>UI 组深度（CanvasLayer.Layer）。</summary>
            [Export]
            public int Depth { get; set; }
        }

        // ================================================================
        //  成员变量
        // ================================================================

        /// <summary>默认优先级。</summary>
        private const int DefaultPriority = 0;

        /// <summary>界面辅助器。</summary>
        private IUIFormHelper m_UIFormHelper;

        /// <summary>资源组件引用。</summary>
        private ResourceComponent m_ResourceComponent;

        /// <summary>事件组件引用。</summary>
        private EventComponent m_EventComponent;

        /// <summary>对象池管理器。</summary>
        private IObjectPoolManager m_ObjectPoolManager;

        /// <summary>UI 根容器（CanvasLayer）。</summary>
        private CanvasLayer m_UIRoot;

        /// <summary>UI 组字典。</summary>
        private readonly Dictionary<string, UIGroup> m_UIGroups = new();

        /// <summary>所有已加载的 UI 窗体信息（serialId → UIFormInfo）。</summary>
        private readonly Dictionary<int, UIGroup> m_UIFormGroups = new();

        /// <summary>正在加载的 UI 窗体（serialId → OpenUIFormInfo）。</summary>
        private readonly Dictionary<int, OpenUIFormInfo> m_UIFormsBeingLoaded = new();

        /// <summary>加载后需要释放的 UI 窗体序列号集合。</summary>
        private readonly HashSet<int> m_UIFormsToReleaseOnLoad = new();

        /// <summary>回收队列。存储 (UIForm, UIGroup) 元组，下一帧 _Process 中统一回收。</summary>
        private readonly Queue<(IUIForm, UIGroup)> m_RecycleQueue = new();

        /// <summary>序列号生成器。</summary>
        private int m_Serial;

        /// <summary>是否正在关闭。</summary>
        private bool m_IsShutdown;

        /// <summary>内部的 UI 窗体查询结果缓存。</summary>
        private readonly List<IUIForm> m_InternalUIFormResults = new();

        // ================================================================
        //  [Export] 配置属性
        // ================================================================

        /// <summary>界面实例对象池自动释放间隔（秒）。</summary>
        [Export] public float InstanceAutoReleaseInterval { get; set; } = 60f;

        /// <summary>界面实例对象池容量。</summary>
        [Export] public int InstanceCapacity { get; set; } = 16;

        /// <summary>界面实例对象池过期时间（秒）。</summary>
        [Export] public float InstanceExpireTime { get; set; } = 60f;

        /// <summary>界面实例对象池优先级。</summary>
        [Export] public int InstancePriority { get; set; } = 0;

        /// <summary>UI 分组名称数组（与分组深度一一对应，在 Inspector 中配置）。</summary>
        [Export] public string[] UIGroupNames { get; set; } = null;

        /// <summary>UI 分组深度数组（与分组名称一一对应，在 Inspector 中配置）。</summary>
        [Export] public int[] UIGroupDepths { get; set; } = null;

        // ================================================================
        //  公开属性
        // ================================================================

        /// <summary>获取界面组数量。</summary>
        public int UIGroupCount => m_UIGroups.Count;

        // ================================================================
        //  生命周期
        // ================================================================

        /// <summary>
        /// 节点初始化。
        /// 获取组件引用，创建辅助器，创建 UI 根容器和默认分组。
        /// </summary>
        public override void _Ready()
        {
            base._Ready();

            // 获取核心模块
            m_ObjectPoolManager = GameFrameworkEntry.GetModule<IObjectPoolManager>();
            if (m_ObjectPoolManager == null)
            {
                Log.Fatal("Object pool manager is invalid.");
                return;
            }

            // 创建界面辅助器
            m_UIFormHelper = new DefaultUIFormHelper();

            // 获取资源组件
            m_ResourceComponent = GF.Resource;
            if (m_ResourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
                return;
            }

            // 获取事件组件
            m_EventComponent = GF.Event;
            if (m_EventComponent == null)
            {
                Log.Fatal("Event component is invalid.");
                return;
            }

            // 创建 UI 根容器（CanvasLayer）
            m_UIRoot = new CanvasLayer();
            m_UIRoot.Name = "UI Root";
            // UI Root 使用较高的 Layer 值确保渲染在普通场景之上
            m_UIRoot.Layer = 100;
            AddChild(m_UIRoot);

            // 创建默认 UI 分组
            if (UIGroupNames == null || UIGroupNames.Length == 0)
            {
                // 使用默认的 4 组配置
                AddUIGroup("Background", 0);
                AddUIGroup("Normal", 1);
                AddUIGroup("Popup", 2);
                AddUIGroup("Tips", 3);
            }
            else
            {
                // 使用用户自定义的分组配置
                for (int i = 0; i < UIGroupNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(UIGroupNames[i]))
                    {
                        int depth = (UIGroupDepths != null && i < UIGroupDepths.Length) ? UIGroupDepths[i] : 0;
                        AddUIGroup(UIGroupNames[i], depth);
                    }
                }
            }
        }

        /// <summary>
        /// 每帧更新。
        /// 处理回收队列，更新所有 UI 窗体。
        /// </summary>
        /// <param name="delta">帧间隔时间（秒）。</param>
        public override void _Process(double delta)
        {
            base._Process(delta);

            if (m_IsShutdown)
            {
                return;
            }

            // 处理回收队列
            ProcessRecycleQueue();

            // 更新所有 UI 窗体
            float elapseSeconds = (float)delta;
            float realElapseSeconds = (float)Engine.TimeScale > 0f
                ? elapseSeconds / (float)Engine.TimeScale
                : 0f;

            foreach (var group in m_UIGroups.Values)
            {
                group.Update(elapseSeconds, realElapseSeconds);
            }
        }

        /// <summary>
        /// 节点即将从场景树移除时调用。
        /// 关闭所有已加载的 UI 窗体。
        /// </summary>
        public override void _ExitTree()
        {
            if (!m_IsShutdown)
            {
                m_IsShutdown = true;
                CloseAllLoadedUIForms();
            }

            base._ExitTree();
        }

        // ================================================================
        //  UI 组管理
        // ================================================================

        /// <summary>
        /// 是否存在界面组。
        /// </summary>
        public bool HasUIGroup(string uiGroupName)
        {
            return m_UIGroups.ContainsKey(uiGroupName);
        }

        /// <summary>
        /// 获取界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>要获取的界面组。</returns>
        public IUIGroup GetUIGroup(string uiGroupName)
        {
            if (m_UIGroups.TryGetValue(uiGroupName, out UIGroup group))
            {
                return group;
            }

            return null;
        }

        /// <summary>
        /// 获取所有界面组。
        /// </summary>
        /// <returns>所有界面组。</returns>
        public IUIGroup[] GetAllUIGroups()
        {
            IUIGroup[] results = new IUIGroup[m_UIGroups.Count];
            int index = 0;
            foreach (var group in m_UIGroups.Values)
            {
                results[index++] = group;
            }

            return results;
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>是否增加成功。</returns>
        public bool AddUIGroup(string uiGroupName)
        {
            return AddUIGroup(uiGroupName, 0);
        }

        /// <summary>
        /// 增加界面组。
        /// </summary>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="depth">界面组深度。</param>
        /// <returns>是否增加成功。</returns>
        public bool AddUIGroup(string uiGroupName, int depth)
        {
            if (m_UIGroups.ContainsKey(uiGroupName))
            {
                Log.Warning("UI group '{0}' is already exist.", uiGroupName);
                return false;
            }

            // 创建 CanvasLayer 作为 UI 组容器
            DefaultUIGroupHelper helper = new DefaultUIGroupHelper();
            helper.Name = Utility.Text.Format("UI Group - {0}", uiGroupName);
            m_UIRoot.AddChild(helper);

            UIGroup group = new UIGroup(uiGroupName, depth,
                InstanceAutoReleaseInterval, InstanceCapacity, InstanceExpireTime,
                InstancePriority, helper, m_ObjectPoolManager);
            m_UIGroups[uiGroupName] = group;

            return true;
        }

        // ================================================================
        //  UI 窗体查询
        // ================================================================

        /// <summary>
        /// 是否存在指定序列号的界面。
        /// </summary>
        public bool HasUIForm(int serialId)
        {
            return m_UIFormGroups.ContainsKey(serialId);
        }

        /// <summary>
        /// 是否存在指定资源名称的界面。
        /// </summary>
        public bool HasUIForm(string uiFormAssetName)
        {
            m_InternalUIFormResults.Clear();
            foreach (var group in m_UIGroups.Values)
            {
                group.GetUIForms(uiFormAssetName, m_InternalUIFormResults);
                if (m_InternalUIFormResults.Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 是否是合法的界面。
        /// </summary>
        /// <param name="uiForm">界面。</param>
        /// <returns>界面是否合法。</returns>
        public bool IsValidUIForm(UIForm uiForm)
        {
            if (uiForm == null)
            {
                return false;
            }

            return m_UIFormGroups.ContainsKey(uiForm.SerialId);
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(int serialId)
        {
            return m_UIFormsBeingLoaded.ContainsKey(serialId);
        }

        /// <summary>
        /// 是否正在加载界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>是否正在加载界面。</returns>
        public bool IsLoadingUIForm(string uiFormAssetName)
        {
            foreach (var kvp in m_UIFormsBeingLoaded)
            {
                if (kvp.Value.UIGroupName == uiFormAssetName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取指定序列号的界面。
        /// </summary>
        public UIForm GetUIForm(int serialId)
        {
            if (!m_UIFormGroups.TryGetValue(serialId, out UIGroup group))
            {
                return null;
            }

            UIFormInfo info = group.GetUIFormInfo(serialId);
            return info != null ? (UIForm)info.UIForm : null;
        }

        /// <summary>
        /// 获取指定资源名称的界面（返回第一个匹配）。
        /// </summary>
        public UIForm GetUIForm(string uiFormAssetName)
        {
            m_InternalUIFormResults.Clear();
            foreach (var group in m_UIGroups.Values)
            {
                group.GetUIForms(uiFormAssetName, m_InternalUIFormResults);
                if (m_InternalUIFormResults.Count > 0)
                {
                    return (UIForm)m_InternalUIFormResults[0];
                }
            }

            return null;
        }

        /// <summary>
        /// 获取指定资源名称的所有界面。
        /// </summary>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <returns>所有匹配的界面。</returns>
        public UIForm[] GetUIForms(string uiFormAssetName)
        {
            m_InternalUIFormResults.Clear();
            foreach (var group in m_UIGroups.Values)
            {
                group.GetUIForms(uiFormAssetName, m_InternalUIFormResults);
            }

            UIForm[] results = new UIForm[m_InternalUIFormResults.Count];
            for (int i = 0; i < m_InternalUIFormResults.Count; i++)
            {
                results[i] = (UIForm)m_InternalUIFormResults[i];
            }

            return results;
        }

        /// <summary>
        /// 获取所有已加载的界面。
        /// </summary>
        public UIForm[] GetAllLoadedUIForms()
        {
            m_InternalUIFormResults.Clear();
            foreach (var group in m_UIGroups.Values)
            {
                group.GetAllUIForms(m_InternalUIFormResults);
            }

            UIForm[] results = new UIForm[m_InternalUIFormResults.Count];
            for (int i = 0; i < m_InternalUIFormResults.Count; i++)
            {
                results[i] = (UIForm)m_InternalUIFormResults[i];
            }

            return results;
        }

        /// <summary>
        /// 获取所有正在加载界面的序列编号。
        /// </summary>
        /// <returns>所有正在加载界面的序列编号。</returns>
        public int[] GetAllLoadingUIFormSerialIds()
        {
            int[] results = new int[m_UIFormsBeingLoaded.Count];
            m_UIFormsBeingLoaded.Keys.CopyTo(results, 0);
            return results;
        }

        // ================================================================
        //  打开/关闭 UI 窗体
        // ================================================================

        /// <summary>
        /// 打开界面（简化版）。
        /// </summary>
        /// <typeparam name="TLogic">UIFormLogic 类型。</typeparam>
        /// <param name="uiFormAssetName">界面资源名称（res:// 路径）。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>界面序列编号。</returns>
        public int OpenUIForm<TLogic>(string uiFormAssetName, string uiGroupName)
            where TLogic : UIFormLogic
        {
            return OpenUIForm(typeof(TLogic), uiFormAssetName, uiGroupName, DefaultPriority, false, null);
        }

        /// <summary>
        /// 打开界面。
        /// </summary>
        /// <typeparam name="TLogic">UIFormLogic 类型。</typeparam>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <returns>界面序列编号。</returns>
        public int OpenUIForm<TLogic>(string uiFormAssetName, string uiGroupName, bool pauseCoveredUIForm)
            where TLogic : UIFormLogic
        {
            return OpenUIForm(typeof(TLogic), uiFormAssetName, uiGroupName, DefaultPriority, pauseCoveredUIForm, null);
        }

        /// <summary>
        /// 打开界面（带用户数据）。
        /// </summary>
        /// <typeparam name="TLogic">UIFormLogic 类型。</typeparam>
        /// <param name="uiFormAssetName">界面资源名称（res:// 路径）。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面序列编号。</returns>
        public int OpenUIForm<TLogic>(string uiFormAssetName, string uiGroupName,
            bool pauseCoveredUIForm, object userData)
            where TLogic : UIFormLogic
        {
            return OpenUIForm(typeof(TLogic), uiFormAssetName, uiGroupName,
                DefaultPriority, pauseCoveredUIForm, userData);
        }

        /// <summary>
        /// 打开界面（完整参数版）。
        /// </summary>
        /// <param name="uiFormLogicType">UIFormLogic 类型。</param>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <param name="priority">加载优先级（暂未使用，预留）。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>界面序列编号。</returns>
        public int OpenUIForm(Type uiFormLogicType, string uiFormAssetName, string uiGroupName,
            int priority, bool pauseCoveredUIForm, object userData)
        {
            if (m_UIFormHelper == null)
            {
                Log.Error("UI form helper is invalid.");
                return 0;
            }

            if (string.IsNullOrEmpty(uiFormAssetName))
            {
                Log.Error("UI form asset name is invalid.");
                return 0;
            }

            if (string.IsNullOrEmpty(uiGroupName))
            {
                Log.Error("UI group name is invalid.");
                return 0;
            }

            if (!m_UIGroups.TryGetValue(uiGroupName, out UIGroup uiGroup))
            {
                Log.Error("UI group '{0}' is not exist.", uiGroupName);
                return 0;
            }

            int serialId = ++m_Serial;
            float startTime = (float)Time.GetTicksMsec() / 1000f;

            // 创建 OpenUIFormInfo 用于传递 UIFormLogic 类型信息
            OpenUIFormInfo openUIFormInfo = OpenUIFormInfo.Create(uiFormLogicType, userData);
            openUIFormInfo.SerialId = serialId;
            openUIFormInfo.UIGroupName = uiGroupName;
            openUIFormInfo.PauseCoveredUIForm = pauseCoveredUIForm;
            openUIFormInfo.StartTime = startTime;

            // 尝试从对象池获取已有实例
            UIFormInstanceObject instanceObject = uiGroup.SpawnUIFormInstanceObject(uiFormAssetName);
            if (instanceObject != null)
            {
                // 池中复用：isNewInstance = false（不重新调用 OnInit）
                InternalOpenUIForm(openUIFormInfo, uiGroup, instanceObject, false);
                ReferencePool.Release(openUIFormInfo);
                return serialId;
            }

            // 池中无可用实例：异步加载 PackedScene
            m_UIFormsBeingLoaded[serialId] = openUIFormInfo;

            // 通过 ResourceComponent 异步加载
            m_ResourceComponent.LoadAssetAsync(uiFormAssetName, null,
                asset => OnLoadUIFormAssetSuccess(serialId, openUIFormInfo,
                    uiFormAssetName, uiGroupName, pauseCoveredUIForm, userData, asset, uiGroup));

            return serialId;
        }

        // ================================================================
        //  异步打开界面（Phase 8: async/await 支持）
        // ================================================================

        /// <summary>
        /// 异步打开界面。
        ///
        /// 与 OpenUIForm 功能相同，但返回 Task&lt;UIForm&gt;，
        /// 支持 async/await 语法。当对象池命中时立即完成，
        /// 池未命中时等待异步资源加载完成后返回。
        ///
        /// 使用方式：
        /// <code>
        /// UIForm uiForm = await GF.UI.OpenUIFormAsync&lt;MainMenuForm&gt;(
        ///     "res://UI/MainMenu.tscn", "Normal");
        /// </code>
        /// </summary>
        /// <typeparam name="TLogic">UIFormLogic 子类类型。</typeparam>
        /// <param name="uiFormAssetName">界面资源路径。</param>
        /// <param name="uiGroupName">界面组名称。</param>
        /// <returns>打开完成的 UIForm。</returns>
        public Task<UIForm> OpenUIFormAsync<TLogic>(string uiFormAssetName, string uiGroupName)
            where TLogic : UIFormLogic
        {
            return OpenUIFormAsync(typeof(TLogic), uiFormAssetName, uiGroupName, DefaultPriority, false, null);
        }

        /// <summary>
        /// 异步打开界面（带暂停覆盖控制）。
        /// </summary>
        public Task<UIForm> OpenUIFormAsync<TLogic>(string uiFormAssetName, string uiGroupName,
            bool pauseCoveredUIForm) where TLogic : UIFormLogic
        {
            return OpenUIFormAsync(typeof(TLogic), uiFormAssetName, uiGroupName,
                DefaultPriority, pauseCoveredUIForm, null);
        }

        /// <summary>
        /// 异步打开界面（带用户数据）。
        /// </summary>
        public Task<UIForm> OpenUIFormAsync<TLogic>(string uiFormAssetName, string uiGroupName,
            bool pauseCoveredUIForm, object userData) where TLogic : UIFormLogic
        {
            return OpenUIFormAsync(typeof(TLogic), uiFormAssetName, uiGroupName,
                DefaultPriority, pauseCoveredUIForm, userData);
        }

        /// <summary>
        /// 异步打开界面（完整参数版）。
        ///
        /// 内部使用 TaskCompletionSource 包装异步加载回调。
        /// </summary>
        private async Task<UIForm> OpenUIFormAsync(Type uiFormLogicType, string uiFormAssetName,
            string uiGroupName, int priority, bool pauseCoveredUIForm, object userData)
        {
            // 验证参数（与 OpenUIForm 相同）
            if (m_UIFormHelper == null)
            {
                throw new GameFrameworkException("UI form helper is invalid.");
            }

            if (string.IsNullOrEmpty(uiFormAssetName))
            {
                throw new GameFrameworkException("UI form asset name is invalid.");
            }

            if (string.IsNullOrEmpty(uiGroupName))
            {
                throw new GameFrameworkException("UI group name is invalid.");
            }

            if (!m_UIGroups.TryGetValue(uiGroupName, out UIGroup uiGroup))
            {
                throw new GameFrameworkException(Utility.Text.Format(
                    "UI group '{0}' is not exist.", uiGroupName));
            }

            int serialId = ++m_Serial;
            float startTime = (float)Time.GetTicksMsec() / 1000f;

            OpenUIFormInfo openUIFormInfo = OpenUIFormInfo.Create(uiFormLogicType, userData);
            openUIFormInfo.SerialId = serialId;
            openUIFormInfo.UIGroupName = uiGroupName;
            openUIFormInfo.PauseCoveredUIForm = pauseCoveredUIForm;
            openUIFormInfo.StartTime = startTime;

            // 尝试从对象池获取已有实例
            UIFormInstanceObject instanceObject = uiGroup.SpawnUIFormInstanceObject(uiFormAssetName);
            if (instanceObject != null)
            {
                // 池中复用（同步完成）
                InternalOpenUIForm(openUIFormInfo, uiGroup, instanceObject, false);
                ReferencePool.Release(openUIFormInfo);
                return GetUIForm(serialId);
            }

            // 池中无可用实例：异步加载
            TaskCompletionSource<UIForm> tcs = new TaskCompletionSource<UIForm>();
            m_UIFormsBeingLoaded[serialId] = openUIFormInfo;

            m_ResourceComponent.LoadAssetAsync(uiFormAssetName, null,
                asset => OnLoadUIFormAssetSuccessForAsync(serialId, openUIFormInfo,
                    uiFormAssetName, uiGroupName, pauseCoveredUIForm, userData, asset, uiGroup, tcs));

            return await tcs.Task;
        }

        // ================================================================
        //  内部方法：UI 窗体异步加载回调
        // ================================================================

        /// <summary>
        /// UI 窗体异步加载成功回调。
        ///
        /// 从 OpenUIForm 的内联 Lambda 提取为独立方法，
        /// 同时供同步 OpenUIForm 和异步 OpenUIFormAsync 使用。
        ///
        /// 工作流程：
        /// 1. 检查是否在加载过程中被取消
        /// 2. 实例化 PackedScene 获得 Control 节点
        /// 3. 创建 UIForm 包装器并挂载到 UI 组容器
        /// 4. 创建 UIFormLogic 并关联到 UIForm
        /// 5. 注册到对象池
        /// 6. 调用 InternalOpenUIForm 完成打开流程
        /// </summary>
        private void OnLoadUIFormAssetSuccess(int serialId, OpenUIFormInfo openUIFormInfo,
            string uiFormAssetName, string uiGroupName, bool pauseCoveredUIForm,
            object userData, object asset, UIGroup uiGroup)
        {
            // 检查是否已被取消
            if (m_UIFormsToReleaseOnLoad.Remove(serialId))
            {
                // 加载完成前已被取消，释放资源
                m_UIFormsBeingLoaded.Remove(serialId);
                ReferencePool.Release(openUIFormInfo);
                return;
            }

            m_UIFormsBeingLoaded.Remove(serialId);

            if (asset == null)
            {
                Log.Warning("Open UI form failure, asset name '{0}', UI group name '{1}', error message 'Asset is null'.",
                    uiFormAssetName, uiGroupName);

                if (m_EventComponent != null)
                {
                    m_EventComponent.Fire(this, OpenUIFormFailureEventArgs.Create(
                        serialId, uiFormAssetName, uiGroupName, pauseCoveredUIForm,
                        "Asset is null.", userData));
                }

                ReferencePool.Release(openUIFormInfo);
                return;
            }

            try
            {
                // 实例化 PackedScene（获得原始 Control 节点）
                object instance = m_UIFormHelper.InstantiateUIForm(asset);
                if (instance == null)
                {
                    Log.Warning("Open UI form failure, asset name '{0}', UI group name '{1}', error message 'Instantiate failure'.",
                        uiFormAssetName, uiGroupName);

                    if (m_EventComponent != null)
                    {
                        m_EventComponent.Fire(this, OpenUIFormFailureEventArgs.Create(
                            serialId, uiFormAssetName, uiGroupName, pauseCoveredUIForm,
                            "Instantiate failure.", userData));
                    }

                    ReferencePool.Release(openUIFormInfo);
                    return;
                }

                // 创建 UIForm 包装器并挂载到 UI 组容器
                Node instanceNode = instance as Node;
                UIForm uiForm = new UIForm();
                uiForm.Name = "UIForm";
                uiForm.AddChild(instanceNode);
                ((Node)uiGroup.Helper).AddChild(uiForm);

                // 创建 UIFormLogic 并关联到 UIForm
                if (openUIFormInfo.UIFormLogicType != null)
                {
                    try
                    {
                        UIFormLogic logic = (UIFormLogic)Activator.CreateInstance(openUIFormInfo.UIFormLogicType);
                        uiForm.SetUIFormLogic(logic);
                    }
                    catch (Exception exception)
                    {
                        Log.Error("Create UI form logic '{0}' with exception '{1}'.",
                            openUIFormInfo.UIFormLogicType.FullName, exception);
                    }
                }

                // 创建 UIFormInstanceObject（Target = UIForm，实现 IUIForm）
                UIFormInstanceObject uiFormInstanceObject = UIFormInstanceObject.Create(
                    uiFormAssetName, asset, uiForm, m_UIFormHelper);
                uiGroup.RegisterUIFormInstanceObject(uiFormInstanceObject, true);

                // 打开 UI 窗体（isNewInstance = true，因为是新创建的）
                InternalOpenUIForm(openUIFormInfo, uiGroup, uiFormInstanceObject, true);
            }
            catch (Exception exception)
            {
                Log.Error("Open UI form '{0}' with exception '{1}'.", uiFormAssetName, exception);

                if (m_EventComponent != null)
                {
                    m_EventComponent.Fire(this, OpenUIFormFailureEventArgs.Create(
                        serialId, uiFormAssetName, uiGroupName, pauseCoveredUIForm,
                        exception.Message, userData));
                }
            }
            finally
            {
                ReferencePool.Release(openUIFormInfo);
            }
        }

        /// <summary>
        /// UI 窗体异步加载成功回调（带 TaskCompletionSource，供 OpenUIFormAsync 使用）。
        ///
        /// 调用 OnLoadUIFormAssetSuccess 完成实际的 UI 创建和打开流程，
        /// 然后通过 TaskCompletionSource 返回结果给 await 调用方。
        /// </summary>
        private void OnLoadUIFormAssetSuccessForAsync(int serialId, OpenUIFormInfo openUIFormInfo,
            string uiFormAssetName, string uiGroupName, bool pauseCoveredUIForm,
            object userData, object asset, UIGroup uiGroup, TaskCompletionSource<UIForm> tcs)
        {
            OnLoadUIFormAssetSuccess(serialId, openUIFormInfo,
                uiFormAssetName, uiGroupName, pauseCoveredUIForm, userData, asset, uiGroup);

            // 加载成功后 UI 窗体已注册，通过序列号获取并返回
            UIForm uiForm = GetUIForm(serialId);
            if (uiForm != null)
            {
                tcs.TrySetResult(uiForm);
            }
            else
            {
                tcs.TrySetException(new InvalidOperationException(Utility.Text.Format(
                    "UI form '{0}' not found after successful load.", serialId)));
            }
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        public void CloseUIForm(int serialId)
        {
            CloseUIForm(serialId, null);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="serialId">要关闭界面的序列编号。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(int serialId, object userData)
        {
            if (m_IsShutdown)
            {
                return;
            }

            // 如果正在加载中，标记为加载后释放
            if (m_UIFormsBeingLoaded.ContainsKey(serialId))
            {
                m_UIFormsToReleaseOnLoad.Add(serialId);
                m_UIFormsBeingLoaded.Remove(serialId);
                return;
            }

            // 查找 UI 窗体所在的组
            if (!m_UIFormGroups.TryGetValue(serialId, out UIGroup uiGroup))
            {
                Log.Warning("Close UI form '{0}' failure, not found.", serialId);
                return;
            }

            UIFormInfo uiFormInfo = uiGroup.GetUIFormInfo(serialId);
            if (uiFormInfo == null)
            {
                Log.Warning("Close UI form '{0}' failure, UI form info is invalid.", serialId);
                return;
            }

            IUIForm uiForm = uiFormInfo.UIForm;
            bool isShutdown = m_IsShutdown;
            string uiFormAssetName = uiForm.UIFormAssetName;

            // 从组中移除（会触发 OnCover + OnPause）
            uiGroup.RemoveUIForm(uiFormInfo);

            // 从全局追踪中移除
            m_UIFormGroups.Remove(serialId);

            // 调用 OnClose
            uiForm.OnClose(isShutdown, userData);

            // 刷新组的暂停/覆盖状态
            uiGroup.Refresh();

            // 触发关闭完成事件
            if (m_EventComponent != null)
            {
                m_EventComponent.Fire(this, CloseUIFormCompleteEventArgs.Create(
                    serialId, uiFormAssetName, uiGroup, userData));
            }

            // 加入回收队列（携带 UIGroup 引用，供下一帧 Unspawn 使用）
            m_RecycleQueue.Enqueue((uiForm, uiGroup));
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        public void CloseUIForm(UIForm uiForm)
        {
            if (uiForm == null)
            {
                Log.Warning("UI form is invalid.");
                return;
            }

            CloseUIForm(uiForm.SerialId);
        }

        /// <summary>
        /// 关闭界面。
        /// </summary>
        /// <param name="uiForm">要关闭的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseUIForm(UIForm uiForm, object userData)
        {
            if (uiForm == null)
            {
                Log.Warning("UI form is invalid.");
                return;
            }

            CloseUIForm(uiForm.SerialId, userData);
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        public void CloseAllLoadedUIForms()
        {
            CloseAllLoadedUIForms(null);
        }

        /// <summary>
        /// 关闭所有已加载的界面。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void CloseAllLoadedUIForms(object userData)
        {
            // 收集所有序列号（避免迭代时修改集合）
            List<int> serialIds = new List<int>(m_UIFormGroups.Keys);
            foreach (int serialId in serialIds)
            {
                CloseUIForm(serialId, userData);
            }
        }

        /// <summary>
        /// 关闭所有正在加载的界面。
        /// </summary>
        public void CloseAllLoadingUIForms()
        {
            foreach (int serialId in m_UIFormsToReleaseOnLoad)
            {
                m_UIFormsBeingLoaded.Remove(serialId);
            }
            m_UIFormsToReleaseOnLoad.Clear();
        }

        /// <summary>
        /// 重新激活界面（将其移动到组的最顶层）。
        /// </summary>
        /// <param name="serialId">要激活界面的序列编号。</param>
        public void RefocusUIForm(int serialId)
        {
            if (!m_UIFormGroups.TryGetValue(serialId, out UIGroup uiGroup))
            {
                Log.Warning("Refocus UI form '{0}' failure, not found.", serialId);
                return;
            }

            UIFormInfo uiFormInfo = uiGroup.GetUIFormInfo(serialId);
            if (uiFormInfo == null)
            {
                return;
            }

            // 移动到链表头部
            uiGroup.RefocusUIForm(uiFormInfo.UIForm);

            // 刷新组的暂停/覆盖状态
            uiGroup.Refresh();

            // 调用 OnRefocus
            uiFormInfo.UIForm.OnRefocus(null);
        }

        /// <summary>
        /// 重新激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        public void RefocusUIForm(UIForm uiForm)
        {
            if (uiForm == null)
            {
                Log.Warning("UI form is invalid.");
                return;
            }

            RefocusUIForm(uiForm.SerialId);
        }

        /// <summary>
        /// 重新激活界面。
        /// </summary>
        /// <param name="uiForm">要激活的界面。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void RefocusUIForm(UIForm uiForm, object userData)
        {
            if (uiForm == null)
            {
                Log.Warning("UI form is invalid.");
                return;
            }

            if (!m_UIFormGroups.TryGetValue(uiForm.SerialId, out UIGroup uiGroup))
            {
                Log.Warning("Refocus UI form '{0}' failure, not found.", uiForm.SerialId);
                return;
            }

            UIFormInfo uiFormInfo = uiGroup.GetUIFormInfo(uiForm.SerialId);
            if (uiFormInfo == null)
            {
                return;
            }

            uiGroup.RefocusUIForm(uiFormInfo.UIForm);
            uiGroup.Refresh();
            uiFormInfo.UIForm.OnRefocus(userData);
        }

        // ================================================================
        //  内部方法
        // ================================================================

        /// <summary>
        /// 内部打开 UI 窗体。
        /// 执行完整的打开流程：OnInit → AddToGroup → OnOpen → Refresh → 事件。
        /// </summary>
        /// <param name="openUIFormInfo">打开界面信息。</param>
        /// <param name="uiGroup">界面所属的组。</param>
        /// <param name="instanceObject">界面实例对象。</param>
        /// <param name="isNewInstance">是否是新实例（池复用时为 false）。</param>
        private void InternalOpenUIForm(OpenUIFormInfo openUIFormInfo, UIGroup uiGroup,
            UIFormInstanceObject instanceObject, bool isNewInstance)
        {
            IUIForm uiForm = instanceObject.Target as IUIForm;
            if (uiForm == null)
            {
                Log.Error("UI form instance is invalid.");
                return;
            }

            float duration = (float)Time.GetTicksMsec() / 1000f - openUIFormInfo.StartTime;

            try
            {
                // 生命周期：Init
                uiForm.OnInit(openUIFormInfo.SerialId, openUIFormInfo.UIGroupName,
                    uiGroup, openUIFormInfo.PauseCoveredUIForm, isNewInstance, openUIFormInfo.UserData);

                // 创建 UIFormInfo 并添加到组
                UIFormInfo uiFormInfo = UIFormInfo.Create(uiForm);
                uiGroup.AddUIForm(uiFormInfo);

                // 添加到全局追踪
                m_UIFormGroups[openUIFormInfo.SerialId] = uiGroup;

                // 生命周期：Open
                uiForm.OnOpen(openUIFormInfo.UserData);

                // 刷新组的暂停/覆盖/深度状态
                uiGroup.Refresh();

                // 触发打开成功事件
                if (m_EventComponent != null)
                {
                    m_EventComponent.Fire(this, OpenUIFormSuccessEventArgs.Create(
                        uiForm, duration, openUIFormInfo.UserData));
                }
            }
            catch (Exception exception)
            {
                Log.Warning("Internal open UI form '{0}' with exception '{1}'.",
                    openUIFormInfo.UIGroupName, exception);

                // 触发打开失败事件
                if (m_EventComponent != null)
                {
                    m_EventComponent.Fire(this, OpenUIFormFailureEventArgs.Create(
                        openUIFormInfo.SerialId, openUIFormInfo.UIGroupName ?? "",
                        openUIFormInfo.UIGroupName ?? "", openUIFormInfo.PauseCoveredUIForm,
                        exception.Message, openUIFormInfo.UserData));
                }
            }
        }

        /// <summary>
        /// 处理回收队列。
        /// 在每帧 _Process 中调用，统一回收上一帧关闭的 UI 窗体。
        /// </summary>
        private void ProcessRecycleQueue()
        {
            while (m_RecycleQueue.Count > 0)
            {
                (IUIForm uiForm, UIGroup group) = m_RecycleQueue.Dequeue();

                if (uiForm == null)
                {
                    continue;
                }

                // 回收 UIItem（如果 UIFormLogic 有的话）
                UIForm gdForm = uiForm as UIForm;
                if (gdForm?.Logic != null)
                {
                    gdForm.Logic.InternalRecycleItems();
                }

                // 调用 OnRecycle
                uiForm.OnRecycle();

                // 归还到对象池（不销毁节点）
                group.UnspawnUIForm(uiForm);
            }
        }
    }
}
