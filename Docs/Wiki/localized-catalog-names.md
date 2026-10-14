# 中英文短名称

> description: 查中文短名数据、稳定ID字典加载与地图/手牌显示切换；不改变规则状态。
> 状态：验证中；实际证据见报告，不冒称完整本地化。

中文数据Data/Localization/zh-CN.catalog-names.json与英文包共有198键：84国、103牌、9地区、2超级大国。都是短名称，中文为项目自译；rules_text_key正文尚未提供，中文事件名不代表事件已经实现。

流程：Data目录description→中文数据描述→CatalogNamesLoader.Load按所需稳定ID投影只读名称→DemoView/HandPanelFrame/MapPresenter；按钮只交换显示字典并更新地图context，旧点击失效。界面仍中文，仅国家/卡牌名称可切中英文。世界地图缩放和平移保留；游戏ID、版本、当前身份、选牌和选择草稿不变。

无牌局写入、无随机推进、无网络。缺少标题/重复JSON键/坏元数据或未知语言拒绝加载，禁止静默显示缺失译名。额外的合法静态名称仅可显示，不创建规则对象。中国牌已有独立中文按钮，不参与普通手牌数量。

入口与测试按模块JSON定位。新的LocalizedPlaytest独立构建，ContinuousPlaytest已打开局面不被更新或覆盖。
