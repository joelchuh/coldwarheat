# Missile Envy 设计契约

> description: 查最高 OPS 交换、实体转交、有限子事件与独立的下一行动轮义务。状态：有限参数与契约已登记；参数Bound、处理器planned；C#/Unity 与真实消费者未实现。

先读 [参数](../../Data/Cards/deluxe-2015/missile-envy.parameters.json) 的 description、pending 和 shared_refs，再看 [Schema](../Contracts/missile-envy.schema.json) 与 [案例](../Design/missile-envy.cases.json)。有限设计检查见[验证报告](../Design/missile-envy.validation.json)，没有游戏运行通过记录。

原文已核对 [Deluxe 英文牌图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf) p8 #49、p2 #8；[2015 规则](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) 2.2、7.4、9；[FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf) p6、p12、pp19–20，仅采用适用于 Deluxe 的段落。完整源文件与直接引用 JSON 的 SHA256 见 cases.reference_snapshot。Verified 是明确原文，TextualDerivation 是文字推导，EngineeringBoundary 是项目协议设计，Pending/RequiresRuling 不表示已采用规则。

用户于 2026-10-13 确认两项项目解释，唯一来源 ID 为 PROJECT-ME1-USER-20261013（HumanConfirmed，非官方专门裁定；原始两问两答位于候选外 work/missile-envy-user-interpretations.json，SHA256 已纳入来源快照）：原持牌者按当前普通 OPS 修正和共享上下限比较最高值，修正后并列也由持牌者选择；真正使用 OPS 时按实际使用者当时修正重新计算。空手或只剩计分牌时，事件无变化完成、Missile Envy 进弃牌堆，不交换，也不产生后续义务。计分牌无 OPS 值，中国牌独立且不可被事件强迫，不构成普通候选。裁定范围不延伸到其他未回答边界。

交换必须原子移动两个实体：目标 Hand → InResolution，Missile Envy InResolution → 原持牌者 Hand。取得己方或中立牌走事件路线；取得反方牌走 OPS 并压制该事件。根 Phasing、根 slot、父帧和事件实际使用者分开记录，子事件不能覆盖根责任。正常交换后父事件完成时来源牌已经转交，禁止通用清理再次弃掉它。只有已确认的无候选分支直接弃来源牌。

当前仅有 Fidel 完整普通事件行：读共享参数与 Cuba 稳定度，清除 US 影响并确保 USSR 控制；实际子结果验证通过后移除 Fidel，完成父帧并保留转交的来源牌与义务。另一行只计算反方取得牌的实际使用者 OPS 预算并创建等待；真实行动、OPS 消费与完成收据尚未实现。中立牌虽然明确走 Event，但中立控制方与复杂消费者未登记，保持 Pending，不能因为数据 Bound 就假定支持。

持续义务使用独立记录，绑定 owner、来源实体、原 resolution、创建命令、创建回合及 due 状态。父帧完成不删除它。普通后续行动门禁只允许该实体按 OPS 使用；头条不会履行它。FAQ pp19–20 明确必须计分时保留 Missile Envy，下回合首个 AR 按 OPS 用；FAQ p12 明确陷阱时应优先弃该牌，Purge 使其不足阈值时例外。有限门禁探针保存与读取义务，真实计分、回合推进、陷阱弃牌及骰子仍 UNRUN。例外后恢复、其他额外行动、被其他事件弃走或 UN 覆盖均未默认补规则。

玩家投影：选牌前只有持牌者看见自己的手牌与最高候选，来源事件玩家和旁观者只见等待；交换后所选牌与 Missile Envy 转移可见，其余手牌、牌库、凭据、收据和 proof anchor 始终不公开。旧命令重放返回当时已接受投影，不用后来状态重新投影，也不覆盖后来 Finished/Draw live state。

协议先闭合形状验证再读取字段。Begin → ChooseHighest → CompleteChild 每次保存完整 before/result、命令 nonce、先前 ID、身份、根/父/子帧与引用摘要；历史 keys/request/result IDs 与独立 accepted-ID、nonce 索引逐项一致。只清空任意一个表不能绕过去重。无效回调保留已经接受的等待状态和此前账本。独立 anchor 是合成可信输入，SHA 或 provider 标签本身不构成真实鉴权。

案例明确区分结构/引用检查、有限状态转换、协议拒绝、投影和持续义务门禁。真实游戏、持久化、网络鉴权、OPS/计分/陷阱消费者、递归组合与实际终局全部有 UNRUN 验收项。正式卡牌定义与目录只登记参数及来源，未接通运行服务。作者与整合检查的范围见验证报告；原始作者引用快照作为设计时的历史证据保留。

独立审阅第一轮修复：后续门禁先校验完整 live 状态，只处理 Active、无未完成子帧的已接纳后续根行动。phase/owner 必须与当前根状态一致，整个当前快照及上下文绑定独立合成 scheduler anchor；缺少 anchor 不会执行义务。创建与完成收据各有独立摘要，门禁重算有限交换/完成结果，核验原 obligation ID、owner、实体、来源 resolution、创建回合和命令；原 resolution 不等于后来当前根的 resolution。一个来源实体出现多个义务直接 Pending。Trap/Purge 输入必须与共享效果 ID 对应的当前普通修正总量一致；仅单独 Purge 的已知例外可返回结果，正向抵消组合仍 Pending。此处的合成调度证据不是实际 TurnFlow、陷阱消费者或生产鉴权实现。原 91 案例定义保留，11 个 gate 夹具仅补独立接纳证据；新增20项修前RED/修后GREEN场景、预期与结果记录在验证报告；临时检查器保留在工作证据区，不是生产游戏代码。

共享帧与能力边界见[借牌模块](nested-card-events.md)，当前义务的真实TurnFlow/Trap接入仍待实现。
