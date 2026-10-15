# 头条收集与本地会话

description: 查可选回合入口、收集权威牌区和原子提交；核心及固定身份Host/公开投影已实现，事件选择后原子继续已实现。

先读[模块描述](../Descriptors/headline-session.module.json)。Definition的HeadlineParameters由可信工厂装配，默认null沿用旧Demo跳头条。启用后StartPractice或BeginNextTurn完成原准备，再进入CollectingHeadline；收集不会另发牌或改善DEFCON。GameState.Cards必须与HeadlineSelection.Cards同引用，选择与结算队列互斥；即使尚无保留牌也拒绝普通行动。

BeginCollection仅供可信回合编排，不是客户端跳阶段命令。Submit复用支持集合/身份和空间能力选择策略：首张锁定只提交一次；第二张选齐清除收集并调用既有头条结算，整体返回到下一等待/终局/普通行动。失败无部分状态或Steps；保留上次已发布的首张锁牌。零合法候选停留明确边界，不跳过/判负。

```mermaid
flowchart LR
 A[布置完成或下一回合准备] --> B[启用头条配置]
 B --> C[收集双方选择]
 C -->|首张| C
 C -->|第二张| D[原子事件编排]
 D --> E[等待选择/终局/苏联行动1]
```

18新增RED后GREEN；最初跨回合测试误用全时代夹具被正确拒绝，修正夹具后补充回归2017通过。Unity160项原生用例、构建/鼠标未执行。现有Demo未启用，完整会话与UI不能据此称已完成。

SubmitHeadlineAsync固定到PlayerSession.Side，共用宿主锁与跨命令收据。Command只含ID/GameId/ExpectedVersion/Card；取消在收据前停止；相同内容回历史收据，换内容同ID冲突。两方同版本竞争一成功一过期，失败收据也缓存，刷新后用新ID重交。普通CardAction与Headline共享ID命名空间。

PublicHeadlineView收集时只投影自己的牌/候选和对方锁定，双方齐备才有公开Order；太空先看后选复用已有投影。Resolving区分PlayedBy与WaitingOwner，Completed保留两张公开历史直到下一回合。PublicHeadlineStep仅含牌/出牌方/等待/终局及既有公开效果摘要，不含中间View或权威GameState。LocalTestSnapshot.ReservedHeadlines只在显式启用观察时可读且不可改。

Task2新增18项先RED后GREEN，公开JSON秘密检查/双并发/历史收据/跨类型冲突/取消均通过；可用托管2035通过。Host等待选择的最终继续已由下述Task3完成，旧Demo仍未启用。

Task3在同一Host事务锁内将头条选择的原始效果交给CompleteChoice；当前牌去向及下一张效果全部成功才发布。后续溢出返回失败、空Changes/HeadlineSteps/序列标记和旧View；已经在先前请求完成的前缀保留。成功EventCommandResult.Changes保持原来的本次选择范围，HeadlineSteps附上当前及后张公开摘要，View是最终状态。普通事件沿原路径。

端到端验证了两个不同Owner连续选择、已发布前缀、选择后技术失败不泄露、首/第二张核战责任、历史收据、下一回合只补一次及太空先看后选。太空案例使用明确的合成一步必成功轨道，通过实际Space行动获得能力，再进入回合2，未用反射或内部状态构造绕过。13新案例首次9已有行为通过/4缺失行为失败，完成后13通过；最终49新增、2048可用托管通过。该数字不含160原生排除项。

```mermaid
flowchart TD
 A[固定身份会话选择事件目标] --> B[同一锁：ID/版本/Decision校验]
 B --> C[原始影响力草稿]
 C --> D[完成当前头条并继续下一张]
 D -->|成功| E[最终View和公开Steps，提交一次]
 D -->|失败| F[旧View与空Changes/Steps，不提交]
 E --> G[重复ID返回原收据，当前状态保持]
```

真实Unity/程序集隔离、播放器、地图点击均尚未验收；旧Demo未启用该可选配置。Defectors已选头条通过可选绑定接入，见[该模块](defectors-headline.md)；嵌套/事件OPS等未知处理器仍明确拒绝，不能把这批服务当完整头条规则覆盖。
