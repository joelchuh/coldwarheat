# 播放器载荷核验工作记录

> description: 记录单源构建清单、只读脚本及真实验证，不混同游戏规则测试。

BASE ad696b09913b6fa5223a678a2fa00398c0a8ec00；分支feat/player-payload-verification。沿用用户项目E:/TwilightStruggle，保留两项未跟踪用户文件。brainstorming/writing-plans/executing-plans inline；用户已明确自主决定无需询问。

Ruling: 工具从C#字面Copy列表迁移到单源JSON，而非长期正则解析源码——构建和验收共享同一映射，减少漏加与假成功；代价是新增一个受限C#加载/文件复制组件，需要原生回归。
Ruling: 工程策略与映射分别保存；前者含exe/目录/程序集/工程限额，后者只列文件对应关系。不复制游戏参数；不引入数据库或泛化脚本平台。
重复流程证据：Docs/Worklogs/2026-10-17-early-war-play.md、2026-10-17-space-event-play.md、2026-10-17-olympic-play.md（已按实际路径及58/61/66份构建核对记录确认）；同一批重建不累计复用次数。

Task1 RED：3503/3457/46，46项均为NotImplementedException，旧3457无回退；XML20710f9c4d6827488b4dd1efa7464b52d4d8dd3e11310ffd1a05141ffc89b653。源路径大小写碰撞负例改用同一合法Data前缀，避免仅因前缀大小写错误而拒绝。实现清单与预检复制，移除Editor字面Copy函数，新增独立PayloadManifestPlaytest目标；GREEN待运行。

首GREEN 3502/3503：唯一失败是测试要求精确IOException，而真实缺源返回其子类FileNotFoundException；生产已正确拒绝且未覆盖。将断言改为捕获IOException家族，保留文件未改断言；同时补三个真实Windows junction检查（根/源/目标），验证现有NoLinks实现而非模拟文件属性。新完整回归运行中。

Ruling: 核验CLI将播放器限定在Builds/子目录、构建日志限定在TestResults/子路径，避免将源目录当作构建副本自比或扩张到无关目录；现有全部Player符合此约定，代价是任意外部构建需先纳入项目标准目录。只读工具不推断源码提交。

Task1完成：3506/3506原生通过，零失败/跳过/不确定；49个新用例（其中3个真实Windows junction）通过。XML1177ca873342dcb814b5821779307d56aa75ce1eb704a0ab61f5bac87640bb7e。清单与旧Copy 66条映射完全对应；实际Player尚待新核验脚本落地后一并构建验收。

Task2 CLI：真实68例先在stub全部RED，再实现全部GREEN。三处真实junction、实际读期间修改故障注入、只读重复执行均覆盖；临时fixture保留。已有Olympic Player用新工具核验66份通过（非新构建证明），报告TestResults/player-payload-existing-olympic.json，SHA5b761dd33d7b1385e48ee15fb159660132d7a4915a0d90a99a7ee24cb26faf8d。

跨消费端边界：发现Json.NET接受Python拒绝的尾逗号/单引号/无引号属性/前导零/十六进制。6例原生RED（3512/3506/6），XMLc912890ac76f6cf1aec9dde5c3089f41d9c4c5eccf36b61d4cc1b4b65a508ad0；增加严格词法过滤，结构与重复键仍由原解析器检查；额外正例保留描述内标点/转义/中文。最终GREEN另记。

新增五份描述和三catalog登记；元数据178描述/86数据集/457 C#入口/732数据依赖通过。所有脚本依赖共享数据ID；未新增技能或泛化执行器。

最终Task2原生GREEN：3513/3513，零失败/跳过/不确定；XML1e745eceda4877552335dab1ada891bcf372edc8c93e6e6b1f89568df90ea507。仅新增7个跨消费端语法检查（6负1正），未修改游戏规则。下一步整批fresh审查与新Player构建。
