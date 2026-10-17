# 成功事实随权威状态保留

先读 [模块JSON](../Descriptors/event-fact-state.module.json)，再按 entrypoints 定位。底层记录与规则查询见 [成功账本](event-fact-foundation.md)。

GameState.EventFacts 可为空，表示旧练习未配置历史；不能由缺字段推断已知空。明确账本必须绑定相同 gameId、Map 实例和真实 Cards.Catalog，所有事实的版本与序号不能超出状态水位。

Apply、ApplyRealignment、WithPractice、WithNextPracticeTurn、WithInfluenceEvent、WithDeployment 六条复制路径保留同一不可变账本。普通 OPS 不生成事件成功记录。

内部 WithEventFacts 只允许相同账本的幂等调用，或同一定义/对局/历史完整性且精确保留旧事实前缀的一次追加。禁止清空、重写、批量导入或把未知历史改成已知。附着记录不额外推进状态版本/事件序号。

这仍是状态基础，尚未在真实出牌里调用追加，也不注册马歇尔/北约或改变 Player。后续 CompleteEventAction 与五年计划子事件必须把影响力、记录及牌去向原子发布；存档迁移另建明确入口。真实验证见 [报告](../Testing/event-fact-state.validation.json)。
