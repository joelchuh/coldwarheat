# 头条收集与会话接线

description: 从0764085130af7434b1e5d5fb334c227b5dde45ec分出feat/headline-session，按三任务接入单个权威宿主，保留原生许可门禁。

Task1核心收集完成。18项先RED；首次GREEN17/18失败来自全时代夹具错误进入第二回合，生产TurnDeal拒绝正确，改该夹具后18通过，整体可用补充2017通过。可选参数null保持旧行为；收集绑定牌区，第二次选择连同后续结算原子返回。下一回合仅补牌一次，完整队列清空。太空先看后选已由基础纯服务覆盖，端到端案例移到Task3。未恢复Unity许可，不重试Editor、不更新main/播放器。

Task2完成固定身份SubmitHeadlineAsync、共享幂等缓存、PublicHeadlineView/Step和只读观察。18项RED→GREEN，整体2035补充通过，0失败/跳过；真实并发一成功一过期，JSON无对方秘密牌、无权威草稿。同批没有新增工作脚本或改规则裁定。

Task3完成头条事件选择与后张原子继续。13项新增初跑9通过/4失败（缺失连续执行与回滚行为），实现后13通过，整体2048通过；不能将已有9项写成RED。公开摘要包含本次选择/后一张且无权威状态，技术失败不发布raw变化。合成太空一步轨道通过真实Space行动获得资格并在回合2验证先看后选；基础回合发牌和旧收据重复仍只发生一次。整批新增49，Unity160项原生排除，未构建/鼠标测试。等待独立审查。

整批独立审查e67028ae：0Critical/0Important，1项相邻摘要过期Minor，按用户知识同步要求已修正；运行源码未变化，不重复测试。13源码/10XML摘要一致；6个meta固定提交和当前字节相同、GUID均32hex。审查只批准源检查点，Unity/main/播放器未批准。常规分支推送及远程SHA另行核验。

## 执行账本归档

```text
# SDD ledger — plan: Docs/Plans/2026-10-15-headline-session.md
Base:0764085130af7434b1e5d5fb334c227b5dde45ec; branch feat/headline-session.
Pre-flight: Task1 trusted immutable HeadlineResolutionTransition feeds Task2 filtered public result; Task3 uses same bounded step projection and shared receipt dictionary. Catalog/Cards identity must remain bound during collection. No conflicting consumers found.
Ruling: Continuous user development/Git authorization and AGENTS26 supersede repeated brainstorming/finishing approval menus. Use existing feature-branch workspace; no Bash available so briefs/ledger are maintained manually with equivalent evidence gates. Source-only until native license recovery; wrong choice costs unproven native integration, not an accepted-player replacement.
Task1: in progress.

Task1:18 tests RED failed; first GREEN17/18. Remaining failure was all-era fixture entering turn2, correctly rejected by existing TurnDeal. Restricted that test fixture to Early deck/reserve (production unchanged). Space-policy end-to-end is relocated to Task3; existing selection tests already cover policy.
Task1: complete (0764085..fdbfbc537e1f1046cafe6626bf2e2b5817a74b91);18 RED/18 GREEN,2017 full managed passed;139 metadata passed. Task2 base:fdbfbc537e1f1046cafe6626bf2e2b5817a74b91
Task2: complete (fdbfbc5..3b765ecf3a93e5ee467b640800f2e48d160fb5bc);18 RED/18 GREEN,2035 full managed passed,139 metadata passed. Task3 base:3b765ecf3a93e5ee467b640800f2e48d160fb5bc

Task3 source:13 new cases initially9 passed/4 failed;13 GREEN and2048 full managed passed. Space policy relocation complete via actual synthetic-track Space and next-turn entry. Review pending.
Ruling: EventCommandResult.Changes retains current-choice-only compatibility, HeadlineSteps covers all current/following effects; UI must consume final View and steps, not assume Changes contains the entire transaction.

Task3: complete (3b765ec..e67028ae6eabe0eb2b31e9032dddbd03277736aa);13 new tests9pass4fail before implementation,13 GREEN and2048 full managed passed,139 metadata passed. Wholebranch review0Critical/0Important/1Minor.
Final: Ruling: Neighboring stale retrieval summaries were already identified by implementer before review and are mandatory synchronization under user workflow/AGENTS23. Reconcile the documentation minor now rather than defer inaccurate capability metadata; no runtime fix/test repeat. Cost if left wrong: repeated work or improper feature assumptions.
Final: Ruling: Unity EditMode and160 native cases remain unexecuted because of license; supplemental single-assembly runner does not establish asmdef isolation. Keep feature branch separate — cost if bypassed: native integration faults could ship.
Final: Ruling: Player build/mouse/UI and enabling existing Demo remain deferred, existing factory retains null configuration. Cost if enabled prematurely: untested player workflow.
Final: Ruling: Unsupported Defectors/nested/event OPS handlers reject before execution; full-game/extra-space/final-scoring remain outside this bounded batch. Cost: prototype cannot complete every legal game path yet, explicitly documented.
Final: Ruling: Network authentication/persistence/reconnect/full replay and receipt retention are not shipped by local fixed sessions. Cost: current session lifetime is the bound; not a persistent or authenticated network protocol.
Final: Ruling: Existing effect rules reused, integration reviewed rather than exhaustive new rule-lawyer audit; retained existing official data and accepted interpretations. Cost: an underlying rule defect would need its own reproduction/repair.
Final: Ruling: Reviewer did not establish remote/main/player state; implementer verifies remote after push, main/player unchanged. Cost if skipped: local commit could be mistaken for remote backup.
Final review: approved source checkpoint, no runtime blockers. 13 source hashes and10 XML hashes matched. Six new .meta fixed-head/live bytes also match and GUIDs are32hex; initial visual suspicion retracted, no metadata defect or fix. Documentation minor reconciled; no deferred minors.
```
