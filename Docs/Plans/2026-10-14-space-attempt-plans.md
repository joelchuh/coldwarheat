# 普通太空尝试计划：计数、骰子、奖励与重复保护

执行方式：executing-plans inline，两项实现任务，最后一次独立审阅。依赖刚完成的轨道基础；不重建播放器。
Spec: Docs/Wiki/space-race.md，Data/Rulesets/deluxe-2015/space-race.json，Docs/Wiki/deterministic-random.md。

## 范围

本批实现可信规则层的普通单格尝试计划，不把它接成玩家命令。Application仍必须验证持牌/出牌阶段/中国牌状态、完整有效OPS和活动限制/用牌钩子；本服务不宣称这些门禁已经实现。它计算目标、次数、骰子、奖励与牌去向意图，没有GameState或CardZones写入。事件推进、特殊奖励政策、完整存档留后续，不能拿普通尝试代替事件推进。

SpaceAttemptRequest保存AttemptId、SourceActionId、TurnNumber、Actor、共享CardDefinition对象、正整数EffectiveOps。数值是可信OPS层的报价输入，不能来自客户端。计分牌永远拒绝；普通事件牌及中国牌可计算，后者仅返回ChinaTransferRequired，宿主未来需正确转交并处理钩子，不能放普通弃牌堆。带星事件牌的太空意图仍为Discard，ExecuteCardEvent=false。

SpaceAttemptState保存不可变Track、同一TurnCalendar与TurnContext、双方已用次数、只读收据。Create建立从可信位置开始、次数为0的初态，不是恢复接口。BeginNextTurn只能推进相邻合法回合，保留轨道和收据、次数归零；同回合重试原样返回，不再次清空已用次数；旧回合、跨回合和日历终点拒绝。日历对象绑定避免随请求替换规则。

ValidateAttempt纯查询，先检查请求回合，再检查非计分牌、目标存在、当前动态能力所给额度、目标最低OPS，返回明确资格枚举。PlanAttempt先按ID检查已提交收据：同一请求事实重试原样返回，次数/奖励增量均0，随机返回当前快照，不能回退到原掷骰时的快照；同ID不同事实拒绝。共享CardDefinition使用引用绑定，当前不提供存档恢复，未来恢复必须重新绑定同一已批准目录。新请求通过资格和4096条工程记录上限后，才能Fork一次并调用NextInt一次（范围采样内部可消耗多次原始PCG输出）。拒绝Fork同实例、快照不一致或越界骰子；随机源接口仍是可信依赖，不能检测恶意共享隐藏状态。

成功仅前进一格，目标FirstVp/SecondVp由对方当前是否已达目标确定；VP符号为US正、USSR负，奖励数值来自共享定义。失败也次数+1，位置不动，VP0，牌去向意图相同。先后奖励纯单向普通路径，不涵盖事件跳格的到达记录政策。内部构造新状态及收据，输入状态和随机源不变。成功到2格可动态取得第二次机会；对手随后追平会取消未用机会，已用次数不倒扣。

Plan返回Before/After、Receipt、AlreadyApplied、VpDeltaToApply、RandomBefore/RandomAfter；宿主必须原子提交状态、随机数、牌区/中国牌、VP与行动游标，再检查胜负。receipt含原请求、原始骰子、起止格、是否先到与奖励、原随机快照；重复计划不重发牌区动作或回滚RNG。

## Task 1：计数、资格与回合边界

文件Core/Rules/SpaceAttemptState.cs、SpaceAttemptRules.cs；测试SpaceAttemptTests.cs。
先接口桩+RED，再实现：空值/非法enum/id/ops，计分牌、终点、OPS目标门槛、双方独立次数、当前能力额度、绑定回合，Create输入不变，顺序新回合/相同回合重试保持已花次数/拒绝跳过或退回，只读收据。

## Task 2：事务计划、骰子、奖励与重试

先新增计划桩及测试观察RED，再实现：所有8目标格×每骰点×双方的成功/失败和先后奖励；失败用次数但不移动；高OPS不改骰子；到2格再尝试、对方追平即时拒绝；同ID重试/冲突/不同card对象；非法请求无Fork无NextInt；确定性PCG恢复/成功不变原随机/失败异常不泄漏；坏Fork/骰子拒绝；返回牌去向意图区分中国牌和带星事件。检查回合重置后旧请求不重放。

保持描述JSON、Wiki、正式metadata、测试报告与Worklog一致；全量Unity回归，一次整批review，例行提交/push并核验远程。描述不能把未接线纯计划称为完整太空出牌。

## Review Focus

资格查询不取随机；Plan先验证/去重后Fork；原状态不变；重复请求不奖励、消费、移动或回退RNG。动态额度是里程碑独占条件。回合次数清零恰好一次。CardDefinition绑定、可信有效OPS/外部授权边界清晰。数字取共享轨道；普通/中国牌去向只是意图，宿主未接入。暂未支持事件推进不得暗自套普通奖励。
