# 头条收集与本地会话接线

description: 将已独立验证的秘密选择/核心编排接入同一个权威GameState、固定身份会话及回合入口；还不启用旧Demo或添加原生UI。

Spec: Docs/Architecture/headline-runtime-integration.md；持续开发/Git授权有效。使用executing-plans，3个任务各TDD，整批一次独立审查。

## 设计决定

1. ActionPracticeDefinition最后追加可选HeadlineOrderParameters。null明确保持旧练习跳过头条；开启后StartPractice完成布置及BeginNextTurn准备完成都进入收集。同一回合补牌/改善DEFCON仍只发生一次，不在收集入口重新发牌。新开关由可信工厂配置，不接受客户端开关。
2. ActionPracticeState追加HeadlineSelection及CollectingHeadline阶段。选择和结算队列不能同时存在；收集只在普通计数0、无ActiveCard/OpponentOps/期末报告时存在；Catalog须同一实例。GameState.Cards与选择状态.Cards须同一权威快照，构造拒绝脱节。普通动作门禁把收集期明确挡住。
3. HeadlineFlowRules.BeginCollection只开可信阶段，不是玩家跳过阶段命令；Submit(state,side,card)复用HeadlineSelectionRules。第一张只原子提交锁牌；第二张选齐后清除收集状态并在同一事务调用HeadlineResolutionRules.Begin，推进到选择/终局/普通行动。若第二张之后技术失败，整个第二次提交不发布，第一张已锁定前缀仍在。
4. 新HeadlineCommand继承SessionCommand，只携带CommandId/GameId/ExpectedVersion/Card。IGameSession新增SubmitHeadlineAsync，PlayerSession身份固定；LocalGameHost复用同一gate和所有命令共用的receipt字典。同内容重试返回历史收据，新内容同ID冲突；过期拒绝也缓存，刷新后须新ID重交。
5. PlayerViewSnapshot新增PublicHeadlineView：收集时复用过滤后的HeadlineSelectionView；揭示后显示只读双方顺序、完成游标、当前出牌方与等待Owner。不得包含Definition、权威状态/其他手牌/牌库/RNG。LocalTestSnapshot单独加入ReservedHeadlines，只读观察入口显式启用才允许读取。
6. HeadlineCommandResult和EventCommandResult附有界PublicHeadlineStep列表，单步只投影Card/PlayedBy/等待或终局和既有公开影响力/地区计分/DEFCON/OPS结果，不包含任何GameState。View始终为本次事务最终快照；历史收据保留其旧View，不覆盖当前状态。
7. SubmitEvent在当前为头条根时，把原始InfluenceEventFlow草稿交CompleteChoice，成功后一次性发布最终状态。错误不能把raw.Changes当已接受变更发送；普通事件沿原路径，错方/旧Decision/旧版本不推进。选择后进入下一张的摘要与等待信息一起返回。
8. 不增加任意Reveal/SkipPhase/Advance按钮，不引入联网/存档或凭据。现有Demo工厂保持HeadParameters=null；后续有原生验证才能另建头条试玩入口。

## 任务与验收

- Task1 Core收集和可选回合接入：先RED初始/下一回合收集、默认旧行为、单次锁牌/源不可变、双牌自动推进、非法卡/空能力覆盖、太空先看后选、牌区与选择快照不一致拒绝、普通动作拒绝、第二次提交错误保持首张。再实现。Expected: 新测试GREEN及既有1999托管回归通过。
- Task2 会话命令/过滤投影：先RED双身份可提交、秘密单锁不可见、太空例外、同时旧版本竞争一胜一过期、新ID重交、相同ID重试/跨类型冲突、错Game/负版本/默认Card/取消、只读观察受限与所有公开JSON无秘密牌/权威对象。再实现。Expected: 所有已注册接口真实编译、测试GREEN。
- Task3 选择继续与跨回合：先RED两个Owner等待恢复、结果合并公开步、重复选择不再执行、选择后失败无raw变化、先方终局吸收、完成普通轮后T2重新收集且只补一次、历史收据重试不倒退状态。再实现，核验全可用集合/metadata/source+XMLhash、整批审查并推送。

真实Unity测试、asmdef边界、构建与鼠标仍是独立门禁；许可证未恢复时只推源分支，main与已有播放器不变。

## 文件与依赖

Task1修改Core/Rules/ActionPracticeState.cs、ActionPracticeRules.cs、PracticeNextTurnRules.cs、Core/State/GameState.cs，新增Core/Rules/HeadlineFlowRules.cs和EditMode/HeadlineFlowTests.cs。Task2新增Application/Sessions/HeadlineSessionContracts.cs、LocalGameHost.Headlines.cs，修改SessionContracts/PlayerViewSnapshot/LocalTestSnapshot/LocalGameHost及新增HeadlineSessionTests。Task3修改LocalGameHost事件提交与EventSessionContracts，新增HeadlineSessionContinuationTests。所有相对源码路径位于Assets/ColdWarStruggle/Scripts，测试在Assets/ColdWarStruggle/Tests/EditMode。

接口依赖：Task1的HeadlineResolutionTransition由Task2过滤为公开结果；Task3复用相同公开step投影。SessionCommand的收据字典共用，禁止第二个独立字典让跨命令类型ID绕过检查。完成的Headline队列可留作公开报告，但下一回合必须清除；收集状态不能沿普通构造被遗失。

## 验证与提交步骤

每任务：先新增行为测试、最小编译占位、观察命名RED XML实际失败；再实现并GREEN；在每任务的源检查点提交前跑可用托管回归。保留唯一XML文件名，不覆写证据。完整命令和XML输出/hash在验证JSON及执行账本记录。临时.NET8/NUnit runner位于聊天work/managed-check，编译实际源码与测试副本；现有160项原生相关用例排除，不能冒称完整Unity检查。

最后同步Docs/Descriptors模块与catalog、Wiki/index、Architecture状态、Worklogs、Testing验证JSON，执行Tools/Scripts/validate-project-metadata.py --project-root E:/TwilightStruggle与git diff --check，期待exit0。一次独立整批审查；无阻塞后常规提交并推源分支，ls-remote SHA等于HEAD。不上main、不更新播放器，等待Unity原生门禁。

## Review Focus

固定身份与跨类型幂等、秘密牌JSON泄露、错误路径返回原始成功变化、第二次提交和等待选择后的技术失败原子性、历史收据不能回写Host、初始和下一回合单次发牌/轨道改变、未注册处理器不自动跳过、无Unity依赖和未执行原生测试不算通过。

## 进度

- [x] Task1 收集与回合入口
- [ ] Task2 会话和公开投影
- [ ] Task3 事件继续与跨回合、审查和同步
