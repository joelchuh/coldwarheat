# SDD ledger — plan: Docs/Plans/2026-10-14-coup-attempt-plans.md

前批基础计算正在固定HEAD 54afbcce5a42e60ec716b647508ec0ae6d7a3347独立审阅；此时仅新增独立文件，不改其受审范围。最终BASE在前批审阅/同步后记录。
Pre-flight: Task 1定义/计划及其实际证据由Task 2文档消费，接口无冲突；复用CoupDefinition/Defcon/Military/PCG，不更改前批实现。
Ruling: 沿用Windows特性分支/Worklog账本而不建立不适配的Bash工作区；当前单一实现者，并行隔离代价同前。
Ruling: 先完成不可变整体计划，再接实际GameState宿主；计划包含军事/DEFCON/影响力/RNG/收据/版本，但玩家授权、卡牌游标和已发生的非核胜利仍由宿主验证，代价为当前exe不能出牌政变。
Task 1 in_progress：先接口桩及测试，行为RED后实现。
BASE 74d8883ac42d7821cb3acbf64703bff94e4bea67；前批已快进/上传并核对远端；新增文件已保留到feature/coup-attempt-plans。

Task 1 RED: coup-attempt-red 1561总计/1511通过/50新增失败（NotImplementedException），0跳过、退出2；XML 1c486ab783038559f78b8c243197cfb49412489e02ae0e96fe52cb1d7c9adf1c。实现完整不可变After与收据、先军事再fork/影响力/DEFCON的计划时序。

Task 1: complete。coup-attempt-green 1561/1561、0跳过、退出0；XML 0b0ea07d4a3897189c50193346b89f1bfdb869693bf2b70e49e86153d7672919。50新增测试覆盖计划/收据/4096上限。Task 2文档/描述/消费者记录及实际报告已更新，metadata与独立审阅待完成。

Task 2: complete。metadata通过115描述/catalog、70数据集、182符号、582引用、204读取文件；代码未再改变，沿用本次1561通过的实际Unity证据。整批独立审阅待完成。

Final review: fresh gpt-6-astra reviewed 74d8883..d077a61，ready to merge，0 Critical/Important/Minor；七份输入和RED/GREEN XML摘要/计数一致，1561全绿。不追加修复或重复审阅。
Final Ruling: 实际GameState并发比较/原子提交/持久化、会话身份/阶段/持牌/预算/根槽仍由宿主接入；SourceActionId仅因果信息，不阻止新ID重复根动作。若当玩家入口使用，会绕过这些授权，因此保持纯计划接口。
Final Ruling: 强度与军事信用来源仍由可信策略核实；地区/免费政变/危机/条约/豁免未实现，当前非空效果拒绝。代价为这些分支暂不可用，不能把General值当完整地区预算。
Final Ruling: 非核整局门禁、核战赢家提交和后续卡流程截断由胜负宿主负责；本计划只使DEFCON核战不可继续接受新政变。误当整局终局实现会允许其他未门禁操作。
Final Ruling: 回合切换/旧收据恢复/跨进程防重/迁移和外部事件后聚合合并均未实现；Create不能充当恢复或每次操作重建入口。代价是先接主状态才能与其他命令交错。
Final Ruling: 随机依赖可信，显式检查不能防恶意隐藏别名或伪造快照；未来不可信插件需额外隔离。
Final Ruling: 不做UI/播放器构建或完整可玩宣称；审阅采用已指定规则依据和测试证据，不提供独立规则再考证/完整RulePack认证或Git同步证明，后者由实际同步命令核验。
Deferred minors: none。
