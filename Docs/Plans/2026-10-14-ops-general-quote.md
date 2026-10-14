# 通用 OPS 报价与太空请求衔接

Goal: 实现通用Flat修正的纯值计算，复用已有共享ops_policy；支持真实牌和经验证的视同牌OPS，按实际Actor归属，所有修正合并后一次处理上下限。接一个太空请求工厂，避免调用者重复拼接Actor/Card/有效OPS。
Spec: Docs/Wiki/persistent-effects.md、space-race.md；Data/Cards/deluxe-2015/persistent-effects-batch1.parameters.json#/ops_policy；Docs/Contracts/persistent-effects-batch1.schema.json。
Execution: executing-plans inline，两项任务+一次独立审阅。用户持续优化/Git授权有效，剩余约1%时兑换一张已有额度卡。

## 边界与接口

只实现General（无地区奖励）结果。所有地区奖励及花费历史、持续实例激活/到期/取消、同效果重复激活、完整CardPlay/OPS预算仍待实现。已有PE-INTERPRETATION-01的小值地区公式已向用户发问，未答前不将其当采纳规则；本批不依赖它。

Core/Rules/OpsGeneralRules.cs：OpsValuePurpose五种Influence/Coup/Realignment/Space/OpsThreshold；OpsGeneralDefinition(minimum,ordinaryMaximum,flatPurposes)非空唯一有界，输出只读用途集合。OpsFlatModifier(Id,AffectedSide,Delta)为可信来源条目，不代表已验证卡牌激活；ID去重，状态/事件来源由未来ActiveEffect层验证。最多128条是工程上限。

QuoteCard(definition,card,actor,purpose,modifiers)读取实际CardDefinition.PrintedOps，计分牌拒绝。QuoteAsIfCard(definition,sourceRuleId,verifiedBaseOps,actor,purpose,modifiers)只接受普通地图行动三用途，不允许Space/OpsThreshold伪造物理卡。BaseOps正整数；合成定义允许不同上限验证数据驱动。作用方按Actor筛选，不随卡牌原持有者转移。

计算Sum为作用于Actor的Flat delta总和，使用long；Raw=Base+Sum；GeneralValue=max(minimum,min(ordinaryMaximum,Raw))。不可边加边截断。OpsGeneralQuote保存定义、实际Card或AsIf来源、Actor/Purpose/Base/总修正/Raw/结果、只读已应用修正。其结果对Influence/Coup/Realignment仅是通用部分，不能在地区奖励尚未处理时当完整可花预算；Space/OpsThreshold不加地区奖励。

Infrastructure/DataLoading/OpsGeneralDefinitionLoader.LoadSubset(json,schema)：复用可信现有完整Schema，锁定schema1/content0.1.0/ruleset及dataset/scope；仅装配ops_policy的上下限和完整Flat用途，核对已支持aggregation、已知来源/Verified引用和数值类型，不能把schema通过当完整效果语义已验证。读取地区政策身份不执行地区奖励。数据业务值不变，无第二张常量表。

SpaceAttemptRequest.FromQuote(id,sourceActionId,turnNumber,quote)：必须真实Card且Purpose=Space，从不可变quote取Card/Actor/GeneralValue。原构造器仍为可信底层兼容入口，不声称这个工厂能替代持牌/阶段/限制校验。没有随机或游戏状态写入。

## Task 1：通用报价

先桩与RED，再实现。测试真实牌/中国牌/视同牌、Actor归属、合并后封顶/保底与顺序无关、所有用途/受支持用途子集、只读集合、重复ID/空/超限/非法枚举/参数、int极值long累计。地区与头条规则不混入。

## Task 2：共享加载及太空工厂

先桩与RED，再实现。实际共享值与合成数值变更、错误版本/Schema/来源/未Verified/缺失或重复用途/错误数值/额外字段/重复JSON拒绝。工厂保持牌对象/Actor，通用+1可使2OPS牌达到第5格门槛；错误用途及视同牌不能建太空请求。做真实Loader→Quote→SpacePlan衔接，不声称完整用牌授权。

同步新的模块JSON、catalog、数据消费说明、Wiki/工作记录/实际报告；Unity全回归+metadata；一次整批review、例行Git同步。无UI/播放器重建。

## Review Focus

按Actor选修正、全部合并再上下限；长整数安全；计分牌/未知用途不默认放行。General值不能充当带地区条件的完整预算。Schema与来源引用只验证子集，不激活任何事件。FromQuote不允许事件OPS伪造实体牌或改变Actor/Card。保持太空既有防重/随机与未接线宿主边界。

## 来源复核

2026-10-14重新读取GMT的2015规则书 https://www.gmtgames.com/nnts/TS_Rules-2015.pdf ，PDF第8页§7.4要求修正合并，§7.4.1归属受影响玩家，§7.4.2覆盖用途，§7.4.3视同OPS遵守相同限制。本批只用无地区奖励通用计算，不据例子自动裁定尚未确认的地区小值组合。
