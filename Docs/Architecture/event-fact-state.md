# 实现规格：成功事实随权威状态保留

目的：补齐完整马歇尔出牌之前的状态边界，不单独开放卡牌或北约。当前 Player 保持 JapanPactPlaytest。

GameState 新增只读 EventFacts 与可选构造参数；未配置的旧练习保留 null，不能自动推定为已知空。非空账本必须匹配 gameId、Map 实例和真实 Cards.Catalog，且每条已提交事实的版本/序号不超过状态水位。不存在真实牌区时不能挂账本。

六条内部复制路径 Apply、ApplyRealignment、WithPractice、WithNextPracticeTurn、WithInfluenceEvent、WithDeployment 都传递同一账本实例。内部 WithEventFacts 只接同一定义/游戏/HistoryKnown 的精确原账本或前缀完全保留的一次追加；禁止清空、回退、不同历史与替换原有事实。方法不递增 GameState 版本或序号，账本事实绑定既有完成序号。

验收先写失败测试：错误权限/水位/缺牌区拒绝；真引擎完成得到的追加接受，原状态不变；重试幂等；null/清空/替换/未知变已知/多次批量插入拒绝；六条状态复制逐一保留，普通出牌和跨回合实际路径保留。测试使用正式资料与 genuine InfluenceEventFlow；仅对 internal copy helper 的定向测试可用反射，不在生产代码使用反射。

边界：不注册 PracticeInfluenceCatalog 持续牌、不接 CompleteEventAction、不初始化 Demo 账本、不增加公开 UI 或存档承诺。正式宿主后续必须把影响力、记录、牌去向同一事务发布。完成本批后下一任务为马歇尔真实出牌、两种对手顺序、头条/FiveYearPlan 与 Demo 入口。
