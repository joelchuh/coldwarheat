# Star Wars（SW1）

description：查美国太空领先门禁、公开弃牌选择、独立父子帧、有限消费者矩阵、完成收据与历史重放。这里是verified-subset 参数与契约设计；没有 C#、Unity 或实际运行注册表。数据描述的 ready 只表示参数文件可读，所有处理器与 planned_entrypoints 仍为 planned。Schema 的 `#/$defs/star_wars` 是可独立验证单牌参数的闭合契约，完整数据集的 parameters.star_wars 通过 `$ref` 使用同一契约；正式 effect_contract 已登记该片段，处理器仍 planned。

美国太空位置必须严格领先苏联。并列或落后时事件被阻止，Star Wars 最终去 Discard；2015 规则 p5 §5.2 示例4明确说明落后情况。US 从公开 Discard 中选择非计分实体，东南亚计分也排除。所选牌只走 EventOnly，不能直接改用普通 OPS 或 Space；如果所选事件本身授予行动，仍须未来登记那个事件内部的独立行动提供者。子事件本身的前置、禁止和控制方规则继续生效。

满足领先但没有任何合法非计分弃牌时，无变化完成并按成功星号牌移出游戏。这仅来自用户于 2026-10-13 确认的 PROJECT-SW1-USER-20261013，标记 **HumanConfirmed 项目解释**，不是官方专门裁定。“有规则合法牌，但本批尚未支持其消费者”不等于空池。这样的牌保留在公开合法候选里，提交选择在移牌之前返回 Unsupported。不能因此自动替用户选择、跳过事件或移除源牌。

规则来源是 [GMT 英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf) p13 #85、[Deluxe 2015 规则](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf) p5 §5.2，以及 [FAQv5](https://www.gmtgames.com/nnts/FAQv5.pdf) p19 公开弃牌、p12 UN/WWBY。英文 #8、#21、#30 已核对，支持下表中的有限目标。官方事实用 Verified，并列与计分类型排除用 TextualDerivation；帧、收据与能力登记属于 EngineeringBoundary，未接入的交互标 Pending。没有把所有规则合法目标标成已经支持。

| 实际有限消费行 | 可接受的证据范围 | 控制方与去向 | 来源 |
|---|---|---|---|
| cap.star_wars.fidel | 完整 Fidel 影响力变化；无活动干预 | USSR；清 US，再从共享 Cuba 稳定度补足控制，成功移出 | 英文 p2 #8、R2015 §2.1.7/§5.2；共享 early-influence-batch1 |
| cap.star_wars.decolonization | USSR 确认的四个不同合法国家；无活动干预 | USSR；共享地区并集、额度与每国数值，成功弃置 | 英文 p5 #30、R2015 事件控制；共享 early-influence-batch1 |
| cap.star_wars.nato-blocked | 完整前置历史证明两个许可事件均未成功；只覆盖 blocked 分支 | US；子 NATO 弃置，Star Wars 成功移出 | 英文 p4 #21、R2015 p5 §5.2 示例1；共享 treaty-protection |

上表是离线设计消费模型的实际支持列表。成功 NATO 尚不支持，任何未知活动效果返回 RequiresRuling。子事件成功星号去 Removed，非星号去 Discard；有星号但前置不成立仍去 Discard。成功完成了取回并尝试子事件的 Star Wars 自身按成功清理，即使子事件前置被阻止；这是 §5.2 与各自事件结果分离的文本推导，不冒称专门 FAQ。

Begin 只支持具名 RootEvent、OpponentCardEvent、HeadlineEvent 来源，并按共享时代区间检查 Star Wars 已进入牌组时代。未知嵌套包括 FYP 尚未登记到本批来源契约，停止于变更前；这并非新增游戏禁令。RootSlot 与 RootPhasingPlayer 不因 US 选牌或 USSR 控制子事件改变。SourceEntity 是唯一 Star Wars InResolution；SelectedEntity 是原 Discard 中的另一实体，接受选择时原子移到独立 ChildFrame 的 InResolution，不复制牌。ParentFrame/ParentResolution、ChildFrame/ChildResolution、源实体、目标实体、选择方、actual_card_user=US 与 targetEventController 分别保存。当前 EventOnly 子帧 operations_actor=null；未来事件内部赠予行动须另建授权。China 仍在独立牌区，手牌、已预留头条、其他 InResolution 和源牌都不是候选。root_card_use_count 只计根普通用牌，不把子实体事件误作额外根行动槽或宣称已接入 UN 的 actual-use 钩子。

流程为 Begin → AwaitingSelection → 接受 US 选择并保留父帧 → AwaitingChild → 接受绑定的子收据 → 子牌和父牌清理。Fidel 没有决策；Decolonization 的独立子决策由 USSR 确认，US 取回此牌并不获得这项选择权。参数、地图、牌印刷字段和太空轨道位置均按稳定 ID 与 JSON pointer 读取共享定义，不在本批重复数值。

父事件只在已接受子完成收据后清理。若已核实源仍有根行动的 RootOperations，普通成功完成只保存事件已完成，源仍 InResolution，根未关闭，实际 OPS 和最终清理由外层未来提供者负责；本模型不会冒充已经做完。头条完成不增加行动轮名额。即时终局在同一子完成事务内接受后，父剩余 OPS 不执行，根责任仍是原 RootPhasingPlayer，清理并关闭根。当前没有能产生终局的已登记目标行：终局核战案例是单列协议夹具，用来检查责任与收据/cleanup，不能用作 Fidel 或其他目标的真实能力，也不能授权 Wargames。

每次命令绑定源摘要、参数/共享引用摘要、已认证主体和角色、CommandId、nonce、ExpectedVersion、前序 CommandId。首个 Begin 必须没有前序，后续只接自己的已接受链。子证明另绑定根、父子帧与 resolution、两个实体、固定 EventOnly 路线、精确 provider/capability 行、控制方、子 nonce、序号及实际前后局面的 hash。检查器不仅比较成功标志：它重新计算 Fidel 影响力、Decolonization 的地区/数量/阵营计划、NATO 前置、所有未受影响字段及真实实体去向，再与子收据完整 after_state 比较。

已接受的等待状态不会因坏回调回退。提交入口先校验命令、认证上下文和所附子证明的闭合结构，再读取必填字段；缺字段、错误类型或额外字段稳定返回 InvalidCommand、InvalidAuthentication 或 InvalidProof。提交失败保留当前状态、已接受收据、独立锚与 nonce 记录。历史收据保存原请求、认证上下文、source、authority、子证明、完整前后有限局面及原结果；独立锚和逐步重算共同校验，自改 after_hash 不能改变语义。历史核验还逐条重建唯一 nonce→CommandId 索引，与保存的 nonce 索引完全比对，并要求收据字典键、request.command_id、result.command_id 三者一致；缺失、额外、改映射或重复项返回 InvalidHistory，不自动修复。相同历史命令只返回原 accepted 结果，不覆盖后来 live 状态，不刷新手牌或当前可操作视图。命令内容冲突、复用 nonce、来源变更、越链和伪造 callback 均拒绝。

全局快照允许 Finished/winner=Draw，因此后来平局时仍可重放旧收据。Active 或 Running 下任何非 null winner 仍非法；Finished 上新的动作和新的当前选择查询仍被拒绝。这个全局兼容不扩大局部 controller 或赢家枚举。即时终局的真正原因与目标消费规则尚须由具名运行提供者接入。

弃牌身份和已接受的目标身份公开；US 专属的是可提交选择的权限，不是假装公开弃牌是秘密。当前动作视图核验完整已接受链与当前局面。对手/旁观者拿到公开投影，不能拿到私有手牌、牌库顺序、nonce、证明、原始收据或来源凭证。历史结果不能充当后来新手牌的查看许可。离线独立锚都是可信合成输入，有限模型不证明真实会话认证、哈希存储、磁盘持久化或原子事务已经实现。

运行 capability registry 仍 planned：以规则集/参数摘要/source_kind/target_effect_id/route/provider/coverage/控制规则/收据版本精确登记，处理器入口还要验证这一来源。不能因为一个 card_id 的参数 Bound 就授予任意新来源。扩展时必须同步目标自身的 descriptor/contract/cases，并运行真实集成验收。

Pending 项保留为可检索记录：UN 配牌与 WWBY（FAQ 已说明互动，但提供者未接入）、Grain Sales/Missile Envy 递归与实体保留、Wargames 即时终局和 Draw、Ask Not 等待抽牌、成功 NATO、事件自身赠予 OPS 与中立牌控制、活动干预和其余消费者。这些未知组合不猜默认动作；保留 Unsupported 或 RequiresRuling。

文件：[参数](../../Data/Cards/deluxe-2015/star-wars.parameters.json)、[数据描述](../../Data/Descriptors/star-wars.data.json)、[契约](../Contracts/star-wars.schema.json)、[案例](../Design/star-wars.cases.json)。共享背景：[嵌套来源](nested-card-sources.md)、[影响力第一批](early-influence-batch1.md)、[回合状态机](turn-flow.md)。案例把来源检查、有限规则模型、对抗命令/证明、历史/视图、Schema、终局协议夹具和具名运行验收分层；运行验收全部 UNRUN。正式验证报告见 [设计验证](../Design/star-wars.validation.json)。新增119项有限检查与14项UNRUN运行验收分开；登记参数不代表真实执行器已实现。

共享职责见[借牌事件模块](nested-card-events.md)，先按[JSON描述](../Descriptors/nested-card-events.module.json)定位计划入口。
