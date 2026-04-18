//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataTable;
using GameFramework.Resource;
using Godot;
using System;
using System.Collections.Generic;

namespace GodotGameFramework
{
    /// <summary>
    /// 数据表组件。
    ///
    /// 这是数据表管理系统的封装组件，透传核心层的 IDataTableManager。
    /// 数据表用于管理游戏中的结构化数据，如角色属性、道具信息、关卡配置等。
    ///
    /// 数据表系统的工作原理：
    /// 1. 每个 DataTable 由多个 DataRow 组成
    /// 2. DataRow 实现 IDataRow 接口，定义表结构
    /// 3. 数据从 CSV 文件加载，每行对应一个 DataRow
    /// 4. 支持通过行号或自定义条件查询数据
    ///
    /// 使用方式：
    /// <code>
    /// DataTableComponent dataTableComp = GF.DataTable;
    ///
    /// // 创建数据表（指定 DataRow 类型）
    /// IDataTable&lt;WeaponData&gt; weaponTable = dataTableComp.CreateDataTable&lt;WeaponData&gt;();
    ///
    /// // 从文件加载数据（通过 ResourceComponent）
    /// dataTableComp.ReadData(weaponTable, "res://Data/DataTable/Weapon.txt");
    ///
    /// // 查询数据
    /// WeaponData weapon = weaponTable.GetDataRow(1);  // 按 ID 查询
    /// WeaponData sword = weaponTable.GetDataRow(w => w.Name == "Sword");  // 按条件查询
    ///
    /// // 获取所有行
    /// WeaponData[] allWeapons = weaponTable.GetAllDataRows();
    /// </code>
    ///
    /// DataRow 实现示例：
    /// <code>
    /// public class WeaponData : IDataRow
    /// {
    ///     public int Id { get; private set; }
    ///     public string Name { get; private set; }
    ///     public int Attack { get; private set; }
    ///
    ///     public void ParseDataRow(string dataRowString, object userData)
    ///     {
    ///         string[] columns = dataRowString.Split('\t');
    ///         int index = 0;
    ///         Id = int.Parse(columns[index++]);
    ///         Name = columns[index++];
    ///         Attack = int.Parse(columns[index++]);
    ///     }
    ///     // ... 其他 ParseDataRow 重载
    /// }
    /// </code>
    ///
    /// 对应 Unity 版本中的 DataTableComponent。
    /// </summary>
    public sealed partial class DataTableComponent : GGFComponent
    {
        /// <summary>
        /// 核心层的数据表管理器实例。
        /// </summary>
        private IDataTableManager m_DataTableManager = null;

        /// <summary>
        /// 获取数据表数量。
        /// </summary>
        public int Count => m_DataTableManager.Count;

        /// <summary>
        /// 获取缓冲二进制流的大小。
        /// </summary>
        public int CachedBytesSize => m_DataTableManager.CachedBytesSize;

        /// <summary>
        /// 节点初始化回调。
        /// 获取核心层 IDataTableManager，创建并设置 Helper。
        /// </summary>
        public override void _Ready()
        {
            base._Ready();

            m_DataTableManager = GameFrameworkEntry.GetModule<IDataTableManager>();
            if (m_DataTableManager == null)
            {
                Log.Fatal("Data table manager is invalid.");
                return;
            }

            // 创建默认数据表辅助器
            // DefaultDataTableHelper 负责解析 CSV/文本格式的数据表
            DefaultDataTableHelper dataTableHelper = new DefaultDataTableHelper();
            m_DataTableManager.SetDataProviderHelper(dataTableHelper);
            m_DataTableManager.SetDataTableHelper(dataTableHelper);

            // 设置资源管理器（核心框架要求）
            // DataTableManager.CreateDataTable 要求 ResourceManager 不为 null。
            // 直接从核心框架获取 ResourceManager 模块（已注册）。
            // 在 Phase 2 中，我们使用 ParseData 而非 ReadData 加载数据，
            // 但核心框架的检查仍需要此设置。
            m_DataTableManager.SetResourceManager(GameFrameworkEntry.GetModule<IResourceManager>());
        }

        /// <summary>
        /// 从文件读取数据表数据并解析。
        ///
        /// 通过 ResourceComponent 加载文件内容，然后调用 ParseData 解析。
        /// </summary>
        /// <param name="dataTable">目标数据表。</param>
        /// <param name="dataAssetName">
        /// 文件路径，使用 Godot 路径格式：
        /// - "res://Data/DataTable/Weapon.txt" — 项目资源目录
        /// </param>
        /// <returns>是否加载并解析成功。</returns>
        public bool ReadData(DataTableBase dataTable, string dataAssetName)
        {
            ResourceComponent resourceComponent = GF.Resource;
            if (resourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
                return false;
            }

            string content = resourceComponent.LoadText(dataAssetName);
            if (content == null)
            {
                Log.Warning("Can not load data table data from '{0}'.", dataAssetName);
                return false;
            }

            return dataTable.ParseData(content);
        }

        /// <summary>
        /// 从文件读取数据表二进制数据并解析。
        ///
        /// 通过 ResourceComponent 以二进制方式加载文件内容，然后调用 ParseData 解析。
        /// </summary>
        /// <param name="dataTable">目标数据表。</param>
        /// <param name="dataAssetName">文件路径。</param>
        /// <returns>是否加载并解析成功。</returns>
        public bool ReadDataBinary(DataTableBase dataTable, string dataAssetName)
        {
            ResourceComponent resourceComponent = GF.Resource;
            if (resourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
                return false;
            }

            byte[] bytes = resourceComponent.LoadBinary(dataAssetName);
            if (bytes == null)
            {
                Log.Warning("Can not load data table binary data from '{0}'.", dataAssetName);
                return false;
            }

            return dataTable.ParseData(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// 确保二进制流缓存分配足够大小的内存并缓存。
        /// </summary>
        /// <param name="ensureSize">要确保的大小。</param>
        public void EnsureCachedBytesSize(int ensureSize)
        {
            m_DataTableManager.EnsureCachedBytesSize(ensureSize);
        }

        /// <summary>
        /// 释放缓存的二进制流。
        /// </summary>
        public void FreeCachedBytes()
        {
            m_DataTableManager.FreeCachedBytes();
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>() where T : IDataRow
        {
            return m_DataTableManager.HasDataTable<T>();
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type dataRowType)
        {
            return m_DataTableManager.HasDataTable(dataRowType);
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable<T>(string name) where T : IDataRow
        {
            return m_DataTableManager.HasDataTable<T>(name);
        }

        /// <summary>
        /// 是否存在数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否存在数据表。</returns>
        public bool HasDataTable(Type dataRowType, string name)
        {
            return m_DataTableManager.HasDataTable(dataRowType, name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>要获取的数据表。</returns>
        public IDataTable<T> GetDataTable<T>() where T : IDataRow
        {
            return m_DataTableManager.GetDataTable<T>();
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>要获取的数据表。</returns>
        public DataTableBase GetDataTable(Type dataRowType)
        {
            return m_DataTableManager.GetDataTable(dataRowType);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表。</returns>
        public IDataTable<T> GetDataTable<T>(string name) where T : IDataRow
        {
            return m_DataTableManager.GetDataTable<T>(name);
        }

        /// <summary>
        /// 获取数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>要获取的数据表。</returns>
        public DataTableBase GetDataTable(Type dataRowType, string name)
        {
            return m_DataTableManager.GetDataTable(dataRowType, name);
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <returns>所有数据表。</returns>
        public DataTableBase[] GetAllDataTables()
        {
            return m_DataTableManager.GetAllDataTables();
        }

        /// <summary>
        /// 获取所有数据表。
        /// </summary>
        /// <param name="results">所有数据表。</param>
        public void GetAllDataTables(List<DataTableBase> results)
        {
            m_DataTableManager.GetAllDataTables(results);
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>创建的数据表。</returns>
        public IDataTable<T> CreateDataTable<T>() where T : class, IDataRow, new()
        {
            return m_DataTableManager.CreateDataTable<T>();
        }

        /// <summary>
        /// 创建数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>创建的数据表。</returns>
        public DataTableBase CreateDataTable(Type dataRowType)
        {
            return m_DataTableManager.CreateDataTable(dataRowType);
        }

        /// <summary>
        /// 创建数据表（指定名称）。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>创建的数据表。</returns>
        public IDataTable<T> CreateDataTable<T>(string name) where T : class, IDataRow, new()
        {
            return m_DataTableManager.CreateDataTable<T>(name);
        }

        /// <summary>
        /// 创建数据表（指定名称）。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>创建的数据表。</returns>
        public DataTableBase CreateDataTable(Type dataRowType, string name)
        {
            return m_DataTableManager.CreateDataTable(dataRowType, name);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <returns>是否销毁成功。</returns>
        public bool DestroyDataTable<T>() where T : IDataRow
        {
            return m_DataTableManager.DestroyDataTable<T>();
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <returns>是否销毁成功。</returns>
        public bool DestroyDataTable(Type dataRowType)
        {
            return m_DataTableManager.DestroyDataTable(dataRowType);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否销毁成功。</returns>
        public bool DestroyDataTable<T>(string name) where T : IDataRow
        {
            return m_DataTableManager.DestroyDataTable<T>(name);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataRowType">数据表行的类型。</param>
        /// <param name="name">数据表名称。</param>
        /// <returns>是否销毁成功。</returns>
        public bool DestroyDataTable(Type dataRowType, string name)
        {
            return m_DataTableManager.DestroyDataTable(dataRowType, name);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <typeparam name="T">数据表行的类型。</typeparam>
        /// <param name="dataTable">要销毁的数据表。</param>
        /// <returns>是否销毁成功。</returns>
        public bool DestroyDataTable<T>(IDataTable<T> dataTable) where T : IDataRow
        {
            return m_DataTableManager.DestroyDataTable(dataTable);
        }

        /// <summary>
        /// 销毁数据表。
        /// </summary>
        /// <param name="dataTable">要销毁的数据表。</param>
        /// <returns>是否销毁成功。</returns>
        public bool DestroyDataTable(DataTableBase dataTable)
        {
            return m_DataTableManager.DestroyDataTable(dataTable);
        }
    }
}
