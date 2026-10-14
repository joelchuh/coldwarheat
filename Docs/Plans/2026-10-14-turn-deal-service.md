# 补牌与时代加入实现计划

使用 executing-plans 单实现者执行；最后一轮最强独立整分支审查。用户持续开发与日常Git授权有效。
Goal：提供已审核回合参数驱动的纯补牌计划，完整覆盖普通补牌与时代转换，不改动本批Demo行为。
Spec：Docs/Architecture/turn-deal-service.md；既有Docs/Wiki/turn-flow.md、card-zones.md与R2015 §4.1/4.3–4.5B。
Global constraints：纯C#、不复制手牌目标/时代数字、输入不可变、只推进Fork随机、结果牌区/RNG成对提交、私有结果不进入客户端。

## T1 纯规则与测试
文件：新增Core/Cards/TurnDealService.cs及meta；Tests/EditMode/TurnDealTests.cs及meta。
接口：TurnDealService.Plan(CardCatalog,CardZones,GameTurnContext,IRandomProvider) -> TurnDealPlan；属性Before/Cards/Turn/Random、UsDealt/UssrDealt、EraAdded/DiscardReshuffles。没有公开可任意构造的成功计划。
- [x] 创建测试与可编译throw NotImplementedException stub，跑run-unity-editmode.ps1 -RunId turn-deal-red，预期新增14例失败且既有1960通过。
- [x] 实现：校验null、Resolving、通过CardZones重建核对目录完整分区；最高EnteredEra必须与目标相同或下一时代首回合。将目标EraReserve追加Draw后倒序Fisher–Yates，保留Discard。差额用long算Max(0,target-count)且先核对总可用牌。Fork后每次苏/美跳过已满一方，从首端发；只在还需发牌且Draw空时洗弃牌。完成后构建全分区与RandomSnapshot计划。
- [x] 测试固定小牌组：已有Draw不洗/不动China；Draw[a]Discard[b,c,d]，零随机重洗后苏[a,d]美[c,b]；进Mid时Draw[a]+Reserve[m]→[m,a]而Discard[d]保留；Late类似且Removed不混入；时代洗后仍短缺再洗Discard；满手仍加入新年代但第二次不重复；超目标保留；初始已发不再发；非法时代/Resolving/不匹配目录/空输入/总不足/巨大目标/随机溢出拒绝且源不变；实际103牌按共享8目标补5/6、不洗剩余8、重放一致。
- [x] run-unity-editmode.ps1 -RunId turn-deal-green；必须完整既有+新增全部通过。实际用例以XML数量为准。

## T2 知识与交付
- [x] 独立module JSON与catalog/index、Wiki状态、工作记录、源码/XML哈希报告；metadata validator通过。
- [x] 只增加纯服务不重建或替换已鼠标验收Demo；报告明确无新UI/无下一回合按钮。下一批协调器才处理轨道/回合与头条。
- [ ] 审阅diff并提交，唯一整分支只读review；Critical/Important一次TDD修复，Minor记账；ff/main推送远程核对。

## Review Focus
1. 新时代只加一次，不混弃牌；已进入同年代的重试不再次洗牌。对应Mid/FullHandRetry测试。
2. 原Draw必须先发完，随机消费不能提前；超额手牌和中国牌不能占补牌差额。对应Existing/Exhausts/AboveTarget测试。
3. 卡目录不匹配、Resolving、未来/缺段时代、巨额目标必须在循环前拒绝。对应Invalid/Insufficient测试。
4. Fork失败/随机溢出不能改变原牌区或源RNG。对应RandomExhaustion测试。
5. 服务结果含全私有牌区，只有权威协调器可使用；无客户端接线，本批不可宣传已完成连续回合。

Ruling: 沿用既有数字重放顺序并按需重洗 — 既有TURN-03在“继续补牌”时触发；保留未必要的随机消费 — 未来通用抽牌必须保持同一边界。
Ruling: 总牌不足拒绝整计划 — 无确定专门裁定，不虚构牌或默默少发 — 该异常输入需后续规则确认方能继续。
Ruling: 独立纯服务先交付 — 避免把尚缺太空回合末选择/头条的协调器冒称完整规则 — 用户本批仍使用已验证TurnEndPlaytest。
