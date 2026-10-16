# 早期战争事件运行基础

> description: 把已绑定的三张早期战争参数转为可测试的纯C#结算，保留权威出牌和持续效果接线边界。

用户目标是逐步完成可玩的本地Demo、复用模块和替换地图表现；已授权自主决定和持续开发。本批是独立规则子系统的基础实现，采用brainstorming architectural路径与writing-plans，随后在当前会话执行；依据用户“自行决定不用询问”不再发起例行审批。基线为已验收并同步的cc309fc2ca6580a05748d4b5290c8ba6b76787a6，工作分支feat/early-war-foundation。

## 范围和规则

读取现有war-events-batch1.parameters.json中的共享war_policy和三张定义：朝鲜、阿以、印巴。数值只由正式参数组装；不从印刷OPS或临时OPS修正计算军事/VP。固定战争收益归苏联，印巴归事件控制者。邻接按规则地图直接边、控制按稳定度差；阿以另扣目标自身控制。胜利保留已有己方影响力并接收全部对方影响力，失败保留原局面。零影响力仍合法，修正骰不保底。

未被阻止的合法尝试只调用一次有界骰，胜负都给卡牌军事值，成功才给卡牌VP。战争不检查DEFCON地区限制，也不降级；已核战或已达VP终局不能新开。军事、国家、VP和新随机游标组成同一不可变结果，宿主以后统一提交。使用既有MilitaryOperationsService和VictoryService；不新增第二套收益/胜负算法。

## 组件

- WarEventDefinition.cs：只读共享WarPolicy、三种具名WarEventKind、逐牌定义以及绑定正式CardCatalog/MapDefinition的WarEventCatalog。目标列表防御复制；卡牌身份、阵营、星号、effect/parameter引用必须一致。
- EarlyWarDefinitionLoader.cs：现有JSON与schema封闭验证、版本/来源/引用核查，再组装三牌；不执行表达式，不自动加载中晚期战争。
- WarEventRules.cs：QueryTargets和Calculate纯投影，固定骰用于真实算术测试；不是客户端掷骰接口。输出明确列出修正来源和影响力转移。
- WarEventService.cs：WarEventState/Plan与单骰事务规划。核对当前局面、完整地图、原始随机快照；独立fork推进，输入随机器不变化。先验证军事信用、最坏胜利转移/VP溢出，再掷骰。非法/未知组合零随机；随机器异常不返回计划。

本批只接受可信规则调用方提供的WarInterferencePolicy：NoActiveInterference或CampDavidPreventsArabIsraeli。第二项只对阿以合法，零骰/零军事/零VP返回Blocked。Unknown或未实现组合拒绝。这是内部计算前提，不能来自玩家Command，也不证明Camp David/Flower Power事实系统已完成。正式接线须从权威活动效果/成功事件事实推导，不能靠弃牌堆、卡牌可见或客户端布尔值推断。未来新增效果时先扩充该检查再注册处理器。

## 接口与边界

`EarlyWarDefinitionLoader.Load(json, schema, map, cards, knownEffectIds)`返回`WarEventCatalog`。

`WarEventRules.QueryTargets(catalog, card)`返回只读已注册候选；`Calculate(catalog, card, countries, controller, target, roll)`返回`WarCalculation`。

`WarEventService.Plan(state, card, controller, target, resolutionId, policy, random)`返回`WarEventPlan`。state携带Catalog、Countries、Military、Defcon、Victory、VpForUS、Random；before/after绑定相同规则对象。重复军事ID拒绝，幂等由未来宿主回执层实现，不能重加收益。这里不处理出牌合法性、身份会话、父事件/头条、星号去向或行动轮；不把纯服务测试叫整张卡已可玩。

## 验收

加载正式三牌；封闭参数/来源/引用负例；三牌骰1–6、临界控制、直接超级大国不递归、阿以自身修正、印巴双方/双目标、空白目标、失败军事、VP符号/终局、累计军事超过5、溢出及非法目标。随机provider匹配、独立fork、非法返回和不推进都拒绝；PCG重放同输入同结果。阻止仅阿以，其他卡或未知policy不得默认成功。原生完整套件、元数据和独立分支审查；基础批不重建相同界面的Player，后续接线批再构建人工战争场景。

规则来源：[2015规则§2.1.5/7、7.6、8.2.3/4、10.2/3](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)、[FAQ二版/Deluxe适用说明及#24](https://www.gmtgames.com/nnts/FAQv5.pdf)、已有[战争知识](../Wiki/war-events.md)及其绑定卡牌参数。2026-10-17在线复核规则书战争空白目标、邻接及FAQ无DEFCON降级；卡牌数值沿用已有已核实参数，本批没有新的规则解释。
