# 头条可玩演示接线

description: 已实现第15场景、显示模型和命令控制器；UI尚待接线。先读[模块JSON](../Descriptors/headline-playtest.module.json)与[数据描述](../../Data/Descriptors/headline-scenario.data.json)。

WithHeadlineScenario在已配置的quick-start后追加新场景，克隆能力绑定和StartupChoices，保留原14场景引用。真实初始牌/部署数据不变，部署后进入双边头条收集。PublicPracticeView.HeadlineEnabled供后续UI选择正确提示；本身不控制规则。

流程：已有完整快速场景 → 校验独立fixture → 同目录载入头条/Defectors → 复制原能力并启用头条 → 验证CreateHost进入CollectingHeadline。无静默跳过或重新发牌。

17项新增RED→GREEN，全部可用托管2127通过；原生/构建/UI未执行。后续模型/控制器和地图按钮见[计划](../Plans/2026-10-15-headline-playtest.md)，证据见[报告](../Testing/headline-playtest.validation.json)。

Task2：HeadlinePanelFrame只读取过滤视图和显式同局同版本观察数据；完成/取消来自队列Outcome。WaitingForOpponentFirst区分太空选择先后与无已实现候选，避免错误等待。DemoController提交/重发头条命令，切身份清显示，重置清收据，以generation阻止旧异步结果串局。18专项、2145托管通过；实际本地调用同步完成，不冒称未来网络延迟已测。
