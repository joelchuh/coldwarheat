# 动态计分效果：台湾决议与穿梭外交

description：查询计分专用战场身份、US選穿梭排除国、敌邻接/存在/控制、一次消费与中国实际使用取消。

状态2026-10-06：#35/#73参数Bound；37 Bound / 65 Unresolved，102处理器planned，未执行C#/Unity。[JSON描述](../../Data/Descriptors/scoring-effects-batch1.data.json)→[参数](../../Data/Cards/deluxe-2015/scoring-effects-batch1.parameters.json)、[Schema](../Contracts/scoring-effects-batch1.schema.json)、[案例](../Design/scoring-effects-batch1.cases.json)、[报告](../Design/scoring-effects-batch1.validation.json)；[模块](../Descriptors/scoring-effects.module.json)。

## 台湾决议：只影响亚洲计分

成功激活后持续到取消。亚洲计分牌实际计分或最终亚洲计分时，若US此刻控制台湾，该国在本次不可变ScoringFacts由非战场变战场：US总控制数不变，US战场+1/非战场−1，亚洲战场全集+1。不直接奖励VP，档位/战场奖励由既有RegionScoring统一算。US不控（包括USSR控）时台湾仍基础非战场。

不改变CountryDefinition.base_battleground、真实控制、影响力、政变DEFCON、战争修正、Summit/Kitchen等其他事件或东南亚专用计分。若US仅台湾与若干战场、没有其他非战场，不能把台湾同时当非战场满足Domination。所有基数从正式地图派生，不另存亚洲6/7常量。

US实际合法打出中国牌后取消，普通OPS或太空都适用FAQ #6；USSR使用、事件转交、显示/查询、非法命令不取消。只有权威已接受的实际CardUse能生成取消计划，继承正确CardPlayer，不能以RootPhasingPlayer/China持有者替代。取消不逆转以前VP，不产生永久禁止记录；未知来源/真实使用尚未成功时停止，不能靠UI按钮取消。

## 穿梭外交：下一次匹配实际计分

成功事件后实体进入公开ActiveEventHolding，不在Hand/Draw/Discard/Removed；活动实例引用实体和ActivationResolutionId。不是把牌复制为持续图标。下一张实际亚洲或中东计分牌执行时使用；欧洲等其他地区、东南亚、Summit、Kitchen、最终计分都不触发或消费。跨回合继续有效。

计分前生成US选择决定：本次地区USSR控制的计分用战场国中选一个。事实先应用台湾重分类，因此被US控台湾不会成为USSR候选。US控制决定，但不另开US行动。排除该国只作用于USSR计分事实：总控制国−1、战场控制−1，若邻接敌方US再减对应邻接计数；非战场不变，实际影响力/控制不动。FAQ北韩唯一例因此USSR无Presence；日本例由US選Japan便同时消去其US邻接奖励。

**战场全集分母保留。** 不把该国从地区Control所需战场全集删除；否则USSR原本控制全部战场会在减一后仍达Control，违背FAQ明确的阻止Control。台湾重分类增加的全集仍保留。US计分事实不变，双方合算一个VP批次。

没有USSR战场时无决定、排除零国，仍在此次匹配计分完成后消费并弃置；这是next scoring字面推导SE1，不称专门FAQ。计分预览只作纯投影：若需选国提供公开候选或指定假设分支，双方都能查，不把预览当US正式回应，也不擅定“最佳”国；不产生真实决定、VP、实体或消费变化。正式选择旧Revision/越权/非法目标一律零变化，合法候选重查当前快照。

## 提交、恢复与牌区

流程：校验真实ScoringInvocation/来源与完整活动状态 → 当前控制/台湾重分类 → 穿梭候选/US决定（必要时等待） → 完整双方分项 → 一个VP批次及Holding→Discard/消费收据同一事务 → 胜负服务。终局在成功计分批次后检查；消费不能因获胜后流程截断遗留在可再次用状态。技术失败/非法决定不提前消费。

保存ScoringId、StateVersion、EffectInstanceId、选择Revision、SelectedCountry与消费收据；读档可恢复等待，重复命令返回原结果而不第二次扣国/移动牌。SALT仅公共Discard候选，不能取回Holding中的穿梭；待全部成功消费后该实体才可正常再洗入/取回。不同Resolution重复激活仍RequiresRuling，不擅自叠加多个排除。

来源：英文p6 #35/p11 #73；FAQv5 PDF p4 #6、p8 #35、p15 #73；R2015控制/持续与地区/最终计分。台湾动态分母、穿梭排除事实/保留Control分母与FAQ一致；状态模型、零候选、事务/收据和预览是SE1工程或卡文推导。真实计分、China使用、生命周期、牌区/持久化实现仍待运行；画风仅渲染当前事实与效果标记。

下一批优先绑定七张计分牌至既有地区算法，避免以通用服务设计冒称牌处理器已完成；再安排共享VP/太空事件及剩余卡牌。

主离线验证22数据＋50固定参考＝72新项，1057旧回归，共1129通过；22 Schema/26数据描述/19模块/69指针，11runtime未运行。独立只读审阅无未关闭Critical/Important/Minor，独立50参考投影与20计分输出重算、141 JSON/69指针/19模块无环/161链接/10摘要核对；未独立运行22数据/Schema元校验/1057旧回归。
