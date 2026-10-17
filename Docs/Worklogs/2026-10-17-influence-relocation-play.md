# 影响力迁移可玩接线工作记录

description：记录纯规则、会话、地图草稿和实际验收，未知裁定保持边界。

BASE 04c892c2a79f86efb05c4bbd4248b5fe5457a8d7；feat/influence-relocation。按brainstorming/writing-plans/executing-plans inline执行；用户自主授权，不重复确认。两项未跟踪用户文件保留。

Ruling: 复用InfluenceEvent和InfluenceMutation，新增双分配决策，避免为一张牌分叉根行动宿主。先完整草稿再原子提交，无永久中途移出状态。
Ruling: 继续既有DS1-OVERLAP待裁定；支持不同国家的正常迁移与明确零迁移，不自行新增项目解释。正式数值和规则来源沿用已核对数据。重新读取GMT FAQv5 p7 #33确认事件放置不使用OPS邻接；英文卡图沿用既有目视核对记录，不把本次PDF提取到的旁列译文作规则依据。

任务计划与边界见Docs/Plans/2026-10-17-influence-relocation-play.md和Docs/Architecture/influence-relocation-play.md。Task1开始；旧3518测试作为本批基线。

Task1：首轮仅因NUnit断言重载编译失败，没有测试执行，修正测试调用后重跑。实际RED3584/3518/66，66项均NotImplementedException；XMLa4bd1da64c9a7bb38dae094636ea75cf76e24bfaa909825636aea72f58d0adcd。实现有界双列表、不可变查询、排序守恒事务与严格参数装载后，GREEN3584/3584、零跳过/不确定，XMLede41d762dc1903fdc297539496fcc660616d9552821c4fe82ee2d0ddd9f1009。工作副本溢出不发布；零/少量/四点、控制条件、重叠、所有者、版本、非法条目和参数引用均覆盖。仍未注册真实手牌；Task2接线随后进行。
