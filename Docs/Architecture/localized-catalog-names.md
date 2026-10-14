# 中英文短名称切换

用户目标：先做方便中文单人测试的Demo，保持稳定ID/规则与可替换地图分离。延续持续开发与常规Git授权，不重复请求编码/合并批准。
范围：198键中文短名（84国家、103牌、9地区、2超级大国），项目自译非官方规则正文；英文保留。新增严格CatalogNamesLoader将.name键投影为已请求的稳定ID字典。Bootstrap读取两包，默认中文，右上按钮在中英文名称间切换；其余UI仍中文。只换表现，不建新局、不改Actor/选择/命令和版本。
接口：Load(json,expectedLocale,requiredIds)支持en-US/zh-CN，schema1/content0.1.0/dataset精确匹配；缺少请求ID、空白/过长标题、重复JSON属性、非法name key/类型拒绝，输出只读副本；额外合法显示键不授予规则实体。MapPresenter.ReplaceNames完整验证当前地图名称，再刷新新显示context以使旧点击失效。DemoView.SwitchNameLanguage内部交换，保存世界地图Center/Zoom并按最后真实绘制区域恢复；不重建controller，不清选牌/草稿/打开手牌状态。保留老Initialize调用可选参数兼容。
不做：规则全文翻译、逐卡说明、完整界面语言切换、字体/画风更换、术语定稿和新事件。中国牌固定按钮现有中文维持，公开OPS效果中若有英文事件名通过同一Names查询更新。
验收：坏包门禁；真实两包198键一致且187运行名称完整；手牌内容/数值/隐私视图与版本不变；MapPresenter旧context输入拒绝/新context可选，坏名字不污染已显示地图；真实按钮切换并截图。独立LocalizedPlaytest构建，保留用户在玩的ContinuousPlaytest。
