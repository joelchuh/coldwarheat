# SDD ledger — plan: Docs/Plans/2026-10-14-coup-base-calculation.md

BASE 4e32063d95bcb75fc2280ef794e5068955ed3011；已核对GitHub main一致/干净，分支feature/coup-base-calculation。
brainstorming采用已确认普通政变设计的有界实现；writing-plans/executing-plans inline，两任务和一次最终独立审阅。
Pre-flight: Task 1 CoupDefinition及Calculate由Task 2 Loader和真实数据集成消费，无接口冲突。
Ruling: 沿用Windows特性分支/持久Worklog账本，不使用不适配当前项目的worktree/Bash辅助脚本；当前单一实现者，代价为并行隔离较弱。
Ruling: 本批只做已掷骰的可信计算步骤，分离随机/军事/DEFCON提交；完整宿主仍须在取随机数前先做目标/干预验证。代价为尚不能在exe完成普通政变。
Task 1 in_progress：先写接口桩及行为测试，再运行RED。

Task 1 RED: coup-calculation-red 1483总计/1452通过/31新增失败（NotImplementedException），0跳过、退出2；XML 7212aca99b48e985708db27dd2c87fbea4e3e9c7544cc8a503c4c865418e7fd5。实现基础门禁、long差值/checked影响力与战场意图。

Task 1: complete。coup-calculation-green 1483/1483、0跳过、退出0；XML b764a46aa05c384a4132978384b6330d7015049e504d901cd331802a30c33d22。Task 2共享数据/加载桩和测试已写入。

Task 2诊断: coup-loader-red在测试JSON字符串编译时报CS1003，无测试报告，不是行为RED。原因是生成测试源码时引号未保留；改用C# verbatim literal后重跑，业务桩不变。

Task 2 RED: coup-loader-red-02 1511总计/1483通过/28新增失败（加载桩NotImplementedException），0跳过、退出2；XML 1747fd93fade0ab90644c9167136ec69efe6c22e45e029912d63ce10b9f56c0d。随后实现有界JSON解析、精确字段/profile/来源校验及数值装配。

Task 2代码: coup-loader-green 1511/1511、0跳过、退出0；XML 265e30cfaff81943ad1142c1ea7427e045413b0a053a174b0f4d3659cbc19f32。模块/数据JSON、catalog、Wiki与报告已更新；metadata及最终审阅待完成。

Task 2: complete。metadata通过114描述/catalog、70数据集、177符号、579数据引用、201读取文件；Unity1511全绿。一次整批独立审阅待完成。

Final review: fresh gpt-6-astra reviewed 4e32063..54afbcc，ready to merge，0 Critical/Important/Minor；五份输入和四次有效RED/GREEN摘要一致，1511全绿。编译错误尝试不算行为RED。
Final Ruling: 客户端身份/归属/阶段/版本、强度/影响力新鲜性和效果集合完整性继续由可信宿主验证；本基础计算不是直接玩家入口，错接会允许伪造事实。
Final Ruling: 条约/逐卡拦截/豁免/地区奖励未实现，非空活动效果保守拒绝；代价为这些真实游戏分支暂不可用。
Final Ruling: RNG/防重和军事/DEFCON/胜负/影响力原子时序由下一批计划接入；本批组合测试不证明宿主提交，误当完成会有重复副作用。
Final Ruling: 非核胜利终局吸收、完整RulePack认证/数据真实性及存档兼容仍属宿主/后续加载契约；目前无完整游戏或存档入口，不扩大本API职责。
Final Ruling: 未构建播放器或验收鼠标；审阅不重跑Unity/metadata或验证远端，采用作者实际证据，Git同步由随后命令核验；代价为没有审阅者独立运行证据。
Deferred minors: none。
