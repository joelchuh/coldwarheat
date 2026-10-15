# 叛逃者：已选头条的有限运行时

description: 在已接线的头条根/保密会话上实现具名优先与取消，源静态事实来自既有defectors参数，不扩大到嵌套或普通行动事件。当前实施规范；上一分支a6162c9已审查并推送，用户持续开发授权有效。

## 依据和目标

既有Docs/Wiki/defectors.md与Data/Cards/deluxe-2015/defectors.parameters.json已核实；2026-10-15重新查GMT FAQv5.pdf p18 #103，确认US选Defectors即使苏联较高OPS仍取消；苏联自己选为头条自取消且0VP。该PDF p14 Grain Sales仅说明实际先后不能倒转历史，本批无嵌套入口。卡图PDF文本层为土耳其语、截图此次超时，不能声称本次再次目视英文；保留既有英文目视来源和共享CardCatalog，不改印刷事实。

用户目标仍是一个窗口可双边测试、规则与地图解耦、描述和知识可逐层检索。此次增加真实有用的特殊头条覆盖；旧Demo不启用且Unity许可仍待恢复。

## 设计

1. Core的DefectorsHeadlineDefinition绑定Catalog/CardId，只有selected-headline语义；Infrastructure.DefectorsHeadlineLoader.Load(json,schema,catalog)校验既有完整参数契约/profile，再显式验证本子集语义，读取稳定ID。共享soviet_action_round_vp不会在本子集用到，不复制数值。ActionPracticeDefinition末尾可选HeadlineDefectors，默认null；同目录、互斥handler注册。
2. HeadlineOrderRules.BuildOrdinaryOrder和CreateOrdinary行为保持。新增HeadlineResolutionState.CreateSelected(...,DefectorsHeadlineDefinition)复用普通校验，只有US保留的是Defectors才将US根置首，并记录具名HeadlineOrderStrategy。USSR自选不改变普通顺序，也不取消US。
3. 为现有队列追加只读CompletedRoots记录：Card/PlayedBy/Outcome(Resolved或Cancelled)/CancellationSource(仅取消有值)。CompletedCount由记录数决定；Current/ResolutionId保持。队列保留已验证的Defectors绑定；CurrentCancellationSource只在USSR自选Defectors、或US已按优先策略成功完成Defectors且当前为USSR时产生。AdvanceAfterCancellation(card,side)只能推进该精确当前根，来源由状态推导，调用方不能自报；AdvanceAfterResolution拒绝必须取消的根。两种纯游标方法都不是效果已执行/牌区已清理的证明，宿主编排仍须先完成实体去向。终局未完成当前根的既有审计策略保持。
4. SupportedCards仅在可信HeadlineDefectors存在时包含该牌。普通Event/OpponentOps支持集合不因此扩张；US普通OPS/双方Space沿原共享路径，USSR普通事件VP和嵌套仍未实现。不能因对方可能被取消就允许选择未知处理器。
5. 结算当前US Defectors：无需地图/VP效果，源牌弃牌，当前根Resolved；下一步识别已成功的US Defectors来源，当前仍待执行的苏联根取消并弃牌（即使星号或计分牌）。每张实体只移动一次，两个根都完成，普通行动0、OPS0、RNG不动。
6. 结算当前USSR Defectors：按普通位置取消当前自己，0VP、弃牌、Cancelled来源为自身，继续仍待执行的US根。若US首张已终局，不另做清理；若US事件待选择则仍等待原Owner，完成后再自取消。
7. PublicHeadlineStep从该步骤完成后的队列中投影对应根的Completion（Card/PlayedBy/Outcome/CancellationSource）；等待和终局未完成根为null。PublicHeadlineView显示真实排序策略及只读完成记录。记录仅含公开标量，无权威State；不在CardActionTransition再复制一份相同事实。选择仍保密，揭示后可区分“效果发生”和“被取消”，不伪造空成功事件。现有Host事务与receipt重放不变。
8. 保留旧Demo工厂和main/player，源码分支验收后同步；不自动恢复许可/登录。

## 分批TDD

Task1 定义/加载/排序/完成记录：先测试契约字段错误、profile不符、目录不符、空/重复/错副本、正常排序不变、US特权高于4OPS/计分、USSR自选普通排序、完成记录只读/精确一次/取消非成功，再实现。

Task2 真实根取消：先RED支持开关、US取消高值/星号/计分、无地图/VP/DEFCON/RNG变化、USSR自取消先后、US等待后才自取消、首张终局截断、取消与弃牌原子性/溢出、未知handler仍拒绝、第一普通行动苏联1，再实现共享Move/Next的最小复用。不能跳过全部队列只清空保留牌。

Task3 公开会话与数据说明：先RED秘密第一牌、揭示优先/取消摘要、同ID重放/跨type冲突/历史结果不回写、等待选择恢复返回自取消、最终公开完成记录与普通动作边界，再接投影。补完整可用托管回归、来源/源码/XMLhash、JSON/Wiki/flow/worklog；一次独立整批审查，无阻塞后推源分支并验证远程SHA。

## 原生门禁

真实Unity/EditMode/asmdef/构建/鼠标均等待许可。临时托管测试不是玩家体验验收，不覆盖或宣传现有LocalizedPlaytest已有本功能。后续另接普通USSR使用该牌的1VP及其对手事件/OPS顺序，须遵守既有defectors终局去向契约，不能直接套旧终局留Resolving策略。
