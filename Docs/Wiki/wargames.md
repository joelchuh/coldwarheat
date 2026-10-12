# Wargames：送分与可选立即结束

description：查 #100 的三个独立选项、DEFCON 条件、完整 VP 批次、来源与原请求收据，以及跳过最终计分的终局边界。

状态 2026-10-13：单牌数据 verified-subset；C#/Unity 处理器、鉴权、持久化、事务和共享运行服务均 planned。整合后84 Bound/18 Unresolved，102处理器均planned；[本批报告](../Design/wargames.validation.json)实际记录182新＋3062旧＝3244离线检查。按[数据描述](../../Data/Descriptors/wargames.data.json)→[参数](../../Data/Cards/deluxe-2015/wargames.parameters.json)→[契约](../Contracts/wargames.schema.json)→[案例](../Design/wargames.cases.json)检索。

## 三个选择及来源

英文卡图 PDF p15 #100 是 Neutral、4 Ops、Late、星号；这些印刷事实通过 cards 数据引用，处理器不能以印刷 OPS 推算本事件 VP。GMT FAQ v5 p17–18 明确三选项：GiveSixAndEnd（给对手六分并结束）、GiveSixAndContinue（给六分但不主动结束）、DeclineBoth（都不做）。决定者为 EffectController，给分对象为其对手。RootPhasingPlayer 保留为独立来源身份，不能用它替代一般的 EffectController 收款关系。

本批只绑定 module.turn-flow 提供的 RootEvent/ActionRound 和 HeadlineEvent/Headline。在这两个普通中立来源中，源牌拥有者、EffectController 和根 Phasing 相同；这是这两个来源的约束，不是所有嵌套事件的通则。Neutral 不能借五年计划的 US 关联子事件路线执行。任何其他父提供者尚未核实，返回 RequiresRuling，不因客户端自称 Verified 就获得权限。

独立权威牌区列表中的 InstanceId 与 CardId 都必须唯一，后者沿用基础牌组每个牌号只有一张实体的边界。恰好一个实体可以引用源事件 frame，且必须是本源实例；CardId、InResolution 和 frame 必须匹配。源实体仍在 Hand、错牌、错帧、相同 CardId 的另一个实例、另一张牌复用源帧、过期版本或参数摘要均在送分前拒绝。源上下文里的字符串不能替代权威实体检查。这些跨字段规则应用于 source_bundle、初次结算及历史收据重新验证；严格 Schema 形状并不代替领域校验。

## DEFCON 与事件发生事实

只有 DEFCON 2 才开放三个玩家选择。DEFCON 3–5 由内部 ResolveBlockedPrerequisite 命令处理，没有玩家第四选项：事件未发生，源牌进入 DiscardPile，不能因星号移出；仍结束这个已经接受的根出牌槽。核战等级、非法 DEFCON、已有胜利 VP 或 Finished 不接受新结算。

DEFCON 2 的 DeclineBoth 是成功但零转移，按星号移出。这被命名为 WG1-DERIVED-01：从 FAQ 的 used 条件与 R2015 §5.2 推导；不是官方对这个精确去向的额外专门答复。

## VP、终局和未定零分

先验证完整事件 VP 提供者、所有相关奖分已知，以及终局清理提供者。当前已核实事件只有卡文六分；未知相关奖分不能跳过并按六分先提交。方向、自动胜利阈值和合法 DEFCON 区间读取共享 victory/defcon；不会在本牌参数再写一套共享常量。

GiveSixAndContinue 仍在完整事件批次关闭后检查自动胜利，例如对手原有十四分，收到六分达到共享阈值仍立即结束。GiveSixAndEnd 同样先检查自动胜利，再按已接受的净 VP 决定胜者。非零正负映射是一般 VP 胜负语义的具名推导，不是假称专门 Wargames FAQ。

结束后恰零目前预检 RequiresRuling，送分、移牌、根槽完成都不提交。官方英文卡与 FAQ 没有专门零分条款；R2015 §10.3.2 零分平局在最终计分后段，而本牌跳过该阶段。采用 Draw 需要显式项目政策，不能移植 Playdek/Wikipedia 或假称官方 FAQ。这个开放点仅影响 GiveSixAndEnd 恰零；给六分后 Continue 恰零仍继续，DeclineBoth 在零分也不主动结束。

Wargames 结束只使用已经接受的事件 VP 结果，不调用 FinalScoring，不加中国牌最终一分，不按欧洲控制做地区计分，不做回合末军事扣分或扣留计分牌审核。案例带着这些不利旁证，验证它们不改变本事件结论且保持原值。

## 一个草稿内的清理与收据

本批普通源牌本身就是已接受根槽的实体。可信清理证据必须绑定根 ID、该实体、ActionRound/Headline 槽类型和当前版本；只能证明当前源牌及槽已完整覆盖。另有已接受父/根清理义务时先 RequiresRuling，不能忽略或自行移走另一张头条牌。给分、事件发生事实、按共享星号策略清理源牌、完成已接受根槽在同一草稿完成，再设置 Finished，最后写不可变收据；不会 Finished 后另发一个清理命令。

客户端请求只有命令/决定 ID、预期版本、三个选择之一和来源摘要；不能传送分量、胜者、事件控制者或骰点。内部阻止条件结算使用具名命令而不是增加玩家选项。所有分支零随机调用；非法 choice、过期或 Finished 拒绝时保持完整当前状态。

收据保存原请求、历史源上下文、历史前状态、决定与提供者证据、实际结果和后状态。SHA256 使用 UTF-8、键排序、紧凑 JSON、整数数值，拒绝额外键。读取时同时核对独立已接受命令索引的原请求/来源/前状态/收据摘要，并从历史输入重新推导所有结果、去向、根槽与终局顺序。把错误赢家重新哈希不能通过。哈希只是完整性检查，真实存储权限、签名和不可篡改索引没有实现。

合法同命令重试先查历史收据，验证调用者与原请求，再返回原结果；不读取后来 live VP/DEFCON/牌区来重算，不用旧 after 覆盖 live state。相同 ID 不同语义拒绝；相同 resolution 换 command ID 也不重执行。没有已接受历史时 Finished 是吸收态。离线模型的可信 actor/proof/anchor 是显式合成前提，不构成真实鉴权或真实完整提供者证明。

## 验证范围

临时 new-only 检查器只读取五文件与冻结基础参数，分别报告 data、contract、reference。Schema 形状测试和跨字段领域校验明确分开；不把 JSON Schema 当作全部领域证明。有限参考只演示纯值转换、完整历史校验与当前状态保持，不调用生产模块，不证明并发、存档、崩溃恢复或真正原子终局。十个运行时集成案例保留 planned-runtime；修后182独立检查及128额外探针通过，额外探针单列不计入3244。
