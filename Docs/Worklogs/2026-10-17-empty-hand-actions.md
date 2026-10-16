# 普通空手行动衔接

基线 eee8ba7；分支 feat/empty-hand-actions。用户授权持续自主开发，沿用 E:/TwilightStruggle 已导入的 Unity 工作目录，不新建重复 Unity 导入。不改变两项无关未跟踪文件。

实施计划：Docs/Plans/2026-10-17-empty-hand-actions.md。

验证过程：首次 empty-core-red 2567通过/17失败；其中测试准备器重复 StartPractice，不能作为有效行为 RED。已根据准备器实际契约修正，并在 empty-core-red-verified 重新验证缺失功能，未提前实现功能。首次运行期间改动了测试源文件，证据按实际 XML 对应已编译版本解释；后续运行期间不修改 C# 源文件。

Ruling：普通空手按当前名额逐次跳过，保留中国牌选项；公开计数含合法跳过。与既有 turn-flow 已确认设计一致，不另造规则。

后续：完成 Core/session → UI/fixture → 新上下文审查 → 原生与实际 Player 验证 → Wiki/JSON/推送。

第二次验证暴露旧最终计分准备器不按早期时代筛选手牌；已重建真实时代分区。empty-core-red-clean 为2571通过/13项预期失败，错误均为尚未接入的跳过行为。实现后 empty-core-green 为2584/2584通过、0跳过、Unity exit0，XML SHA256 eccfc4370fe212f5b557b60a49c73dac15ac0df655b32245ccdb2bf801660af4。

UI/fixture：empty-ui-red 2595通过/5项预期失败；empty-ui-green 2600/2600、0跳过、Unity exit0，XML SHA256 6fe246891d2cea7a0ada6cd7b1d8d4f3d537066afc9a3539a588ff00189fc74e。元数据150描述/150目录/78数据/367符号/662依赖/329文件通过；Player构建和新上下文审查进行中。
