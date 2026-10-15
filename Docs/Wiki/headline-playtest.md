# 头条可玩演示接线

description: 当前仅第15场景工厂和公开启用标记已实现，显示/控制器/UI尚待接线。先读[模块JSON](../Descriptors/headline-playtest.module.json)与[数据描述](../../Data/Descriptors/headline-scenario.data.json)。

WithHeadlineScenario在已配置的quick-start后追加新场景，克隆能力绑定和StartupChoices，保留原14场景引用。真实初始牌/部署数据不变，部署后进入双边头条收集。PublicPracticeView.HeadlineEnabled供后续UI选择正确提示；本身不控制规则。

流程：已有完整快速场景 → 校验独立fixture → 同目录载入头条/Defectors → 复制原能力并启用头条 → 验证CreateHost进入CollectingHeadline。无静默跳过或重新发牌。

17项新增RED→GREEN，全部可用托管2127通过；原生/构建/UI未执行。后续模型/控制器和地图按钮见[计划](../Plans/2026-10-15-headline-playtest.md)，证据见[报告](../Testing/headline-playtest.validation.json)。
