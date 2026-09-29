# 早期影响力事件：第一批

> description: 查询5张早期影响力牌的类型化参数、4个具名效果模板、公共原语、国家选择与可恢复继续位置。
> 状态：2026-09-29，基础效果设计与参数绑定已完成；5个Bound仅表示引用已绑定，102个处理器仍全部planned，尚无C#实现。
> 入口：[参数描述](../../Data/Descriptors/early-influence-batch1.data.json)、[参数数据](../../Data/Cards/deluxe-2015/early-influence-batch1.parameters.json)、[模块描述](../Descriptors/influence-events.module.json)、[参数Schema](../Contracts/early-influence-parameters.schema.json)、[案例](../Design/early-influence-batch1.cases.json)。

## 本批范围与分工

选择Fidel、Romanian Abdication、Nasser、Truman Doctrine、Decolonization，先覆盖无持续能力的直接影响力效果。Vietnam Revolts、De Gaulle、Marshall Plan等需要持续效果系统，后批处理；Socialist Governments/Suez Crisis等额度移除，以及Independent Reds/COMECON等选择边界也另批展开，不把所有牌强塞进一个模板。

印刷OPS、阵营、时代、移出星号仍只在cards.json保存；参数文件不重复这些字段，也不保存完整卡牌原文。固定国家按CountryId引用，稳定度与地区成员只从world.json读取。每张卡的effect_binding引用一个参数对象；逐卡EffectId不变，两张清空后控制牌共享同一具名handler_key。

模板是代码注册表中的固定算法，不是JSON脚本。JSON只保存类型化参数，不支持任意操作列表、表达式、反射类名、脚本路径或eval。未知模板或参数版本拒绝处理。可以复用原语和选择器，但每张卡仍独立维护规则来源、案例和版本。

## 五张牌的结算契约

| 卡牌 | 受益/选择方 | 基础执行流程 | 选择 |
|---|---|---|---|
| #8 Fidel | USSR | 清空古巴US影响力，再给USSR补足控制所需差额 | 无 |
| #12 Romanian Abdication | USSR | 清空罗马尼亚US影响力，再给USSR补足控制所需差额 | 无；复用上项模板 |
| #15 Nasser | USSR | 埃及USSR增加2，随后移除US当前影响力的一半，移除量向上取整 | 无 |
| #19 Truman Doctrine | US | 在双方均未控制的欧洲国家中选择一个，清空该国USSR影响力 | 一个国家 |
| #30 Decolonization | USSR | 从非洲与东南亚的并集中选4个不同国家，每国增加1 USSR影响力 | 四个不同国家 |

来源：C-DELUXE-EN英文卡图PDF第2页#8、第3页#12/#15、第4页#19、第5页#30；R2015 §2.1.7、§5.2、§7.1、§7.4.3。F2010 PDF第5页#19明确uncontrolled是双方都未控制，不能替换成NotControlledByUSSR。

- 补足控制只增加不足部分，已有更多USSR影响力不降低。清除后US为0，所需下限从国家稳定度读取；不能在处理器里再写“古巴=3/罗马尼亚=3”。
- 纳赛尔是移除量向上取整：US=5时移除3、剩2；不能把剩余量向上取整。US=0时仍增加USSR影响力。
- 杜鲁门目标按事件实际开始时的控制状态判定。合法集合不额外要求该国有USSR影响力：卡文未加此限制；选择零影响力目标时结果为ResolvedNoChange。这是按卡面目标条件与R2015 §5.2推导的采用解释，不冒称FAQ另有专门零影响力裁定。零变化选项标明结果，不用AI替玩家自动选收益最大目标。
- 若杜鲁门没有任何双方均未控制的欧洲国家，无选择地完成ResolvedNoChange；不能制造一个永远无法回答的决策。它无类似“若某前提成立”的事件门槛，不把无数值变化误判成BlockedPrerequisite。
- 去殖民化可全选非洲、全选东南亚或跨两区；区域并集先去重，必须选4个不同国家。受益方即使无当地/相邻影响力也可放；基础卡面不排除US控制国。在无其他干预的正式地图上有25个候选国家，不能无故少选或对同一国投4点。

事件正常完成后由既有CardDispositionRules读取唯一移出标记：本批前4张移出，去殖民化弃牌。无变化但确已发生仍按标记处理；被明确禁止、前提不成立、未触发事件、太空弃置等仍沿用不同Outcome，不能合并。

## 可复用影响力原语

拟定InfluenceMutation只处理已经验证的CountryId/Side/整数，并生成增量计划，不修改公开GameState、不解析卡牌文本、不负责选目标。

| 原语 | 计算/输出 | 不变量 |
|---|---|---|
| AddExact | new = old + amount | 非负整数，保留已有数值 |
| RemoveAll | removed = old；new = 0 | 不使对手其他国家变化 |
| RemoveFractionCeiling | removed = ceil(old/divisor) | 先确定移除量；非负整数算术 |
| EnsureControlByAdding | add = max(0, enemy + stability - own) | 仅增加，按当前工作快照算控制差额 |

纳赛尔除数、增加量和去殖民化每国量从参数读取。Fidel/Romanian按先清除后补足的工作快照执行。RemoveFractionCeiling实现应使用整数商与余数避免浮点误差和old+divisor-1的溢出；加法使用checked或等价工程限额，数值异常报告技术错误，不悄悄截断游戏规则值。

InfluencePlan含来源EffectId/CardId、输入StateVersion、按执行顺序排列的前后值/增量。同一牌的多个原语在隔离工作快照内执行，整个命令成功才提交；事件按同一批次更新控制查询。控制变化不会自动触发地区计分或欧洲控制胜利，后者在规定计分检查点处理。

直接事件不花OPS、不检查普通OPS可达性、不收敌控双倍费用、不计军事行动，也不因给战场国加影响力而降低DEFCON。但它仍须遵守已生效卡牌的明确禁止或修正；“直接事件”不是绕过所有活动效果的许可。

## 选择器与多步骤继续机制

CountrySelector负责通过稳定地图ID构建候选；本批用RegionUnion（非洲/东南亚）、RegionWithControlPredicate（欧洲且双方未控制）。SelectionContract具有DecisionOwner、MinCount/MaxCount、Distinct、LegalCountryIds、ConstraintsVersion及SourceStateVersion。本批去殖民化Min=Max来自target_count；杜鲁门有候选时Min=Max=1。

ResolveDecisionCommand沿用会话认证、CommandId和ExpectedStateVersion，payload只提交DecisionId和目标CountryId集合，不接收客户端提供的变化量、受益方或合法目标。事务串行，不接受动画回调推进；校验拥有者、当前帧/步骤、决策版本、目标存在、去重、数量及重新计算的合法性。

EffectController由事件阵营/授权确定，PhasingPlayer保留原行动责任：
- US打出苏联牌走OPS而触发去殖民化：US仍为PhasingPlayer，USSR是DecisionOwner。
- USSR打出杜鲁门主义触发US事件：US做选择，不能让出牌者选择。
- 头条也按事件受益方执行选择；责任身份与原先头条框架一致。

拟定ContinuationState包含EffectInstanceId、EffectId、ContractVersion、参数版本/摘要、Step、DecisionId、DecisionRevision和父ResolutionFrame返回点。必要时保存已经提交的类型化局部数据；不保存委托、闭包、任意代码或场景对象。

流程：检查EventAttempt → 解析类型化参数 → 构造候选 → 无选择则直接生成计划，否则原子保存AwaitingDecision → 响应校验 → 在当前允许的快照上构建完整计划 → 原子提交结果并标记实例完成 → CardDisposition/返回父帧。决策等待期间不允许其他无关改变状态的命令；恢复后如状态/参数不匹配，拒绝旧响应并报告，不静默重选目标。

本批每张牌最多一次玩家选择，不人为拆成4次去殖民化点击提交；UI可编辑草稿，确认后原子提交四国。复用框架允许未来多步骤事件按Step建立新的决策，但不能把同一决策响应执行两次。计划应用、完成标记、去重结果与领域事件必须在同一事务中持久化。

~~~mermaid
flowchart LR
 A[CardId与已绑定参数] --> B[具名处理器]
 B --> C[目标筛选]
 C --> D{需要选择}
 D -->|需要| E[保存PendingDecision与继续位置]
 E --> F[受益方提交目标]
 F --> G[身份/版本/合法性检查]
 D -->|不需要| H[隔离状态中生成影响力计划]
 G --> H
 H --> I[一次提交增量与完成标记]
 I --> J[统一卡牌去向与返回父帧]
~~~

## 活动效果、隐私与失败边界

本批已核对的是卡牌本体效果。Chernobyl等活动效果与本批牌的交互尚未冻结，列为后续工作；尤其“部分增量被禁止时是否仍执行余下部分”和“合法候选不足4时如何处理”不能套一个万能min公式。

InfluenceEventPolicy拟返回Allowed / Forbidden / RequiresRuling。对未知或未支持的活动效果组合返回RequiresRuling并中止依赖事务，保留开发诊断；不能默认为Allowed，也不能擅自判任一玩家输。将来已核实的逐卡交互策略决定是过滤目标、禁止整个事件还是部分执行，来源及案例一同更新。本批参数Bound不代表所有跨卡交互已经核实，更不代表处理器implemented。

非法输入返回错误且不改影响力、牌区、随机状态、阶段或完成标记；重复成功命令返回原结果。技术错误回滚当前事务，Application记录Faulted。本批原语不调用随机源；未来其他事件的随机性需独立版本化。

已触发事件的卡名和地图变化是公开信息；手牌中尚未打出的卡、牌库和父帧私有数据不得混入DecisionRequested/错误日志。UI只读经PlayerView过滤的字段。更换地图主题、语言或动画不会改变CountryId、可选集合与结算公式。

## 验收与后续批次

[案例文件](../Design/early-influence-batch1.cases.json)区分参数/引用检查、离线效果参考模型及planned运行案例。参考模型验证算式与选择约束，不证明事务、视角隔离和持久化代码存在。当前没有C#服务测试。

[第二批](early-influence-batch2.md)已展开Socialist Governments、Suez Crisis、East European Unrest。后续处理Independent Reds、COMECON，再设计持续效果和跨卡拦截。第一批额外活动效果交互一并排入该阶段，资料不明确时登记待核实。

[开局设计](setup.md) · [普通行动](operations.md) · [返回索引](index.md)

本批38项参数/参考模型检查和87项既有检查于2026-09-29通过；12项运行验收尚未执行。详见[工作记录](../Worklogs/2026-09-28-early-influence-batch1.md)。
