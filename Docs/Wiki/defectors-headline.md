# 已选叛逃者头条运行时

description: 已实现定义/载入/排序/完成记录，尚未接物理取消与会话。先读[模块JSON](../Descriptors/defectors-headline.module.json)，再按入口读取代码。

共享事实来自Data/Cards/deluxe-2015/defectors.parameters.json和印刷目录，不复制普通行动VP数值。CreateSelected只对US所选叛逃者提序；USSR自己选择保持普通排序。AdvanceAfterCancellation推导取消来源并写Cancelled，不是Resolved，也不自动改牌区。普通CreateOrdinary保持原义。

流程：共享契约校验 → 同目录绑定 → 两张保留牌排序 → 精确当前根的Resolved/Cancelled只读记录。物理弃牌、宿主与公开记录为后续任务，普通VP和嵌套无实现。

30项新测试及2078项可用托管回归通过。Unity许可阻塞，160项原生相关测试未运行；无新构建或UI验收。详见[报告](../Testing/defectors-headline.validation.json)。

Task2已接：ActionPracticeDefinition.HeadlineDefectors仅扩展头条候选；同目录、重复handler校验；US源Resolved后取消苏方，USSR自选按普通次序Cancelled，使用共享Move/Commit完成弃牌。等待/终局保持审计现场，后续溢出整批无草稿。20项新增GREEN与2098项可用回归通过，公开Completion投影仍待Task3。
