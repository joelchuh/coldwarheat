# 九练习Demo与数量草稿

2026-10-13；基于034dacf，feature/allocation-demo。用户持续优化与Git同步授权有效，沿用已确认UI/规则分离和EI2-RULING-01。

新增社会主义政府、苏伊士危机人工场景。数量字典只存本地意图，经查询容量/总额限制编辑；确认送同一数量会话。地图仅定位、切视图保留草稿；身份/场景/权威版本变化清空。旧国家集合与OPS行为回归保留。

新增CardEffectReferenceIndex，只做固定版本的稳定引用核验；正式卡牌102个处理器仍未装配。Fixture只存初始局面，额度读取唯一参数。Editor/播放器同步由9份扩展为13份。

## 实际验证

- Catalog RED 279/256通过/23失败，GREEN 279全过；Draft RED 294/280/14，第一次GREEN 293/294。唯一失败来自测试尚未绑定决策就测本地Revision；先查询再测量后修正测试前置条件，未改生产规则迎合断言。
- Payload RED 295/294/1，缺少运行时数据；装配完成后295/295通过，零跳过。XML SHA256：493731d6a13c63f5919b9ea1beeaa0e3fc96eb28bdbb49179856226aa53b2c0d。
- Unity 6000.6.0f1 Windows构建exit0；allocation-demo-build.log含FIRST_DEMO_BUILD_SUCCEEDED 97024231。13份源文件与播放器StreamingAssets逐字节一致，逐文件摘要见[验证JSON](../Testing/allocation-demo.validation.json)。
- 游戏自身导出苏伊士危机Ready/table 1280×820帧，已检查文字、九场景标识、列表和布局，图保存在Docs/Images/allocation-demo.png。未声称此帧验证了等待阶段±按钮或鼠标操作。
- 新构建使用当时未运行的FirstDemo目录，用户原PreviewDemo窗口保留。窗口工具激活失败后已停止鼠标自动化并告知用户恢复输入。
- 独立只读代码审阅：Critical/Important/Minor均无；审阅者直接确认295 XML与diff检查，另独立核对6份XML摘要、13份构建数据、构建exit0、截图与最终文档；不把未执行的鼠标验收算通过。

模块/数据描述、目录摘要、Wiki与计划同步维护；复用现有Unity验证工作流，未新增通用脚本。正式出牌、牌区、持续效果、回合和东欧动荡仍待实现。下一批优先建立权威回合上下文，避免把印刷时代当对局时代；再逐步接正式出牌。

Git按已授权流程审阅、提交、快进main并push；是否同步以实际本地/远程SHA为准，本日志不预写未发生的push成功。
