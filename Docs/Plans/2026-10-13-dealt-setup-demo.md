# 真实手牌接入开局预览计划

> 沿用户单窗口/双方可观察、模块化地图和持续开发授权继续；inline实施，一次独立审阅。基于CardZones与InitialDealService；完成后仍是可发牌/部署的规则Demo，不宣称完整对局。

## 设计

GameState追加可选CardZones与RandomSnapshot，两者必须一起存在；与旧fixture字符串手牌互斥。保留GetHand兼容接口：真实牌区时从CardId派生字符串ID，旧练习仍使用显式示例标签。所有Apply/WithInfluenceEvent/WithDeployment复制都保持同一不可变牌区与随机快照，不能在部署中丢牌或重新洗牌。没有公开让客户端替换权威牌区的方法。

PlayerViewSnapshot增加只含公共数量/中国牌持有面/EnteredEras的PublicCardSummary；本人OwnHand仍由身份投影，不能包含对方牌ID/牌库顺序/随机状态。LocalTestSnapshot保留显式开关，只额外公开双方手牌；不提供可写GameState或随机源。CardCatalog与名称是可公开的静态数据，由Bootstrap单独注入UI，不从隐藏状态获取整副牌。

新增第13个显式fixture场景“发牌与初始部署”。读取正式印刷目录/schema/profile、setup/turn-flow及确定的PCG随机快照，使用InitialDealService产出的不可变结果创建权威GameState，然后走已有InitialDeploymentFlow。新场景无伪造手牌字段。fixture配置记录可复现的完整初始RandomSnapshot；重置同一练习创建新GameId但恢复同一初始局面，不做平衡重抽。此前12练习保留原行为。

启动默认选新场景（按稳定ID查找；老目录没有它时仍回到首项）；显式--demo-scenario选择继续有效。世界地图仍沿现有默认；新场景初始选波兰，可使用原有定位工具。若命令行指定未知ID，仍拒绝，不静默回退。

真实手牌界面通过独立DemoView.Cards渲染只读面板：常驻摘要显示牌库/预备数量、中国牌持有面和本人/对方手牌数，按钮打开手牌面板；普通模式只渲染当前身份，显式观察模式可看双方。静态牌名使用现有103个英文目录名称，占位卡面显示OPS、阵营、计分牌标记；不显示复杂未接入效果全文，不提供出牌按钮。面板打开期间屏蔽底层地图/部署控件输入，关闭不改变局面；换局/切换身份重读当前投影，避免缓存泄露。

Editor/Player加载同一审核数据，构建增加card schema/profile和新fixture载荷；现有cards/turn-flow/setup/names不另复制新事实。随构建附上PCG Apache-2.0许可与来源说明（binary分发所需）。不改原已运行PreviewDemo，使用FirstDemo输出检查。

## Task 1：权威状态与玩家投影

- [x] RED测试CardZones/Random成对、与旧hands互斥；影响力/事件/部署复制保留；玩家视图只含本人ID和公共计数，观察入口可禁用；通过序列化核查对方牌/随机/牌库顺序不泄漏。
- [x] 实现GameState与PublicCardSummary，保留旧测试兼容；真实Unity回归。

## Task 2：可复現开局fixture

- [x] 新数据/description，加载器读取受支持随机状态、正式目录与初始定义；保存不可变InitialDealResult，创建Host时开始部署。
- [x] RED测试实际13场景、新GameId重置但相同手牌、不同身份/非法部署/重试后不重发、坏profile/fixture/随机状态拒绝；默认与显式启动选择测试。
- [x] 接入Editor及Player Bootstrap；回归。

## Task 3：手牌面板与实际构建

- [x] 卡面显示模型只依赖当前投影和静态目录，验证切换身份/观察模式下的内容集合；面板关闭、换局和地图切换不修改权威手牌。
- [x] UI实现只读面板及当前阶段提示；替换地图接口和部署流程保持。
- [x] 构建FirstDemo并核对所有运行载荷与第三方许可；用游戏自身ScreenCapture分别检查开局画面与手牌面板。截图仅证明渲染，不冒充原生鼠标操作验收。
- [x] 模块/数据JSON、Wiki/索引、工作记录、测试与构建证据同步，独立审阅后Git提交并核验远端。

## Review Focus

1. GameState所有复制路径都保留牌区和随机状态，旧fixture与真实牌区不会同时成为权威。
2. 正常视图和合法查询不泄漏对方手牌、完整牌区、RNG状态；观察入口权限边界不被UI开关替代。
3. 重置新局与重复旧命令是两条流程；部署不能重洗牌或占用普通出牌。
4. 面板仅显示当前投影，切身份/场景无旧手牌残留，覆盖层阻断底层点击。
5. schema/profile/随机载荷和许可真正进入播放器；源码/测试通过不替代构建证据。

基线456220504ebf51e49e67a3f7a4da7953985cdd7c；实现前729项回归。


实现/测试/审阅完成；本批常规提交与推送继续按已授权Git流程执行，远端结果另核验。
