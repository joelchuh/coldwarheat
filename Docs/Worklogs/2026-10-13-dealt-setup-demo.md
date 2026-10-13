# 真实手牌开局预览实现记录

基线4562205，feature/dealt-setup-demo；用户持续编码、单窗口双方观察与Git授权。按同名计划inline实现，一次独立审阅。

## Task 1：状态与投影

GameState成对持有CardZones/RandomState，与旧fixture手牌互斥；GetHand保留字符串ID兼容投影，来源从真实牌区派生。三个状态复制入口保留牌区/随机快照。PublicCardSummary只含公共数量、已进入时代和独立中国牌；正常OwnHand仍按身份选择，观察开关仍在Host执行权限检查。

dealt-state-red：737项，729通过/8失败（牌区丢失、手牌空、缺少成对守卫）；dealt-state-green：737/737零跳过，XML SHA8067170d1e0cc3c794dbe8c4db4edf4ffae5d0b1b70c80dab72c20b92b9226eb。包括部署、事件、普通影响力复制、私有牌序列化不泄漏与禁用观察入口。

## Task 2：完成

新增显式initial-deal-scenario fixture，完整PCG初始状态对应seed42/stream54；选择波兰，手牌由正式目录发牌服务生成。新测试首轮dealt-demo-catalog-red无XML：测试误用了不存在的DemoController.SubmitDeploymentAsync；编译器CS1061已定位，改为实际ConfirmDeploymentAsync后重跑RED，不能把编译失败算作行为失败测试。

Ruling: Controller仅缓存最后一次部署命令及其固定身份。测试应切回该身份重发，不应要求它重发更早的另一方命令；更早历史收据由会话API承担且已有测试。保留控制器当前边界，未为新测试改变生产语义。

以下记录后续实际结果。

Task 2数据/宿主部分：dealt-demo-catalog-red2共756项，737通过/19失败；green为756/756，XML SHA d76c3d2bb2de2151cc1b8334c9d0430927f331b3f6181d46f3a67746be809e33。独立旧12局面保留；新练习持有不可变InitialDealResult，CreateHost先建立真实GameState再部署，默认启动优先稳定ID。随后已接Editor/Player与面板，见Task3。

## Task 3：完成

独立HandPanelFrame只接受当前玩家投影、静态目录以及可选同GameId/Version的观察快照；正常模式一行本人，显式观察两行当前方优先。面板显示真实牌名/OPS/阵营/计分，不可出牌；开启时停止绘制底层地图和部署控件。新CLI参数只控制打开面板。标题由实际渲染发现Socialist Governments截断后按高度收缩字体，重新构建并复核两张画面。

hand-panel-red 762项中6失败；green与接线后的ui-green及title-green均762/762，零跳过。最后XML SHA247b27f1650b9a5543a7417cd529828b9f7a83fb7fcdcb00bd6c0e6325bd6e4f。两次Windows构建均exit0；最终dealt-setup-demo-title-build.log marker FIRST_DEMO_BUILD_SUCCEEDED 97297142。25份源/播放器载荷逐字节及SHA一致，含23 JSON和PCG许可/说明。

游戏自身ScreenCapture分两次检查1280x820地图与双方8+8手牌，均exit0；最终画面存Docs/Images/dealt-setup-demo.png与dealt-setup-hands.png，源路径由实际日志取得。地图仍可替换，占位卡面不承诺最终美术。未操作原生鼠标/拖拽；未关闭旧PreviewDemo。

JSON摘要、Wiki、索引与README已同步；元数据检查108描述、34模块、68数据、6脚本，入口路径/符号及目录description一致。未新增正式工作脚本。详细原始运行和摘要见Docs/Testing/dealt-setup-demo.validation.json。

## 整批审阅与收尾

review_dealt_setup_demo确认无Critical/Important。三项Minor已修：部署结束文案区分真实发牌与旧标签、本方/对方手牌数同屏显示、索引与牌区摘要去掉过期状态。显示文案/计数修正后再次Windows构建exit0并重新导出两帧检查；没有将此构建冒称新一轮762测试，最新全量测试发生在这些显示修正之前。数据载荷再核对25/25一致。完整对局与原生鼠标仍待实现/验收。

本批按已授权流程审阅后提交到feature/dealt-setup-demo、快进main并推送；具体HEAD与远端相等结果由实际Git命令另行核验。下一步在新分支实施已设计的DEFCON基础子集。
