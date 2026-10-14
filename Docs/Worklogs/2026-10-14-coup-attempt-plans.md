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
