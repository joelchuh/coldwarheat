# 两张持续 Flat OPS 事件接线

目标：把Containment和Red Scare/Purge接到真实手牌，后续影响力预算、政变强度/军事信用及太空资格统一读取既有OpsGeneralRules报价。地区奖励、越南、中国牌、其他持续干预与完整回合末仍禁用。基础数值不重复写进UI或每个消费者。

对象：TurnOpsEffectDefinition（可信参数、affected-side策略、delta、期间）；TurnOpsEffectInstance（effect/card/resolution、controller/affectedSide、turn/activationSequence、expiryBoundary）；TurnOpsEffectState只保存当前权威回合的不可变实例/公共修正，不等同完整ActiveEffectService或存档。只装配两种已核验Flat效果，绑定CardCatalog；与其他事件族CardId互斥。

激活：Event校验持牌/Actor→按定义固定US或OpponentOfController绑定影响方→生成effect实例并与牌去向/根推进原子提交。星号Containment移出后效果仍生效；Purge弃置后效果仍生效。相同source/effect同请求幂等；不同请求相同source冲突；同回合对同方重复同效果为RequiresRuling，不能累加/覆盖。客户端不传规则定义或delta。

有效范围：只支持已知Flat OPS，在报价时消费；它们不改变直接事件影响力/VP/DEFCON/计分/骰子或头条印刷排序。对这些服务的NoActiveInterference/风险空干预列表意味着没有其职责内未处理的干预，不可省略未知活动效果后继续。实践不能产生其他活动类型。激活后的OPS报价对实际OPS Actor生效，汇总后一次上下限。当前单回合停止在行动末，效果保持到明确ExpireAndReset边界；不伪造到期或自动开新回合。未来正式到期/取消/保存另批接入。

状态传播：ActionPracticeState保存TurnOpsEffectState，所有Start/Next/Placing/ResolvingEvent/terminal复制明确保留。报价唯一入口供CanPlay太空、Influence预算、Space请求和Coup强度/军事使用。普通CardDefinition不改写。PublicPracticeView给公开活动效果；CardActionOption给当前有效OPS，UI显示印刷值与当前报价及影响方/到期说明。

- [x] T1 RED/GREEN：两牌参数/来源/持续期限严格子集加载；定义绑定；纯激活归属/幂等/冲突/重复待裁定；不可变实例/回合校验；星号不决定效果存在；修正同时正负先合并再限。
- [x] T2 RED/GREEN：真实Event→后续Influence/Coup/Space统一报价；OPS Actor与打印阵营/事件受益方区分；保持effect实例跨所有已有动作/选择与终局；失败/溢出不丢牌或留半激活；重复请求不二次激活；直接事件和计分不被OPS误修正。对满值/最低值/太空门槛/军事量作精确断言。
- [x] T3 RED/GREEN：目录装配/默认场景/Controller/公开DTO；Editor与播放器共用现有persistent参数载荷，无新增业务数据；独立构建，UI活动效果和当前OPS反馈。
- [x] T4 完整测试、构建载荷/实际初始渲染；JSON/Wiki/工作记录与一次最终review；阻塞项一次RED/GREEN修复，Minors记录；常规Git同步。

Review focus: 固定US与对手控制者归属；反向出牌/OPS Actor资格不混淆；所有状态复制保留活动实例；既有NoActiveInterference的职责范围和未知干预拒绝；先汇总再上下限且太空/政变/军事/影响力四处一致；星号移出不取消效果；重复未裁定不静默叠加；未接回合末不能擅自过期；公开结果不泄露手牌或内部随机；UI报价来自权威查询。
