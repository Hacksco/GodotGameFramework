//------------------------------------------------------------
// 测试用道具数据行。
// 实现 IDataRow 接口，用于验证 DataTableComponent 的功能。
//
// 【独立示例】本文件未在测试游戏流程中实际加载和使用。
// 作为 IDataRow 实现的参考模板，开发者可参照此模式创建自定义数据表。
// 如需使用，请在 LaunchProcedure 中调用：
//   var table = GF.DataTable.CreateDataTable<TestItemData>();
//   GF.DataTable.ReadData(table, "res://Data/TestItemData.txt");
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataTable;
using Godot;
using System.Text;

/// <summary>
/// 测试道具数据行。
///
/// 对应数据表文件 Data/TestItemData.txt。
/// 每行数据以 Tab 分隔，格式为：Id	Name	Price	Desc
///
/// 此类演示了如何实现 IDataRow 接口：
/// 1. 必须有无参构造函数（CreateDataTable 的泛型约束）
/// 2. 必须实现 Id 属性作为主键
/// 3. 必须实现 ParseDataRow 方法解析一行数据
/// </summary>
public class TestItemData : IDataRow
{
    /// <summary>
    /// 道具唯一编号（主键）。
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// 道具名称。
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 道具价格。
    /// </summary>
    public int Price { get; private set; }

    /// <summary>
    /// 道具描述。
    /// </summary>
    public string Desc { get; private set; }

    /// <summary>
    /// 无参构造函数（必须的，用于 CreateDataTable 泛型约束 new()）。
    /// </summary>
    public TestItemData()
    {
        Id = 0;
        Name = null;
        Price = 0;
        Desc = null;
    }

    /// <summary>
    /// 解析数据行（文本格式）。
    ///
    /// 将 Tab 分隔的字符串解析为字段值。
    /// 格式：Id	Name	Price	Desc
    /// </summary>
    /// <param name="dataRowString">一行数据字符串。</param>
    /// <param name="userData">用户自定义数据（本例未使用）。</param>
    /// <returns>是否解析成功。</returns>
    public bool ParseDataRow(string dataRowString, object userData)
    {
        // 按 Tab 分割
        string[] columns = dataRowString.Split('\t');

        // 至少需要4列
        if (columns.Length < 4)
        {
            GD.PrintErr($"  [TestItemData] 列数不足: {columns.Length}, 期望至少4列。数据: {dataRowString}");
            return false;
        }

        int index = 0;
        Id = int.Parse(columns[index++]);
        Name = columns[index++];
        Price = int.Parse(columns[index++]);
        Desc = columns[index++];

        return true;
    }

    /// <summary>
    /// 解析数据行（二进制格式）。
    /// 本测试不使用二进制格式，直接返回 false。
    /// </summary>
    /// <param name="dataRowBytes">二进制数据。</param>
    /// <param name="startIndex">起始位置。</param>
    /// <param name="length">数据长度。</param>
    /// <param name="userData">用户自定义数据。</param>
    /// <returns>是否解析成功。</returns>
    public bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
    {
        // 使用 Utility.Converter 将字节流转为字符串后解析
        string dataRowString = Utility.Converter.GetString(dataRowBytes, startIndex, length);
        return ParseDataRow(dataRowString, userData);
    }

    public override string ToString()
    {
        return $"[TestItemData] Id={Id}, Name={Name}, Price={Price}, Desc={Desc}";
    }
}
