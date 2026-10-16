# 播放器载荷核验工作记录

> description: 记录单源构建清单、只读脚本及真实验证，不混同游戏规则测试。

BASE ad696b09913b6fa5223a678a2fa00398c0a8ec00；分支feat/player-payload-verification。沿用用户项目E:/TwilightStruggle，保留两项未跟踪用户文件。brainstorming/writing-plans/executing-plans inline；用户已明确自主决定无需询问。

Ruling: 工具从C#字面Copy列表迁移到单源JSON，而非长期正则解析源码——构建和验收共享同一映射，减少漏加与假成功；代价是新增一个受限C#加载/文件复制组件，需要原生回归。
Ruling: 工程策略与映射分别保存；前者含exe/目录/程序集/工程限额，后者只列文件对应关系。不复制游戏参数；不引入数据库或泛化脚本平台。
重复流程证据：Docs/Worklogs/2026-10-17-early-war-play.md、2026-10-17-space-event-play.md、2026-10-17-olympic-play.md（前两项文件名将在登记前按实际路径核对）；同一批重建不累计复用次数。
