# 连续回合试玩工作记录

基线2f55eb6afa440e57514a0311654f12873cc799dc本地/远程已核对。brainstorming设计与writing-plans完成，executing-plans单实现者执行，最终唯一独立审查。
Ruling: 继续单E盘分支 — 用户工程与聊天仓库不同且已干净同步 — 以测试/审查控制串改。
Ruling: 连续试玩跳头条 — 沿当前可玩原型边界 — 不视作正式整局或平衡测试。
Ruling: 特殊期末能力/最后回合明确停下 — 不实现未经接线的选择/最终计分 — 有限路线仍可能停止。
Ruling: 跨回合ID包含Turn — 不能让新行动撞旧收据 — 当前无公开存档，未来版本绑定另行实现。
Pre-flight: 补牌Plan私有且只由权威核心消费；所有状态和随机一次发布。发现既有Space/Coup slot按CompletedActions生成，T1同时统一回合前缀；Military不能用InitialEarned重开，沿Reset结果；T2不复制规则计算。

T1有效RED1975/1985：10新增预期失败；期末弃牌拒绝一例因stub整体拒绝先通过，后续以正向门禁与实现检查区分。逐条核对失败为缺失推进与额外行动审核门禁。现接原子下一回合与共享回合ID，准备GREEN。

T1首次GREEN1984/1985：同槽政变测试误把Fidel期望为1OPS；核对Data/Cards/deluxe-2015/cards.json printed_ops=2及无修正局面，生产结果2正确。仅改测试预期，补终局/残留Resolving/背面China/配置依赖门禁，重跑。

T1 GREEN1988/1988。T2有效RED1988/1991，3新增装配/Controller/公开资格用例预期失败。接NextTurn配置、固定身份查询、按钮、回合号与独立构建，准备GREEN。

T2 GREEN1991/1991，metadata133/133描述、72数据集、289符号、623依赖、270文件通过。冻结全部C#哈希后开始独立ContinuousPlaytest构建。

构建成功97602694字节，250份C#冻结哈希一致，38份构建载荷逐项与共享源比对一致。实际鼠标路线及3张F12游戏截图见validation；进入T2时v27/seq45→v28/seq46，重发不变；新抽中东计分后v29/seq47。伊朗US2/USSR0、VP-2、太空US0/USSR1跨回合保留，随后计分VP+4到2。Player.log无Exception/Error/Assertion。用户额外切身份期间停止点击，获准后重新观察才完成末次动作。

历史文档Minor关闭：索引中国牌回合末翻面待接措辞同步实际已实现状态。未改历史验证报告。
