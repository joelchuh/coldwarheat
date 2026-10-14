# 有界 JSON 契约验证

> description: 定位共享ContractJson解析/Schema子集；查关键字形状、局部引用、tuple顺序及额外数组项的拒绝边界。
> 入口：[模块JSON](../Descriptors/bounded-json-contract.module.json) → ContractJson.Parse / Validate。

这是仓库数据加载器使用的受限验证器，不是完整JSON Schema实现。先整体检查所有Schema分支，再验证数据；未知/形状不符关键字与递归引用拒绝。仅解析本地$defs引用，不访问网络或执行表达式。解析限定2,000,000字符与深度64；检查/验证共享100000预算，不能在组合失败后恢复预算绕过。

2026-10-14为正式事件步骤数组补充prefixItems与boolean items：prefixItems逐位置验证已有元素；数量要求仍由minItems/maxItems负责。items对象只验证prefix之后的尾部，false禁止额外尾部，true/缺省允许尾部。没有prefix时items作用于全部元素；空prefix数组、boolean前缀Schema等超出本实现支持形状，明确拒绝。

回归ContractTupleTests覆盖顺序/缺项和额外项、尾项类型、布尔items、最小数量、不使用的坏Schema/递归分支；DefconVpEventTests实际加载正式三牌Schema并拒绝错顺序/错公式/来源/第三牌参数。运行证据随[本批报告](../Testing/playable-defcon-vp-events.validation.json)保存。后续新增Schema关键字先补行为测试，不在加载器里删除不认识的约束。
