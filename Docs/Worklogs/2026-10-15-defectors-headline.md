# 已选叛逃者头条运行时

Task1：共享契约严格载入、同目录绑定、US优先/SU普通排序、成功与取消分离的不可变根记录。新增30项RED全部因缺失行为失败，GREEN30通过，可用回归2078通过；编译0警告0错误。报告和XMLhash见Docs/Testing/defectors-headline.validation.json。

物理取消和会话尚未实现，整批审查待完成；Unity许可未恢复，main和已验收播放器未改。

Task2：真实保留牌取消接线完成；行为RED 2通过/18失败，GREEN20通过，可用回归2098通过。首次编译缺测试namespace已修，不计行为RED。用户报告已登录Hub并打开项目，窗口清单确认Unity6.6工程运行，下一批原生验收须复核许可且避免并发编辑器。

Task3：公开成功/取消记录和优先策略已接；12项RED均失败，首次GREEN11/12暴露测试误匹配公开DrawPileCount，修正为精确私有字段后12/12通过，最终2110/2110托管回归通过。10份XML及11份源码hash见报告；编译0警告0错误。Unity窗口可读取，但关闭动作连续失败，已请用户保存并关闭编辑器以运行原生测试。

Unity打开后生成的未跟踪ProjectSettings/PackageManagerSettings.asset属于编辑器状态；保留原文件，本功能提交不纳入。

独立审查固定1b83002通过，无任何等级发现。共享源值、队列来源、取消/成功、原子性与公开边界均审阅；11源码/10XML校验通过。不批准main合并或玩家版本发布。

新增工程参考与头条试玩草案仅为文档；不是已实现功能，来源与本项目取舍分别说明。

## 本计划ledger归档

```text
# SDD ledger — plan: Docs/Plans/2026-10-15-defectors-headline.md
Base:a6162c9ee743cdf2dca8ebb71c4305221f02c8ce; branch feat/defectors-headline.
Pre-flight: Task1 bound definition/order/completion records feed Task2 authority resolver; Task3 only projects scalar immutable records already held by queue. Existing ordinary CreateOrdinary keeps explicit semantics. Host boundary unchanged.
Ruling: Continue existing user-authorized branch workflow and manual briefs (no Bash). Native license gate prevents main/player update; routine feature pushes authorized. No repeated approval menu.
Ruling: Implement only selected-headline Defectors, not ordinary VP/nested hooks — runtime currently lacks those source contracts — ordinary opponent-event route remains unsupported until its own batch.
Task1: in progress.
Task 1: complete (18e3926..03b0363c422a1032c6c422be603934c05c0ebe26; dotnet managed runner: RED 0/30, GREEN 30/30, regression 2078/2078; metadata passed; Unity blocked). Metadata first used descriptor ID instead of dataset_id; corrected dependency to the catalog's authoritative dataset ID, validation then passed.
Task 2: complete (03b0363..343939dc515dbab74a3995ff1d8926884dd6bc67; RED 2/18, GREEN20/20, regression2098/2098; metadata/diff passed). Native license recovery reported; validate after Task3 in single-editor lifecycle.

Task 3: Ruling: serialized secrecy test matched public DrawPileCount by substring. Assert exact DrawPile property instead; public count is existing allowed metadata, ordered cards remain private. Cost if wrong: must strengthen visibility tests, not remove legitimate public count.
Task 3: complete (343939d..1b830024a9463c840e2d26bcc28aca4f4074d6e2; RED0/12, intermediate GREEN11/12, corrected GREEN12/12; final2110/2110; metadata passed). Whole-branch review and native gate pending.

Final review: fresh-context gpt-6-astra on a6162c9..1b83002; 0 Critical/0 Important/0 Minor; 11 source and 10 XML hashes verified; 2110 managed cases substantiated, no repeated suite. Source checkpoint approved; native/main/player not approved.
Final: Ruling: Ordinary Soviet Defectors VP was explicitly deferred. Keep ordinary handler unsupported so selecting this source subset cannot silently award zero VP in ordinary play. Cost: that ordinary event cannot yet be played; subsequent batch must implement shared VP/OPS ordering and special terminal disposition.
Final: Ruling: Nested Defectors was explicitly deferred. Keep nested entry points absent until trusted source contracts exist; no parent-frame guess. Cost: Grain Sales or other nested cancellation remains unavailable, not silently approximated.
Final: Ruling: User reported Hub login/editor recovery; native test now waits on user closing interactive editor because UI activation failed twice. Preserve pending native gate; do not call licence fixed/test passed merely from editor visibility. Cost: source checkpoint available before rebuilt player, old accepted Demo remains.
Final: no deferred minors.
```
