# 最终计分试玩接线工作记录

> description: 从93a072e继续最终计分Host、独立练习和公开结算画面；分阶段记录真实证据。

## Task1 已实现

新增第10回合权威命令、原子终局收据、公开六区报告与可信晚期场景初始化。保留原军事审核报告；零分Finished/赢家空，不再进入下一回合；同ID重试返回相同结果。测试覆盖最终正负零、欧洲控制、资格、来源回合、非法输入、溢出回滚和投影保密。

原生RED：2386通过/22失败；GREEN：2408/2408、0跳过、exit0，XML SHA256见验证报告。既有全部2386回归通过。尚未接新场景和UI，未构建新Player。

设计调整：CreateAtTurn仍是AwaitingStart；StartPractice允许显式第2回合以后初始化而无初始部署，第1回合部署门槛保留。没有公开历史或收据注入入口，也不声称加载存档。

后续：Task2数据装配与练习；Task3可视面板、审查、构建及鼠标验收。

## Task2 已实现

共享最终定义绑定与第16个显式JSON练习；原头条场景保留其能力并启用最终命令。新增22项场景与数据边界测试。首次RED编译因测试把属性当方法调用失败、无XML；修正测试后RED2408/22、GREEN2430/2430，0跳过/exit0。新Player尚未构建。

## Task3 源码完成，审查/构建待完成

结算控制器、固定状态栏、只读六区明细和返回地图；明确平局终局和欧洲优先。Editor/Player加入相同数据链，新增3项正式载荷与独立构建入口。新增9项：RED2431/8、GREEN2439/2439，0跳过/exit0。未用界面源码测试冒称真实鼠标验收。

## 一次独立审查与验收修整

全分支审查93a072e..6614a19：0 Critical、0 Important、1 Minor。按Minor补四项太空末期能力门禁测试，包括只开最终计分而不开下一回合；原代码均已正确拒绝。实际窗口还发现终局地图/中国牌仍有可操作提示，定位为Presentation缺少EndReason门禁。三种结局持有普通牌的测试先失败（RED2440/3），补终局只读提示/选牌门禁后GREEN2443/2443，0失败、0跳过、Unity exit0。

第一版Player已真实走通审核→最终计分→重发→返回地图→切地图/语言/身份，VP2→1美国获胜，重发仍v31/seq31。此后源码有上述提示修整，因此当前需要重新构建并确认，不能把第一版验收冒称最终构建证据。

## 最终交付

最终源码a0f7f405962cfb425ac00fb929b0d33639eccb03；Unity6000.6.0f1全套2443通过/0失败/0跳过（本批57项新增）。独立构建exit0，45份载荷逐字节相等；鼠标审核v29→30、最终v30→31、重发维持31，切地图/语言/身份仍VP1美国胜。持有方中国牌禁用，终局地图与计分预览显示只读复盘。两张截图由游戏F12导出，不含桌面内容。代码、XML、载荷、构建日志和画面哈希见[报告](../Testing/final-scoring-playtest.validation.json)。

Git：feat/final-scoring-playtest源码a0f7f40已push，ls-remote与本地一致；收尾文档另提交并再次同步。两项原有用户文件始终未暂存。旧Headlines播放器保留，新输出为Builds/FinalScoringPlaytest。

边界：没有声称103张牌或完整10回合全部可玩；特殊计分修正、太空末期能力仍有明确门禁。美国胜示例完成实际点击，其余结局由原生测试验证。下一批先太空回合末可选弃牌及其保密投影/续回合资格。

## 执行账本归档

```text
# SDD ledger — plan: Docs/Plans/2026-10-16-final-scoring-playtest.md

Base:93a072ec0dba5a6194043bfbc97c5a6ee4ac2f9c
Ruling: 按用户自主持续授权，在E:/TwilightStruggle创建feat/final-scoring-playtest分支，复用已激活Unity工程，不另询问设计/工作树或Git许可；代价：不可并行修改同一工作目录。
Ruling: 使用CreateAtTurn建立可信场景初始上下文，fixture经真实14行动到审核前，不公开内部Completed/Receipt写入口；代价：增加受限场景初始化API，需要严格验证回合/VP与不伪造历史。
Pre-flight: Task1 final receipt/projection由Task3读取；Task1 CreateAtTurn由Task2使用；Task2定义同一Scoring引用要求与Task1构造校验匹配；loader若创建新实例须绑定/比较再复用原实例。
Task1 active; BASE93a072e; brief=plan Task1 and spec invariants. Bash helper adapted to existing PowerShell/Python ledger workflow.

Task1 complete: native RED2386/22, GREEN2408/0/0 exit0. Ruling: trusted late-turn StartPractice needs no initial deployment, while turn1 gate retained; API cannot inject completed actions/history. Task2 active after Task1 commit.
Task2 complete: base3e34382; compile-error attempt fixed in test; native RED2408/22, GREEN2430/0/0 exit0. Task3 active after commit.
Task3 implementation complete: base86781ee; RED2431/8, GREEN2439/0/0 exit0. Whole-branch review, build, actual UI and final evidence pending.
Ruling: build frozen6614a19 concurrently with read-only final review to expose Player-only issues; if Important review changes source, rebuild before acceptance. No review or build claim before returned evidence.

Final review (93a072e..6614a19): fresh review_final_scoring_playtest returned 0 Critical / 0 Important / 1 Minor. Minor requests explicit final-enabled / next-turn-disabled coverage for EndTurnDiscardMaximum and ActionRoundAllowance. Added four cases, already green before production changes; no invented RED for coverage-only work.
Review declined-to-judge rulings: (1) native Player/layout/mouse owned by root, first build verified and refreshed build required after source changes; (2) active scoring modifiers deferred with unsupported handlers excluded from this bounded runtime; (3) ten-turn/full-card/network/AI/save acceptance explicitly out of this batch; (4) reflection-forged internal receipts are outside supported construction boundary, public/reachable invariants remain enforced; (5) concurrent documentation belongs to root and receives metadata/hash verification before push.
Observed UI issue: terminal map still invited placement and terminal China selection. Root traced to Presentation gates lacking EndReason. One follow-up pass: disable terminal selection, use read-only map/scoring text. Existing three outcome tests extended with ordinary held card: RED2440/3, all fail exactly at terminal CanSelectHandCard; four new space tests pass. GREEN pending. No further independent review requested.

Follow-up GREEN2443/0/0 exit0; XML b5c8d665a2f3e080b0cffa0e8816f1cf9453db427c1ff0065143a98bf3f997e0. Final Player closed via its returned window; old Headlines Player preserved. Freeze follow-up source before rebuild.

Complete: final source a0f7f40 built exit0, 45 payloads byte-equal, actual audit/final/retry/map/name/identity all verified; screenshots saved by Unity F12. Source push and ls-remote SHA equal a0f7f40. Final documentation follows in closeout commit.
```
