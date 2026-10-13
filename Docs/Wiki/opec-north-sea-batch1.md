# OPEC 与 North Sea Oil

description：查七国控制指标 VP、OPEC 永久禁止、美国本回合总行动额度，以及来源树、根收尾和历史重放。

状态 2026-10-13：两牌 verified-subset 数据/契约设计；所有 C#/Unity 处理器、鉴权、真正随机/存档/事务服务仍 planned。入口：[描述](../../Data/Descriptors/opec-north-sea-batch1.data.json)→[参数](../../Data/Cards/deluxe-2015/opec-north-sea-batch1.parameters.json)→[Schema](../Contracts/opec-north-sea-batch1.schema.json)→[案例](../Design/opec-north-sea-batch1.cases.json)。整合后86 Bound/16 Unresolved，102处理器均planned；[验证报告](../Design/opec-north-sea-batch1.validation.json)记录202新＋3244旧＝3446离线检查。另45独立探针通过，单列不重复计数；10运行验收未执行。

## OPEC：当前真实控制与完整奖分

英文卡 p10 #61 的七国是 Egypt、Iran、Libya、Saudi Arabia、Iraq、Gulf States、Venezuela。固定 USSR 每控制一国得卡文 VP，不按整个中东、战场属性、地区计分或有影响力就计分。地图稳定度、CardId 印刷事实与 VP 方向/阈值读取共享数据；实际事件步骤读取完整当前影响力。OPS-before 改变控制会改变随后结果，缓存报价不能代替当前快照。

先核实 North Sea Oil 禁止实例的成功来源事实与双期限记录。禁止有效则 SuppressedNoEvent，不发生 OPEC、无 VP；合法父 OPS 仍按原根继续。无禁止且零国控制是成功 ResolvedNoChange。全部国家构成一个事件 VP 批次，在完整批次后检查共享自动胜利。OPEC 本身非星号，不创造影响力、军事、DEFCON 或随机。新的合法再次事件可按新局面计分，同命令重试仍返旧结果。

## NSO：永久禁止与本回合总名额

英文卡 p13 #86 与 §7.3 支持 OPEC 禁止全局持续，而卡文 this turn 将美国行动额度限定为激活回合。两者分别记录并关联同一唯一成功事实，源牌星号移出不消灭持续效果；回合末只使额度到期。仅看到卡牌在 Removed、一个布尔值或缺少合法源证据的成功历史都不证明该禁止成立。

美国取得的是总 8 个根 ActionRound 槽的资格，不是补 8 张实体、不立即插队、不重置太空尝试。与独占 Space8 同时存在时取 max(普通、NSO、有效 Space)，不是相加成 9；ON1-D01 明确这是卡文与既有额度模型的具名组合推导，未找到专门官方叠加 FAQ，也不是对未知未来提供者规定全局 8 上限。对手追到 Space8 只撤销该太空来源，NSO 仍可独立支持本回合总 8。

计数读取 CompletedRootActionRoundSlots。Headline 不算行动槽；UN 用两张实体仍只一个根槽；FYP 子事件不另占 US 根行动。US 自己正常打 NSO 的根最终只加一，USSR 根触发 US 事件不会替 US 消耗一。已完成 8 个根槽不产生第 9 个。仅额度并不证明有合法牌或动作，China 不被强迫使用、陷阱/计分义务/普通太空尝试限制都交共享资格提供者；未知时 RequiresRuling。

在 ActionRound 阶段既有根及所有子帧和源实体去向完成后的正常边界查询额度；Headline 刚完成并不允许立刻进入额外行动决定。额外槽只有一个绑定当前授权集合、完整状态、提供者证据与版本的可选决定。额度提供者必须绑定当前回合及版本。放弃记录当前实际同时提供该可选权利的来源 token，不能跳过普通名额，也不能反复弹出同一来源；永久 OPEC 禁止仍在。放弃命令另有严格 quota_input、quota_receipt 和独立 quota_anchor，历史验证与同命令重放遵守事件收据同样的“不覆盖当前状态”规则。已放弃后又出现独立新来源、重开旧回合、迟到授权或新的 NSO 重复激活仍 RequiresRuling，不猜测刷新、叠加或调度。

## 来源树、父控制者与终局

只设计 RootEvent、OpponentCardEvent、HeadlineEvent 和现有 FYP 的 US 关联子事件四条具名路线。源对象、根、事件与父帧分别绑定独立实体位置和 ledger；InstanceId、基础单牌 CardId、FrameId、ResolutionId 唯一。当前事件 frame 必须 Active；父 root/子关系不能别名、错卡或已完成。源卡仍在 Hand 或另一个实体占用同一事件 frame 在修改前拒绝。

OPEC 固定 USSR 控制，NSO 固定 US 控制。普通对手关联事件保留真正 RootPhasingPlayer，不能把它当 EventController。FYP 父事件控制者读取既有 US 关联参数，始终为 US，即使根是 USSR；其随机对象是 USSR Hand，只有选中的 US 关联 NSO 能进入此子路径，USSR OPEC 不可进入。父 FYP 的已接受选择收据必须精确绑定子实体/卡牌/frame/resolution，父处于 WaitingChild；已有父成功事实或 Completed 父不能重执行。未绑定嵌套提供者先 RequiresRuling，不复制尚未修好的其他候选代码。

验证全局仍 Running、DEFCON 高于共享核战等级、未先有获胜 VP；所有影响当前事件的 VP/相关效果/根清理证据先齐备。普通成功事件若尚有已接受父 OPS，保留根源卡 InResolution，记录事件完成并返回原根；不提前星号处理或消耗根槽。FYP 子 NSO 完成自己的星号去向，随后完成 FYP 事件事实，再按根剩余工作决定父源牌去向。没有余项则清理已接受源/父与根，最后计一个 ActionRound 槽，Headline 零槽。

OPEC 达终局时在同一草稿中关闭完整 VP、记录事件事实、清理已接受源/必要父/根、取消剩余根工作，再设置 Finished。不能 Finished 后补清理，也不能为走完根而继续 OPS。离线 proof 与 ledger 是合成的已核实前提，尚不构成真正来源服务或服务器鉴权。

## 收据与有限验证

原请求、来源、历史前状态、完整结果及后状态进入不可变收据；独立已接受索引锚定 command/resolution 与请求/来源/前状态/收据摘要。读取时重算历史结果并核对独立锚，改结果再自哈希仍拒绝。合法同命令在后来地图、Space、回合、额度或 Finished 改变后返原结果，不用旧 after 覆盖当前状态。摘要不是鉴权或防篡改存储的实现。

原始核对日期 2026-10-12；见 GMT 英文卡 p10/p13、R2015 §2.1.7/4.5/5.2/6.4/7.3/7.5/10.3。FAQ v5 未找到两牌专门回答。参考检查只做严格数据形状、合成 ledger、整数指标和有限额度/收据投影；真实动态调度、合法 Space、UN/China/陷阱执行、存档网络与事务保持 planned-runtime。Space 用途完全不调用这两个事件处理器，沿既有 Space 流程弃牌并跳过事件，不在本批造假模拟一次真实太空执行。

全局快照结果域现允许 Finished/Draw；本事件不因此新增平局分支。主入口的新行动仍拒绝终局或活动局面的非空赢家，历史重放保持原结果与当前状态。范围与实际证据见[全局结果契约](draw-global-contracts.md)。
