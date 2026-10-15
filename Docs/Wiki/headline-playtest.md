# 头条可玩演示接线

description: 已接入第15场景、显示模型、命令控制器及地图/手牌UI。先读[模块JSON](../Descriptors/headline-playtest.module.json)与[数据描述](../../Data/Descriptors/headline-scenario.data.json)。

WithHeadlineScenario在已配置的quick-start后追加新场景，克隆能力绑定和StartupChoices，保留原14场景引用。真实初始牌/部署数据不变，部署后进入双边头条收集。PublicPracticeView.HeadlineEnabled供后续UI选择正确提示；本身不控制规则。

流程：已有完整快速场景 → 校验独立fixture → 同目录载入头条/Defectors → 复制原能力并启用头条 → 验证CreateHost进入CollectingHeadline。无静默跳过或重新发牌。

Task1当时：17项新增RED→GREEN、2127托管通过；原生/构建/UI尚未执行。后续增量与当前原生证据见[计划](../Plans/2026-10-15-headline-playtest.md)，证据见[报告](../Testing/headline-playtest.validation.json)。

Task2：HeadlinePanelFrame只读取过滤视图和显式同局同版本观察数据；完成/取消来自队列Outcome。WaitingForOpponentFirst区分太空选择先后与无已实现候选，避免错误等待。DemoController提交/重发头条命令，切身份清显示，重置清收据，以generation阻止旧异步结果串局。18专项、2145托管通过；实际本地调用同步完成，不冒称未来网络延迟已测。

Task3 flow：默认头条场景 → 切本方/打开手牌 → 仅候选可选 → 返回地图确认锁定 → 换另一方选牌 → 自动公开按队列结算 → 若需国家选择复用事件面板 → 苏联首行动。无候选时明确显示覆盖边界，不自动跳过头条。次回合按钮按配置显示进入头条；旧14入口仍保留。Editor/播放器/构建均加载headline-scenario、defectors参数和schema三份新增共享载荷；生成副本不作为权威来源。

试玩方法：新窗口默认第15项；启动默认关闭双方观察，主动开启后才展示双方隐藏信息；上方“操作方”切换身份不会改变规则行动方。“测试观察”打开时可看双方牌；关闭后首张头条保持保密。先在手牌面板选牌，再在右侧确认；确认后不可改选。“重置当前场景”重新建立固定种子的测试局。只有已支持效果可用于头条，不等于完整游戏。构建和实际点击状态以报告为准。

最终源码审查的默认观察问题已通过真实视图RED→GREEN修复，2320项全套原生通过。TestObserverEnabled仅控制显示偏好；ReadHeadlineFrame与实际右侧面板共用路径，默认为正常过滤视图。主动开启或关闭观察不会改变局面版本或身份。构建/鼠标进度见验证报告，不继承旧播放器结论。

2026-10-16：新播放器已构建，42份载荷与源数据一致；实际鼠标完成“社会主义政府＋遏制政策”头条、地图分配、首个太空行动及重发无重复。新窗口的次回合全路线/叛逃者/终局画面仍待人工覆盖；当前以本批报告区分自动与人工证据。
