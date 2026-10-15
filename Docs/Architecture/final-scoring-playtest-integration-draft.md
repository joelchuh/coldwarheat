# 最终计分试玩接线草案

> description: 下批将已验证的FinalScoringService接入第10回合宿主、过滤投影和结算面板；本文件是设计，不是已实现接口。
> 状态：2026-10-16，待实施；基础验证见final-scoring-foundation。

## 用户体验

继续沿用单窗口双身份测试、可主动查看双方、世界/卡片/列表地图。普通回合末按钮仍只做军事和扣留审核；第10回合完成审核且未结束后，显示“最终计分”按钮。点击后展示六区双方档位/分项、地区净分、中国牌持有奖励、原VP和最终结果。欧洲控制用明确胜利原因；零分显示平局。换地图和画风只改变表现。

为了迅速验收新增独立“第10回合末计分练习”fixture，明确是人工测试局面，避免要求先玩完十回合才能看到效果。原15练习及Headlines入口保持。最终流程也能供真实连续回合调用，不把fixture藏进核心规则。

## 接口与边界建议（待实现）

- ActionPracticeDefinition增加可选FinalScoringDefinition，要求与Scoring引用相同并启用回合末；配置贯穿DemoCatalog拷贝，不丢失原能力。
- CardActionKind追加FinalScoring，不改变现有枚举值。CanCompleteFinalScoring验证日历最后回合为正式第10回合、全部行动与头条已完成、已审核且继续、无Resolving/事件/部署、China翻正、OPS已到期、军事已重置、无未实现太空额外行动/期末弃牌。
- 权威Plan从state同版本创建ScoringSnapshot，使用state.Cards.China，计算完整结果并交VictoryService；一次Commit保存VP、结果、审计报告和Ended。新ID再次请求被终局拒绝；相同ID走现有Host收据，不重复奖励；溢出/无处理器均不发布部分状态。
- 新PublicFinalScoring投影只含公开地区数字、ChinaOwner和胜利原因，不带完整Map/Rules/手牌/牌库/RNG。最终显示可由同一玩家快照驱动；不要把Core结果对象直接交UI。
- DemoController沿既有generation/固定身份/重发模式发命令。DrawFinalScoringReport展示分项；无需改IMapView。
- Infrastructure复用FinalScoringDefinitionLoader，Bootstrap与Editor Build同时补两份共享载荷。单独构建FinalScoringPlaytest，避免覆盖正在运行的头条播放器。

## 必须验证

核心资格：早于第10回合、未审核、待处理效果、特殊期末能力、已结束和定义错配都拒绝。宿主：固定身份、旧版本、重复ID同内容/异内容、公开投影无秘密、事务失败不写状态。结果：正/负/零、欧洲优先、先越20后反转、China只加一次；确认原turnEnd军事记录仍可读。兼容性：所有DemoCatalog装饰工厂保留配置和原场景。

完成原生回归后再构建新播放器，核对新增载荷、实际点击军事审核→最终计分→重发→切图/切语言，并检查分项在窗口内可读；截图不能代替完整交互证据。

这批接线仍不等于整局所有卡牌可玩：台湾决议等动态计分、太空期末能力、剩余事件与保存恢复继续分批推进。
