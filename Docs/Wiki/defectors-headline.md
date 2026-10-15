# 已选叛逃者头条运行时

description: 已实现US优先取消和USSR普通顺序自取消、共享弃牌与公开完成记录；普通VP/嵌套未接。先读[模块JSON](../Descriptors/defectors-headline.module.json)，再按入口读取函数。

共享事实来自Data/Cards/deluxe-2015/defectors.parameters.json及印刷目录；DefectorsHeadlineLoader严格校验完整契约、profile和所支持语义。ActionPracticeDefinition.HeadlineDefectors默认null，配置需同目录且无重复handler；只扩展头条支持集合，不扩展普通对手事件。

CreateSelected只对美国所选叛逃者提序；苏联自选仍按普通印刷值排序。CurrentCancellationSource从可信当前根和已完成前缀推导；AdvanceAfterCancellation写Cancelled，AdvanceAfterResolution写Resolved。记录和队列不可变，精确当前牌/方只能推进一次。游标推进不是物理去向的证明。

实际流程：收集并保密 → 选齐后揭示排序 → US源成功并弃牌 → 待执行USSR根取消并弃牌（包括星号和计分牌）→ 苏联第一普通行动，普通行动计数仍0。USSR自选则在自己的普通顺位仅取消自己；US事件等待选择就等待，若首方终局保留当前Resolving和未来Reserved审计现场。共享Move/Commit处理牌区，后续技术失败不提交任何草稿。

PublicHeadlineView公开OrderStrategy和CompletedRoots；秘密收集时strategy=null、记录为空。PublicHeadlineStep.Completion从步骤结束的队列取对应根，等待和终局未完成根为null。取消不伪造成功事件摘要。现有Host门禁/锁/版本/幂等复用，历史结果不会覆盖当前状态，也不泄露后来的公开信息。

本批新增62项，最终2110项托管补充回归通过，160项原生相关测试另列。Unity已由用户登录打开，尚待关闭编辑器后完整原生验证；旧Demo和main未改，未构建或鼠标验收。证据见[报告](../Testing/defectors-headline.validation.json)，决策与任务记录见[工作日志](../Worklogs/2026-10-15-defectors-headline.md)。

来源：既有[Defectors设计](defectors.md)、[运行规范](../Architecture/defectors-headline-runtime.md)、GMT FAQ v5 #103。普通USSR行动1VP和嵌套来源仍为后续批次。
