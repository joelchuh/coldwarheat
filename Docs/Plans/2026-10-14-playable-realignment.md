# 普通政局调整进入行动试玩

按用户持续开发/常规Git授权推进；在上一批审查/同步结束后从新main开功能分支。单一实现者、每批一次全分支独立审查。

目的：补齐普通OPS第三种地图行动，真实手牌→选调整用途→按当前公开局面选一国→双方骰与修正→扣一点预算→再次选择或结束。地图仍仅显示目标和提交意图，不掌握骰子或规则。

依据：2026-10-14再次打开GMT官方TS_Rules-2015.pdf，§2.1.5/6.0/6.2.1–3/8.1.5。无需己方影响力或投放可达性；目标必须有对手影响力；当前DEFCON地理限制生效。双方各掷d6，修正来自相邻受控国家数、当地影响力严格较多、己方超级大国邻接；高者移除低者影响力至差额/现有量，平局无变化，不增加影响力；每次后重选/可重复目标。规则NorthKorea例应在测试中复现。OPS修正只决定可掷次数，不叠到骰子；军事/DEFCON/VP/太空不变化。

规则数据：新增realignment.json及JSON摘要，严格profile/来源/数值加载；只存此规则的骰面、每掷费用与三类加值，不把它们散在UI/消费者常量中。地图/DEFCON/FlatOPS仍引用已有数据。无其他活动调整干预，未知效果不能默认放行。

纯规则：RealignmentDefinition；RealignmentModifiers（邻国ID明细、当地多数、超级大国邻接、合计）；RealignmentCalculation（双方骰、修正、总值、差额、移除方/量、前后状态）；RealignmentRules.CheckTarget/Modifiers/Calculate只读当前完整国家快照，长整数算总分/差额，合法目标先于骰子。返回US/USSR固定含义，不以发起方替换骰方。

实践：ActionPracticeDefinition可选RealignmentDefinition（Risk同地图必需）；追加CardActionKind.Realignment/RollRealignment/FinishRealignment，Stage.Realigning；不改变既有枚举值。开始时从实际actor统一QuoteCard(...Realignment)冻结预算、手牌→Resolving。每掷只接target，无客户card/骰子/预算，校验Actor/阶段/Mode/预算/DEFCON/目标。使用权威PCG局部副本，固定US再USSR读取；每次接受原子更新国家、SpentOps、RNG、版本和序列，拒绝不消费随机。保留Practice/Flat实例及所有风险历史。同一command由宿主收据重放骰子。完成明确弃置实体牌并根+1；余量可明确放弃，不能转投放/政变/太空，星号因只用OPS而弃置。

应用/表现：公开RealignmentResult含双方骰/三类修正/总数/移除结果，无随机种子/全状态。QueryRealignmentTargets需版本与固定身份；Controller.LegalTargets按Stage路由，现有IMapView自动显示高亮。选牌区三行两列增“政局调整”，开始后按钮“调整一次/结束调整（放弃余量）”，显示剩余次数与风险（双方都可能失去影响力）。独立RealignmentPlaytest，Editor/Player同数据装配；保留原快速入口/13练习。

- [x] T1 RED/GREEN：共享数据/来源与坏输入；官方北朝鲜例、两方对称、平局/全移/己方损失/无新增；动态邻控、多数不是控制、超级大国不重复计数；非邻接合法、无敌/DEFCON禁区非法、未知干预拒绝。
- [x] T2 RED/GREEN：真实牌开始/逐掷/结束；Flat增减只改变预算；两骰确定性/重放/溢出或拒绝无局部变更；每掷后合法性和修正重查；不能混用OPS；零预算/跨身份/旧版本/对手牌/未配置禁用；事件与军事/DEFCON/VP历史保持。
- [x] T3 RED/GREEN：公开DTO/host收据/controller/map路由和实际默认发牌；装配/数据载荷/独立构建；更新JSON/Wiki/worklog，实际渲染和真实鼠标分开报告。
- [ ] T4 全套验证后一次新审查；阻塞项一次RED/GREEN修复，Minors记录；Git常规同步核对远程。

不在本批接免费事件调整授权、取消/拦截类活动效果、地区奖励、头条/对手事件加OPS/回合末/联网。状态机仍为有限单回合练习。
