# 头条可玩演示接线

description: 已接入第15场景、显示模型、命令控制器及地图/手牌UI。先读[模块JSON](../Descriptors/headline-playtest.module.json)与[数据描述](../../Data/Descriptors/headline-scenario.data.json)。

WithHeadlineScenario在已配置的quick-start后追加新场景，克隆能力绑定和StartupChoices，保留原14场景引用。真实初始牌/部署数据不变，部署后进入双边头条收集。PublicPracticeView.HeadlineEnabled供后续UI选择正确提示；本身不控制规则。

流程：已有完整快速场景 → 校验独立fixture → 同目录载入头条/Defectors → 复制原能力并启用头条 → 验证CreateHost进入CollectingHeadline。无静默跳过或重新发牌。

17项新增RED→GREEN，全部可用托管2127通过；原生/构建/UI未执行。后续模型/控制器和地图按钮见[计划](../Plans/2026-10-15-headline-playtest.md)，证据见[报告](../Testing/headline-playtest.validation.json)。

Task2：HeadlinePanelFrame只读取过滤视图和显式同局同版本观察数据；完成/取消来自队列Outcome。WaitingForOpponentFirst区分太空选择先后与无已实现候选，避免错误等待。DemoController提交/重发头条命令，切身份清显示，重置清收据，以generation阻止旧异步结果串局。18专项、2145托管通过；实际本地调用同步完成，不冒称未来网络延迟已测。

Task3 flow：默认头条场景 → 切本方/打开手牌 → 仅候选可选 → 返回地图确认锁定 → 换另一方选牌 → 自动公开按队列结算 → 若需国家选择复用事件面板 → 苏联首行动。无候选时明确显示覆盖边界，不自动跳过头条。次回合按钮按配置显示进入头条；旧14入口仍保留。Editor/播放器/构建均加载headline-scenario、defectors参数和schema三份新增共享载荷；生成副本不作为权威来源。

试玩方法：新窗口默认第15项；上方“操作方”切换身份不会改变规则行动方。“测试观察”打开时可看双方牌；关闭后首张头条保持保密。先在手牌面板选牌，再在右侧确认；确认后不可改选。“重置当前场景”重新建立固定种子的测试局。只有已支持效果可用于头条，不等于完整游戏。构建和实际点击状态以报告为准。
