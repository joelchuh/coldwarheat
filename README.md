# ColdWarStruggle

《冷战热斗》电脑端学习项目，GitHub 仓库名为 **coldwarheat**，内部项目名为 **ColdWarStruggle**。

## 当前阶段

当前是**规则数据与契约设计阶段**。仓库已整理 84 个国家及邻接、103 张基础牌的印刷定义、初始布置、回合流程、行动、计分、胜负和分批卡牌事件设计。具体参数绑定与未确认边界见[正式数据索引](Docs/Wiki/map-card-data.md)及各牌的验证报告。

尚未创建 Unity 工程或 C# 游戏源码，尚无可运行的完整对局。JSON 参数 `Bound` 只表示参数已登记；所有 102 个事件处理器仍为 `planned`。有限设计检查、资料核对和实际游戏测试分别记录，未接入的组合与运行验收不视为完成。

**实现方向已认可，首批计划已准备，尚未开始编码。** 先完成纯规则闭环，再接占位地图。用户选择首个原型供单人测试、允许查看双方信息；当前建议一个客户端、两个固定身份会话。详见[测试模式](Docs/Wiki/local-test-mode.md)、[首批计划](Docs/Plans/2026-10-13-core-influence-first-slice.md)及[JSON 摘要](Docs/Plans/2026-10-13-core-influence-first-slice.json)。

## 已确认的工程原则

- Unity 6、URP、C#；本机编辑器为 6000.6.0f1，正式工程与包版本尚未建立。
- Core 是独立于 Unity、网络与美术的纯规则层。玩家通过 Command 操作，表现层读取按身份过滤的视图。
- 先做单人双边测试原型，再完善同机双人、真实联网与 AI。正常玩家与 AI 只读其获准视图；测试观察入口独立且只读。
- 规则以已核对的 Deluxe 基础资料及明确记录的项目解释为准；未知边界保持待处理，不猜默认值。可选 104–110 号牌未启用。
- 卡牌、地图、固定规则使用共享 JSON 定义；动态局面单独保存。界面通过稳定 ID 关联布局和主题，画风仍可调整。

## 如何查找与继续工作

1. 读 [AGENTS.md](AGENTS.md)，核对当前阶段和 Git 状态。
2. 从 [Wiki 索引](Docs/Wiki/index.md) 的短描述选择知识点。
3. 读命中的模块、数据或脚本描述，再按入口定位相关函数及必要上下文；`planned_entrypoints` 不是已有代码。
4. 修改后同步数据、描述、Wiki、验证证据和工作记录，再审阅、提交并核对远程。

结构与边界见[游戏架构](Docs/Wiki/architecture.md)；共享数据与脚本化条件见[模块、数据及脚本工作流](Docs/Wiki/workflow-and-data.md)。逐批历史已集中到[设计进度记录](Docs/Worklogs/2026-10-13-design-progress-history.md)，不把旧批次的累计数当成当前库存。

## Git 与目录

本地项目为 `E:\TwilightStruggle`；远程为 [joelchuh/coldwarheat](https://github.com/joelchuh/coldwarheat)，`main` 跟踪 `origin/main`。提交保存本地历史，推送并核对后才算远程同步；教程见 [Git 工作流](Docs/Wiki/git-workflow.md)。

代码、规则数据、Wiki、工作日志和项目 Skill 保存在同一仓库。Unity 工程建立后应跟踪 Assets 及 `.meta`、Packages、ProjectSettings；缓存、构建输出、个人设置和编辑器安装文件不纳入仓库。
