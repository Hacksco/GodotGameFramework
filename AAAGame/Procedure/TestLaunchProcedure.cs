//------------------------------------------------------------
// 启动流程（LaunchProcedure）
// 游戏的入口流程，完成框架初始化、加载配置和数据表、创建实体组
//------------------------------------------------------------

using GameFramework;
using GameFramework.DataNode;
using GameFramework.DataTable;
using GameFramework.Fsm;
using GameFramework.Localization;
using GameFramework.Procedure;
using Godot;
using GodotGameFramework;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

/// <summary>
/// 启动流程。
///
/// 这是游戏运行后的第一个流程，主要功能：
/// 1. 验证框架各组件是否存在（Log 输出）
/// 2. 加载游戏配置文件（GameConfig.txt → ConfigComponent）
/// 3. 加载方块类型数据表（BlockTypeData.txt → DataTableComponent）
/// 4. 创建方块实体组（BlockGroup → EntityComponent）
/// 5. 加载历史最高分（SettingComponent → DataNode）
///
/// 所有初始化完成后立即切换到菜单流程。
/// </summary>
public class TestLaunchProcedure : ProcedureBase
{
    /// <summary>
    /// 状态初始化。
    /// </summary>
    protected internal override void OnInit(ProcedureOwner procedureOwner)
    {
        base.OnInit(procedureOwner);
        GD.Print("[LaunchProcedure] OnInit - 启动流程初始化完成");
    }

    /// <summary>
    /// 进入流程。
    /// 执行所有初始化工作后立即切换到菜单流程。
    /// </summary>
    protected internal override void OnEnter(ProcedureOwner procedureOwner)
    {
        base.OnEnter(procedureOwner);

        GD.Print("\n========================================");
        GD.Print("  Click The Blocks — GGF Demo Game");
        GD.Print("  Phase 1-7 Feature Demonstration");
        GD.Print("========================================\n");

        // 1. 验证组件（Log 展示）
        GD.Print("[LaunchProcedure] 验证框架组件...");
        Log.Info("  Log 系统正常");
        GD.Print($"  EventComponent: {(GF.Event != null ? "OK" : "缺失")}");
        GD.Print($"  FsmComponent: {(GF.Fsm != null ? "OK" : "缺失")}");
        GD.Print($"  ConfigComponent: {(GF.Config != null ? "OK" : "缺失")}");
        GD.Print($"  DataTableComponent: {(GF.DataTable != null ? "OK" : "缺失")}");
        GD.Print($"  SettingComponent: {(GF.Setting != null ? "OK" : "缺失")}");
        GD.Print($"  DataNodeComponent: {(GF.DataNode != null ? "OK" : "缺失")}");
        GD.Print($"  ResourceComponent: {(GF.Resource != null ? "OK" : "缺失")}");
        GD.Print($"  EntityComponent: {(GF.Entity != null ? "OK" : "缺失")}");

        // Phase 5: 验证 UIComponent
        GD.Print($"  UIComponent: {(GF.UI != null ? "OK" : "缺失")}");

        // Phase 6: 验证 SoundComponent
        GD.Print($"  SoundComponent: {(GF.Sound != null ? "OK" : "缺失")}");

        // Phase 7: 验证 LocalizationComponent
        GD.Print($"  LocalizationComponent: {(GF.Localization != null ? "OK" : "缺失")}");

        // 2. 加载游戏配置（Config 展示）
        GD.Print("\n[LaunchProcedure] 加载游戏配置...");
        if (GF.Config != null)
        {
            bool loadResult = GF.Config.ReadData("res://Data/GameConfig.txt");
            GD.Print($"  加载 GameConfig.txt: {(loadResult ? "成功" : "失败")}");
            GD.Print($"  配置项数量: {GF.Config.Count}");
        }

        // 3. 加载方块类型数据表（DataTable 展示）
        GD.Print("\n[LaunchProcedure] 加载数据表...");
        if (GF.DataTable != null)
        {
            IDataTable<BlockTypeData> blockTable = GF.DataTable.CreateDataTable<BlockTypeData>();
            if (blockTable != null)
            {
                bool loadResult = GF.DataTable.ReadData((DataTableBase)blockTable, "res://Data/BlockTypeData.txt");
                GD.Print($"  加载 BlockTypeData.txt: {(loadResult ? "成功" : "失败")}");
                GD.Print($"  方块类型数量: {blockTable.Count}");

                // 打印所有方块类型
                BlockTypeData[] allTypes = blockTable.GetAllDataRows();
                foreach (BlockTypeData type in allTypes)
                {
                    GD.Print($"    {type}");
                }
            }
        }

        // 4. 创建实体组（Entity 展示）
        GD.Print("\n[LaunchProcedure] 创建实体组...");
        if (GF.Entity != null)
        {
            bool addGroup = GF.Entity.AddEntityGroup("BlockGroup", 60f, 16, 60f, 0);
            GD.Print($"  创建 BlockGroup: {(addGroup ? "成功" : "失败")}");
        }

        // 5. 加载高分（Setting + DataNode 展示）
        GD.Print("\n[LaunchProcedure] 加载高分记录...");
        if (GF.Setting != null && GF.DataNode != null)
        {
            int highScore = GF.Setting.GetInt("HighScore", 0);
            GF.DataNode.SetData("Game.HighScore", (VarInt32)highScore);
            GD.Print($"  历史最高分: {highScore}");
        }

        GD.Print("\n[LaunchProcedure] 初始化完成，切换到菜单流程...\n");

        // Phase 5: 验证 UI 分组
        if (GF.UI != null)
        {
            GD.Print($"  UI 分组数量: {GF.UI.UIGroupCount}");
            GD.Print($"  Background 组: {(GF.UI.HasUIGroup("Background") ? "OK" : "缺失")}");
            GD.Print($"  Normal 组: {(GF.UI.HasUIGroup("Normal") ? "OK" : "缺失")}");
            GD.Print($"  Popup 组: {(GF.UI.HasUIGroup("Popup") ? "OK" : "缺失")}");
            GD.Print($"  Tips 组: {(GF.UI.HasUIGroup("Tips") ? "OK" : "缺失")}");
        }

        // Phase 6: 验证 Sound 声音组
        GD.Print("\n[LaunchProcedure] 验证音频系统...");
        if (GF.Sound != null)
        {
            GD.Print($"  声音组数量: {GF.Sound.SoundGroupCount}");
            GD.Print($"  Music 组: {(GF.Sound.HasSoundGroup("Music") ? "OK" : "缺失")}");
            GD.Print($"  SFX 组: {(GF.Sound.HasSoundGroup("SFX") ? "OK" : "缺失")}");
            GD.Print($"  UI 组: {(GF.Sound.HasSoundGroup("UI") ? "OK" : "缺失")}");
        }

        // Phase 7: 验证本地化系统并加载字典
        GD.Print("\n[LaunchProcedure] 验证本地化系统...");
        if (GF.Localization != null)
        {
            GD.Print($"  系统语言: {GF.Localization.SystemLanguage}");
            GD.Print($"  当前语言: {GF.Localization.Language}");

            // 加载系统语言对应的字典（供后续 UI 使用）
            Language systemLang = GF.Localization.SystemLanguage;
            string dictFile = systemLang == Language.ChineseSimplified
                ? "res://Data/Localization/ChineseSimplified.txt"
                : "res://Data/Localization/English.txt";
            bool loaded = GF.Localization.ReadData(dictFile);
            if (loaded)
            {
                GF.Localization.Language = systemLang;
                GD.Print($"  已加载字典: {dictFile} ({GF.Localization.DictionaryCount} 条目)");
            }
        }

        // Phase 3: 验证资源加载（通过 ResourceComponent 直接加载）
        GD.Print("\n[LaunchProcedure] 验证资源系统...");
        if (GF.Resource != null)
        {
            // 测试加载文本资源（配置文件）
            string configText = GF.Resource.LoadText("res://Data/GameConfig.txt");
            GD.Print($"  LoadText(GameConfig.txt): {(configText != null ? "成功" : "失败")}");

            // 测试加载二进制资源
            byte[] configBytes = GF.Resource.LoadBinary("res://Data/GameConfig.txt");
            GD.Print($"  LoadBinary(GameConfig.txt): {(configBytes != null ? $"成功 ({configBytes.Length} bytes)" : "失败")}");

            // 测试检查资源是否存在
            bool hasAsset = GF.Resource.HasAsset("res://Data/GameConfig.txt");
            GD.Print($"  HasAsset(GameConfig.txt): {hasAsset}");

            // 测试加载 PackedScene（实体场景）
            Godot.Resource scene = GF.Resource.LoadAsset<Godot.Resource>("res://AAAGame/Entity/ScoreBlock.tscn");
            GD.Print($"  LoadAsset<ScoreBlock.tscn>: {(scene != null ? "成功" : "失败")}");
        }

        // 触发流程切换事件（Event 展示）
        if (GF.Event != null)
        {
            GF.Event.Fire(this, TestPhaseChangedEventArgs.Create(
                "TestLaunchProcedure", "TestMenuProcedure"));
        }

        // 切换到菜单流程（Procedure 展示）
        ChangeState<TestMenuProcedure>(procedureOwner);
    }

    /// <summary>
    /// 离开流程。
    /// </summary>
    protected internal override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
    {
        base.OnLeave(procedureOwner, isShutdown);
        GD.Print("[LaunchProcedure] OnLeave - 离开启动流程");
    }
}
