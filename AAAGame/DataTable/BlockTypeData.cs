//------------------------------------------------------------
// 方块类型数据行。
// 实现 IDataRow 接口，从 BlockTypeData.txt 加载方块类型定义。
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataTable;
using Godot;

/// <summary>
/// 方块类型数据行。
///
/// 对应数据表文件 Data/BlockTypeData.txt。
/// 每行数据以 Tab 分隔，格式为：
/// Id	Name	Score	ColorR	ColorG	ColorB	AutoHide	Lifetime
///
/// 通过此 DataTable 可以在运行时查询方块的属性（分值、颜色、是否自动消失等），
/// 避免在代码中硬编码方块参数。
/// </summary>
public class BlockTypeData : IDataRow
{
    /// <summary>
    /// 方块类型唯一编号（主键）。
    /// </summary>
    public int Id { get; private set; }

    /// <summary>
    /// 方块类型名称。
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// 点击后获得的分数（正数为加分，负数为扣分）。
    /// </summary>
    public int Score { get; private set; }

    /// <summary>
    /// 方块颜色 R 分量（0.0 ~ 1.0）。
    /// </summary>
    public float ColorR { get; private set; }

    /// <summary>
    /// 方块颜色 G 分量（0.0 ~ 1.0）。
    /// </summary>
    public float ColorG { get; private set; }

    /// <summary>
    /// 方块颜色 B 分量（0.0 ~ 1.0）。
    /// </summary>
    public float ColorB { get; private set; }

    /// <summary>
    /// 是否自动消失。
    /// true 表示超时后自动隐藏（红色方块），false 表示只能通过点击消失（绿色方块）。
    /// </summary>
    public bool AutoHide { get; private set; }

    /// <summary>
    /// 自动消失的延迟时间（秒）。
    /// 仅当 AutoHide 为 true 时有效。
    /// </summary>
    public float Lifetime { get; private set; }

    /// <summary>
    /// 无参构造函数（CreateDataTable 泛型约束 new() 要求）。
    /// </summary>
    public BlockTypeData()
    {
        Id = 0;
        Name = null;
        Score = 0;
        ColorR = 0f;
        ColorG = 0f;
        ColorB = 0f;
        AutoHide = false;
        Lifetime = 0f;
    }

    /// <summary>
    /// 解析数据行（文本格式）。
    /// </summary>
    public bool ParseDataRow(string dataRowString, object userData)
    {
        string[] columns = dataRowString.Split('\t');

        if (columns.Length < 8)
        {
            GD.PrintErr($"  [BlockTypeData] 列数不足: {columns.Length}, 期望 8 列。数据: {dataRowString}");
            return false;
        }

        int index = 0;
        Id = int.Parse(columns[index++]);
        Name = columns[index++];
        Score = int.Parse(columns[index++]);
        ColorR = float.Parse(columns[index++]);
        ColorG = float.Parse(columns[index++]);
        ColorB = float.Parse(columns[index++]);
        AutoHide = bool.Parse(columns[index++]);
        Lifetime = float.Parse(columns[index++]);

        return true;
    }

    /// <summary>
    /// 解析数据行（二进制格式）。
    /// </summary>
    public bool ParseDataRow(byte[] dataRowBytes, int startIndex, int length, object userData)
    {
        string dataRowString = Utility.Converter.GetString(dataRowBytes, startIndex, length);
        return ParseDataRow(dataRowString, userData);
    }

    public override string ToString()
    {
        return $"[BlockTypeData] Id={Id}, Name={Name}, Score={Score}, Color=({ColorR},{ColorG},{ColorB}), AutoHide={AutoHide}, Lifetime={Lifetime}";
    }
}
