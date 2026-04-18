# 贡献指南 | Contributing Guide

感谢你对 GGF (Godot Game Framework) 的关注！欢迎参与贡献。

Thank you for your interest in GGF! Contributions are welcome.

---

## 如何贡献 / How to Contribute

### 报告 Bug / Report Bugs

请使用 [GitHub Issues](../../issues) 提交 Bug 报告，并尽量包含以下信息：

- Godot 版本和操作系统
- 问题描述和复现步骤
- 期望行为与实际行为
- 相关日志或截图

### 提交功能请求 / Feature Requests

请在 [GitHub Issues](../../issues) 中描述你希望添加的功能，包括：

- 功能描述和使用场景
- 期望的 API 设计（如有想法）
- 是否愿意自己实现（PR）

### 提交代码 / Pull Requests

1. **Fork** 本仓库
2. 创建功能分支：`git checkout -b feature/your-feature-name`
3. 提交更改：`git commit -m "Add your feature description"`
4. 推送分支：`git push origin feature/your-feature-name`
5. 提交 [Pull Request](../../pulls)

### 代码规范 / Code Style

- **命名规范**：遵循 UGF 的 C# 命名规范
  - 类名、方法名、属性名：PascalCase
  - 私有字段：`m_` 前缀 + PascalCase（如 `m_EntityManager`）
  - 局部变量、参数：camelCase
- **注释**：公共 API 使用 XML 文档注释，代码关键逻辑使用行内注释说明
- **文件头**：保持与现有文件一致的版权声明头
- **Godot 适配**：
  - Unity `_Awake()` → Godot `_Ready()`
  - Unity `_Update()` → Godot `_Process(double delta)`
  - Unity `MonoBehaviour` → Godot `Node`
- **提交信息**：使用清晰简洁的中文或英文描述

### 项目结构约定 / Project Structure Conventions

- `Framework/GameFramework/` — **核心框架层，不要修改**（与 UGF 保持一致）
- `Framework/GodotGameFramework/` — Godot 适配层，新功能在此添加
- `AAAGame/` — 示例游戏代码，仅用于演示

### 开发流程 / Development Workflow

1. 在 `Framework/GodotGameFramework/` 中添加新的适配代码
2. 在 `AAAGame/` 中编写测试用例验证功能
3. 确保所有现有功能不受影响
4. 更新相关文档

---

## 许可证 / License

提交贡献即表示你同意你的代码将在 [MIT License](LICENSE) 下发布。
