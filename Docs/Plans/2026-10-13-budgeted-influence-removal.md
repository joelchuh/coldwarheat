# 额度影响力移除实现计划

> 执行：沿用已授权的持续开发，按 executing-plans 在当前分支逐步执行；完成后按 requesting-code-review 独立审阅。

**目标：** 将社会主义政府、苏伊士危机已确认的额度移除设计落实为纯 C# 规划，供后续宿主与界面接入。

**依据：** Docs/Wiki/early-influence-batch2.md，用户确认 EI2-RULING-01。已有两种国家选择继续兼容。数据仍唯一位于 early-influence-batch2.parameters.json。

**架构：** 新增 BudgetedRemovalEffect 与类型化 InfluenceAllocation；InfluenceEventSelection 使用具名 Allocate 工厂区分国家集合/数量分配。Query 返回只读国家容量与有效总量，Plan 验证完整响应后一次生成变更，按 CountryId 排序。客户端不传容量、规则预算或移除方。独立 BudgetedRemovalCatalogLoader 只暴露该数据集的两个已实现模板，不声称东欧动荡已实现。

**边界：** 纯 Core 无 Unity；沿用显式 NoActiveInterference/Unknown/Forbidden 可信策略，铁娘子完整持续效果服务、正式出牌/牌区、保存、回合与东欧动荡不在本批。先不把新定义注入旧 Demo，防止旧界面把额度误作国家数量。地图美术接口不变。

**选择与安全：** required=min(total,sum(min(influence,cap)))，容量零不列候选，required=0 无选择完成；非零响应是互异国家的正整数分配，总和必须精确等于 required。空、重复、负、零、越界、超额、短额、错误身份/版本/选择种类拒绝且不改变状态。计算容量与响应总和使用 long，避免 int 累加溢出；事件版本/序号溢出整条拒绝。返回集合复制并只读。排序只保证规划输出确定性，不提前改变宿主的命令幂等规则。

## 执行步骤

- [x] Core RED：新增 BudgetedRemovalTests；为测试建立最小类型契约，现有 Rules 对未知效果应拒绝。验证西欧双归属、法英以每国上限与短缺、零候选、非法完整分配、重排确定性、溢出和原状态不变。
- [x] Core GREEN：Rules.Query 委托独立 BudgetedRemovalRules，Plan 按 Kind 验证；InfluenceMutation.RemoveExact 只执行已验证量，不决定短缺。复跑完整 EditMode。
- [x] Loader RED/GREEN：从原参数与 schema 装配两个类型定义，核验 ID、来源、范围、禁止关系、版本和非整数/超 int 数据；不载入时代模板。测试实际数据和损坏输入。
- [x] 知识同步：更新模块描述、数据描述、Wiki、目录摘要、计划 JSON、验证报告和工作日志；所有正式入口指向真实符号。
- [x] 独立审阅无问题，Git diff 检查无错误；提交与远程快进同步以本次工具核验记录为准。

## 验证入口

`Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <唯一名称>`

核心手算样例：苏伊士法3/英1/以0→必须法2+英1，法剩1、英0；法英以各2→2+1+1 合法，3+1 非法。社会主义政府奥地利/芬兰各2、其他西欧0→required3，任选2+1。总量为0→ResolvedNoChange，仍产生完成序号。影像与鼠标不属于本批算法验证。

## 审阅重点

零容量的完成与 AwaitingChoice 分支；错误种类不可把国家列表当数量；不可从受益方移除；未知干预不可当允许；大整数与前置版本失败必须原子拒绝。以上均对应真实测试，参数值不散落到 UI。

实际新增依赖：原有参数使用 oneOf，但仓库校验器之前未支持；已通过独立红绿测试补齐互斥分支、同级约束、分支预检，仍不支持任意JSON Schema。Loader的knownEffectIds由可信装配者提供，仅用于引用存在性检查。
