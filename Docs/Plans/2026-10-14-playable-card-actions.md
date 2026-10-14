# 真实出牌行动试玩实施计划

> 使用 executing-plans 在当前会话实施；一次最终独立审查。

Goal: 将已测试的基础规则接成用户可操作的 Windows Demo。
Architecture: 可选 ActionPracticeState 附着 GameState；纯规则计划生成完整下一状态；固定身份 LocalGameHost 提交，UI 只提交牌 ID 和使用方式。地图继续沿用 IMapView。
Spec: Docs/Wiki/prototype-roadmap.md、first-demo.md、operations.md、space-race.md（以现有真实符号和规则为准）。

范围：部署完成后显式选择跳过头条进入单回合行动试玩。苏联先行，使用日历普通行动数；到本批边界停止，不声称完整回合或一局。真实物理牌、RNG、太空、VP、行动序号一次提交。普通影响力仅本方/中立事件牌；对手事件、计分、中国牌等尚未编排的路径明确拒绝；太空允许普通非计分牌且不触发事件。无活动干预的显式练习，不接受客户端提供 OPS。

## 全局约束
- Unity 6000.6.0f1；Core/Application 无 UnityEngine；固定身份、版本、命令收据与私有手牌投影。
- 状态不可变；牌从手牌移至结算区再弃牌，太空原子弃牌。影响力可逐点消费，结束可放弃剩余 OPS。
- 数据使用现有正式目录、OPS 和太空 JSON；不复制数值到 UI。两张手牌观察权限仍只读。
- 旧诊断场景与部署路径不变。新模式显式启动，不暗中代替头条。

## Review focus
错误方/旧版本/同 ID 改载荷不得消耗牌；RNG/牌/VP/轨道原子一致；手牌与 RNG 不因新增投影泄露；普通影响力保留行动开始时邻接；切视图/身份清除无效本地选择但不重置权威预算。

## Tasks
- [x] T1: ActionPracticeState/Rules + GameState 可选组合，行为测试 RED→GREEN。验证开始门禁、牌所有权、计分/对手事件拒绝、太空失败也弃牌、跨命令状态保留、行动上限。
- [x] T2: PlayCardCommand、固定会话查询/提交、公共轨道投影；RED→GREEN 验证重试/冲突/秘密过滤/影响力预算与卡牌生命周期。
- [x] T3: 给真实发牌场景配置规则；接手牌选牌、用途按钮、行动与结果提示；控制器/载荷验证；构建及真实帧检查。
- [ ] T4: JSON descriptions/Wiki/worklog/真实验证报告；一次最终独立审查；修复重要问题后常规提交推送。

后续批次：政变行动接入、可执行事件和计分、头条与完整回合。按额度和可验收 Demo 分批推进。
