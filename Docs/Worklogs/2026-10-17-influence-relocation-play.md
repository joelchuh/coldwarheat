# 影响力迁移可玩接线工作记录

description：记录纯规则、会话、地图草稿和实际验收，未知裁定保持边界。

BASE 04c892c2a79f86efb05c4bbd4248b5fe5457a8d7；feat/influence-relocation。按brainstorming/writing-plans/executing-plans inline执行；用户自主授权，不重复确认。两项未跟踪用户文件保留。

Ruling: 复用InfluenceEvent和InfluenceMutation，新增双分配决策，避免为一张牌分叉根行动宿主。先完整草稿再原子提交，无永久中途移出状态。
Ruling: 继续既有DS1-OVERLAP待裁定；支持不同国家的正常迁移与明确零迁移，不自行新增项目解释。正式数值和规则来源沿用已核对数据。重新读取GMT FAQv5 p7 #33确认事件放置不使用OPS邻接；英文卡图沿用既有目视核对记录，不把本次PDF提取到的旁列译文作规则依据。

任务计划与边界见Docs/Plans/2026-10-17-influence-relocation-play.md和Docs/Architecture/influence-relocation-play.md。Task1开始；旧3518测试作为本批基线。

Task1：首轮仅因NUnit断言重载编译失败，没有测试执行，修正测试调用后重跑。实际RED3584/3518/66，66项均NotImplementedException；XMLa4bd1da64c9a7bb38dae094636ea75cf76e24bfaa909825636aea72f58d0adcd。实现有界双列表、不可变查询、排序守恒事务与严格参数装载后，GREEN3584/3584、零跳过/不确定，XMLede41d762dc1903fdc297539496fcc660616d9552821c4fe82ee2d0ddd9f1009。工作副本溢出不发布；零/少量/四点、控制条件、重叠、所有者、版本、非法条目和参数引用均覆盖。仍未注册真实手牌；Task2接线随后进行。

Task1 Git：f26e6262cbec0cb137ca8160b1395b0e3e22bdc0已push至feat/influence-relocation，ls-remote一致；尚未整批审查/构建，保持分支。Task2新增真实手牌两方/零迁移、六种事件/OPS顺序组合、双头条归属、最终普通槽、收尾溢出，以及会话身份/双列表幂等/坏信封测试。先验证未注册/未接通时的失败，再接宿主。

Task2：RED3617/3584/33，失败为未注册迁移效果。首次接线后3615/3617通过；两项头条测试重建GameState漏传已完成Deployment，触发正确SetupInProgress门禁。修正fixture保留raw.Deployment，保持产品前置门禁，重跑待结果。

Task2 complete：native3617/3617，零跳过，XML6aa57bc7b1f32b9a57b6a72f35c41456610fe43b21c20e8c572c3d4e371e98f6；玩家会话和真实牌区完成。美国出牌仍由苏联选择，头条与双顺序三种OPS共用既有收尾；两个完整分配共同参与幂等比较。运行命令同Task1，RunId influence-relocation-session-green-v2。Task3随后接地图。

Task2 Git：58faeb4a78f751929a6791e0a378243a8cdc295d已push并核对远端一致。Task3 RED3643/3617/26均NotImplemented；已接双草稿/动态地图目标、两种fixture、Bootstrap与3项共享Player映射。GREEN运行中，不将源码接线视为试玩通过。

Task3 native complete：3643/3643、零跳过，XMLc682bdd9cb79595a63de61b18c2898e0b76080de8656f66c8fe9e82068d6f398（influence-relocation-demo-green）。26项覆盖保留旧机制、严格fixture、双草稿不改权威/失效/守恒/身份/重发及地图切换。参数仅description更新运行导航，业务值未改；Build验证仍会读取最终文件。既有描述用双SHA目录同步脚本更新，新fixture描述与catalog同时登记。下一步整批独立审查与新Player。

Task3代码cbdd279da2b4c92f19c39953a5158db7c0271b22已push、远端一致。因既有Bash不可用，本次用一次性Python调用git log/stat/diff --unified=8产生TestResults/influence-relocation-review.diff，独立审查已分发，未重复建脚本。新RelocationPlaytest从该提交构建exit0，69载荷810517字节、5Managed全部只读核验通过，报告SHA36057c04a2b75904826a352620cccdea84ad2caaf0336f93b3f3bdd52576283d。

实际UI：已启动第36个迁移人工局面，Windows更新弹窗遮挡并提示09:14重启；仅尝试激活游戏后仍遮挡，不操作系统更新。尚未执行任何游戏点击，迁移/零迁移/重发鼠标验收保持未完成，不能用3643自动测试代替。所有源码已远端保存；继续不依赖界面的工作。

Final review：review_relocation_play独立只读04c892c..cbdd279，无Critical/Important；未重复执行测试/构建。
Final: minor (deferred): DemoView.Relocation.cs:30的36px反馈区域扣31px标题后仅5px正文，且未组合政变摘要；展开结果页可读，本批记录为非阻断待办。
Final: Ruling: 官方同国重叠裁定未由审查员重新判断 — 继续既有RequiresRuling — 若将来确认合法，需补该分支。
Final: Ruling: 实际鼠标/渲染验收未由审查员判断 — Windows更新弹窗阻挡，保留未完成，不冒称实机通过 — 风险为实际交互尚待检查。
Final: Ruling: 构建正确性未由审查员独立重跑 — 采用本批父任务exit0及69载荷/5程序集只读报告 — 它不证明可信来源链或游戏界面正确。

后续先独立推进经互会运行基础；本批鼠标验收与Minor保留，不把整个Task3标完成。
