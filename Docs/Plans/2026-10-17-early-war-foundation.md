# 早期战争基础 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: 使用executing-plans在当前会话逐项执行，末尾一次fresh-context只读分支审查。

**Goal:** 将三张已设计战争变成不可变、确定性的基础结算服务。
**Architecture:** 正式JSON→严格加载→只读定义→纯计算→独立随机fork→军事/影响力/VP统一计划；不在UI写规则。
**Tech Stack:** Unity 6000.6.0f1、纯C# Core、Newtonsoft Infrastructure、NUnit EditMode。
**Spec:** Docs/Architecture/early-war-foundation.md。

## Global Constraints

- Core无Unity引用；使用既有规则地图、MilitaryOperationsService和VictoryService。
- 固定数值从现有参数加载；不从OPS推断奖励。
- 不接Player/会话，不把本批声明成三牌可玩；policy只可由可信规则提供。
- 保留用户两项未跟踪文件；显式暂存本批文件；常规提交/推送已授权。

## Review Focus

1. 固定苏联牌由美国出牌仍苏联受益：Task1双控制方计算测试。
2. 阿以被阻止不可消费骰/军事：Task2带监视provider测试。
3. 缺国家/错误地图/修改来源不得静默默认：Task1加载负例、Task2完整局面检查。
4. 胜利溢出或军事重复ID不得先耗随机：Task2先验拒绝与输入不变。
5. 地图图像无关、直接超级大国不能递归：Task1自定义拓扑明确断言。

## Task 1 — 参数、目标和算术

创建Core/Rules/WarEventDefinition.cs、WarEventRules.cs；Infrastructure/DataLoading/EarlyWarDefinitionLoader.cs及.meta；Tests/EditMode/EarlyWarDefinitionTests.cs和WarCalculationTests.cs。

产出规格所列Load、QueryTargets、Calculate。使用正式数据和独立手算期望；加载错误测试还需relaxed schema保证运行语义自己守边界。

- [ ] 写定义/加载/纯计算接口和失败测试；原生RED必须看到业务缺失，不接受编译失败为RED。
- [ ] 实现只读定义、参数绑定与计算；原生GREEN。
- [ ] 更新Wiki/描述/工作记录/验证JSON；元数据校验、diff检查，提交。

## Task 2 — 单骰与原子计划

创建Core/Rules/WarEventService.cs及.meta；Tests/EditMode/WarEventServiceTests.cs。消费Task1 Catalog/Calculation。

- [ ] 写完整上下文、阻止、随机独立性、军事/VP、非法/溢出与PCG重放测试，原生RED。
- [ ] 实现WarEventState/WarEventPlan和Plan；原生GREEN。原始状态与random不变，只有After可由未来宿主提交。
- [ ] 文档明确权威policy/幂等/父帧尚未接线；元数据检查、提交。

## 收尾

- [ ] fresh-context只读整分支审查，修正Critical/Important；需要修补则RED→GREEN。
- [ ] 保存真实验证、来源与限制；推送feat/early-war-foundation并核对SHA。
- [ ] 继续可玩接线设计，不停留于基础服务。

运行：`Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <本批唯一ID> -TimeoutSeconds 600`。运行期间不改C#。Meta使用`Tools/Scripts/validate-project-metadata.py`。
