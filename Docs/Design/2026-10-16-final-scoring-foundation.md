# 最终计分基础设计

> description: 同一快照计算六区与中国牌奖励，一次交给胜负服务；不承担第10回合宿主资格或动态效果。

用户授权持续自主开发。目标是支持最终可玩整局的确定性核心，沿用纯 C#、共享 JSON、可替换地图界面。当前批次完成可测试的计分与判胜接缝，后续接入真实第10回合命令、事务与结算画面。

## 选择与接口

选择内部计算全部地区，而不是接受调用方拼接的地区结果列表：同一快照、完整性和来源一致性由接口保证。也不逐个调用普通 VP 胜负检查点；否则中途越过20分会提前结束。

- FinalScoringDefinition(regionDefinition, finalRegionIds, chinaCardId, chinaHolderVp)：复制最终地区列表，要求恰为基础规则的六个标准地区，无遗漏/重复/未知/SEA；中国牌ID必须为 card.china，奖励非负Int32。
- FinalScoringService.CalculateUnmodified(definition, snapshot, china)：同一地图与不可变快照计算所有地区；只读结果保留各区结果、SnapshotId/StateVersion、中国牌持有方与带符号奖励。拒绝任何尚无处理器的活动计分效果。
- FinalScoringResult：内部构造，不允许外界提供部分/跨快照地区结果。欧洲Control有独立赢家和空NetVpForUS，不能伪装为数值；其他情况用long累计，再检查总净值Int32。
- FinalScoringCompletedEvidence + VictoryCheckpoint.FinalScoringCompleted：VictoryService先检查批次前尚未达普通终局线；欧洲优先，其余一次合并原VP与最终净值；正为美国、负为苏联、零为Finished且Winner=null。欧洲胜利保留原VP，与已有欧洲计分语义一致。
- FinalScoringDefinitionLoader.LoadSubset(regionJson, regionSchema, chinaJson, chinaSchema, map, cards)：复用地区加载器；新增卡牌参数schema，验证来源ID/URL、版本、单一中国牌、目录种类、应用时机与整数范围。奖励只读取现有共享数据，不写死1。

## 不变量与范围

Core不依赖Unity/文件/随机数；地图标签、位置、画风不参与规则。所有输入不变。地区顺序不影响总分；东南亚已含在亚洲，不单独再计。牌面朝向不影响持有奖励，计算不翻牌/移牌。

宿主未来必须保证第10回合、回合末能力与军事/扣留计分牌审核已完成、没有其他终局，地图与China来自同一状态版本，并在原子命令中提交一次。纯计算接口不承诺权限、幂等、终局吸收或事务。当前拒绝所有活动计分修正，后续按已设计规则加入台湾决议等；不悄悄忽略。基础规则总有中国牌持有者，不支持内战变体。

## 验收

完整六区；中国牌双方与正反面；SEA不重复；总分正负零与越线后反转；欧洲优先；来源一致与不可变；空/重复/缺失/未知/SEA列表；跨地图/活动效果/无中国牌；大数可抵销及真实溢出；严格JSON结构、来源、版本、目录引用与参数读取。原有2320项应保持通过。

## 来源

2026-10-16核对 GMT R2015 §10.3.2（PDF p10–11）：最终全部地区后判胜，SEA不单算，20分不提前结束，欧洲控制优先，零分平局。C-DELUXE-EN #6（PDF p2英文卡图）：第10回合末持有中国牌奖励。这里只记录摘要，不复制原文或卡图。

- https://www.gmtgames.com/nnts/TS_Rules-2015.pdf
- https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf
