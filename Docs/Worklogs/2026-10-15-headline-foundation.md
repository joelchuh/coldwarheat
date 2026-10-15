# 头条基础服务

description: 独立实现头条牌区、选择与普通排序，运行时事件根接线另批；记录真实托管验证和Unity许可限制。

基线76cb3bc，分支feat/headline-foundation。用户持续开发与Git授权有效，沿E盘独立分支；main保留0fc0033已验基线。计划见Docs/Plans/2026-10-15-headline-foundation.md，细节见同名.superpowers ledger。

Task1：新增不可变HeadlineReserved/GetHeadline，两个身份各至多一张；复用唯一分区、China独立和时代检查。检查六处生产构造：五处从旧区重建保留字段，InitialDeal创建新分区默认空。现有普通动作/审核/下一回合门禁与补牌拒绝保留中的头条；纯China转交保留实体。本批没有挂入真实头条阶段。

RED先9失败/6通过；实现分区与拷贝后，准确复现四处边界问题（普通动作/审核/下一回合错误接受，以及补牌错误触碰随机）；加入门禁后1869托管用例全通过，编译零警告错误。随机测试使用可补足的微型手牌目标，避免先因牌不足而假通过。原Unity许可证问题未解决，不能称EditMode/播放器/鼠标已验。

以上为Task1完成时状态；后续进度按下文。临时runner不入正式工具目录，数据字段没有在脚本里复制；JSON仅描述模块和真实验证状态。

Task2：Begin/PlanSelection/CanChoose及应用层投影已实现；支持集合和空间策略由权威输入，不把卡牌定义当运行时能力。28个新增用例RED全部NotImplemented，GREEN全过，选择及既有Session/Controller共191项通过。包括移位太空里程碑（第5格，非硬编码第4格）、双方先后提交、保密/先看后选、追平失去资格、拒绝/重复/不可变、计分牌合法、空覆盖不自动推进。核心结果含完整状态，仅供权威使用；真正Host认证/版本/幂等/发送过滤仍未接。

Task3：共享参数加载与只读普通排序完成。36项RED全部失败于占位；首次整批1932过/1失败定位为测试误写纳赛尔OPS，按正式卡牌定义1修正后1933项全过，编译零警告/错误。印刷OPS与PlayedBy保留，计分/平手从数据读取，Defectors等特殊优先未冒充实现。测试证据和源码hash见验证报告。

Unity Hub正常启动尝试因应用控制授权等待超时；没有自动操作登录/凭据/许可证。原有Unity exit198阻断仍在，未构建或覆盖Localized播放器。Task3整批独立审查及分支推送待完成。

整批审查：review_headline_foundation审查76cb3bc..b876dcf，无Critical/Important/Minor；23个源码/证据hash一致。1933项托管通过，另160项未选原生案例不计入；未重新跑Unity。接受其七项范围排除，详见报告review.declined_accepted；后续实施负责接事件生命周期、Host与UI。源码分支可推送，main/播放器仍不具备验收条件。

本批实施决定：现有持续开发与Git授权有效；缺少Bash时以同等任务brief/基线/RED-GREEN/ledger门禁替代包装命令，未新增永久脚本；沿E盘开发分支、保持原生验收；普通排序只作候选次序；旧流在保留区未空时拒绝推进；Hub控制超时不算许可恢复。这些决定若不适配后续完整流程，应修订相应接线与测试，不能绕过门禁。未延期代码Minor。

<details><summary>实施记录存档</summary>

```text
# SDD ledger — plan: Docs/Plans/2026-10-15-headline-foundation.md
Base: 76cb3bc33edfc1f81ead654dc75d177051c9baf7; branch feat/headline-foundation.
Pre-flight: Task1 produces immutable HeadlineReserved/GetHeadline; Task2 consumes it without mutating original zones. Task3 consumes shared Catalog/IDs and parameters, not secret mutable state; special ordering remains outside it.
Ruling: User ongoing development/Git authorization satisfies routine implementation decision; follow approved turn-flow spec and record new detailed plan without repeated permission gates.
Ruling: No installed Bash; skill task-start/task-done shell wrappers cannot run natively. Use manual plan-specific briefs/base ledger and exact test evidence with same completion gates; do not install shell or create permanent workflow script for this one incident.
Ruling: Single E project branch follows existing project practice; Unity build/license/asmdef/native verification pending, feature branch checkpoints only until that gate clears.
Ruling: Pure selection service returns no full execution queue; ordinary sorter is explicitly conditional. This avoids silently implementing Defectors as an ordinary2OPS event.
Task1: in progress.
Task1 Ruling: add fail-closed guards at legacy Check/CanAssess/CanBegin boundaries while pending reserved cards exist; phase orchestration is not connected yet, so silently advancing would be unsafe. Pure China transfer retains entities, but does not itself authorize playing.
Task 1: complete (76cb3bc..ad9b186400edb74e1682dd85bbe80650356c31d1; headline-zones-green.xml:1869 passed/0 failed; metadata136 passed). Task 2 base: ad9b186400edb74e1682dd85bbe80650356c31d1.
Task 2: complete (ad9b186..1cc9dc7065c87c305b720e69065a7f41874b5e9c; headline-selection-red.xml:28 failed, green28 and selection-session-green191 passed; metadata137 passed). Task 3 base: 1cc9dc7065c87c305b720e69065a7f41874b5e9c.

Task3: RED36 failed at stubs; first full run1932 passed/1 failed from test assuming Nasser printed OPS2. Verified canonical cards.json printed_ops1 and corrected test expectation only; headline-foundation-corrected.xml1933 passed/0 failed. Review/push pending.
Ruling: Unity Hub normal launch app-approval timeout does not establish license recovery; keep native acceptance gate pending.

Task3: source complete b876dcf; headline-foundation-corrected.xml1933/0, metadata137 passed; whole-branch review0/0/0.
```
</details>
