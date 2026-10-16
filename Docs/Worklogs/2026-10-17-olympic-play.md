# 奥运会正常手牌试玩工作记录

> description: 从真实手牌到比赛/抵制OPS和独立Player的接线证据。

BASE 203636085d1a431b7fe796e6b73c198f51cfd718，分支feat/olympic-play。用户已授权持续自主开发/编码/Git；沿用E:/TwilightStruggle，不改两项用户未跟踪文件。采用brainstorming→writing-plans→executing-plans inline、TDD、一名fresh最终只读审查。

Ruling: 首批只注册正常行动轮奥运会，头条/特殊嵌套保持未支持；保留OG1-OPEN-01。代价是暂时不能把奥运会选作头条，但不会自行定义影响力起点。
Ruling: 内部每次只推进一对，由可信Host及Demo Update驱动；没有客户端继续按钮。代价是无UI/服务器时调用者需显式pump，不把帧率或动画当成规则。
Pre-flight: Task1产生GameState中的Olympic父执行与公开结果源；Task2复用同wrapper和既有Ops；Task3只通过Core/Host消费，不直接改地图或权威state。新增参数全部置于现有构造末尾，需全量检查生产复制入口。

Task1 RED：3386总数，旧3362通过、新24均NotImplemented失败、零跳过；XML 836cecc6f192c1639695e0bf9532904d5b0e0f5c798da096e8bfb7440dfb9481。使用真实完整CardZones、正式Olympic定义与真实PCG种子定位字面骰子场景，未向生产接口传入骰值。已实现正常根绑定、两响应/内部单对、VP/DEFCON提交、正常弃牌单槽及终局Resolving审计，GREEN运行中。

Task1 complete：3386/3386原生GREEN，零失败/跳过，XML 61615f9ca4a635951969e428757febc5fc629b2a3310347bd43a848d1cf0976f。真实根/响应/逐对与比赛、抵制终局/存活已验证。Task2 Ruling: 增加CanContinueOlympicOperations共享门禁并由InfluenceRules调用，避免地图直接提交绕过事件父上下文；不改变普通投放算法。

Task2 complete：新增29例。首次运行因测试误用internal构造器未产XML，已改为真实出牌激活效果；不计有效RED。有效RED 3415/3386/29，其中25个stub未实现、4个旧政变查询拒绝事件授权。初次GREEN 3408/3415，7例揭示模式选择后的上下文匹配仍保留等待阶段；补齐进行中模式/预算对象绑定。最终GREEN 3415/3415，零失败/跳过，XML 545ae89e77a3a4bd1fcbc5edc487f36d93bcd6c10f0d6c03646e541b9dd21580。

三OPS复用已完成：QuoteAsIfCard、固定根可达性/当前费用、普通调整骰、政变/军事与核战；Finish只完成一次原根。四种跨上下文测试覆盖game/version/random/source；内部封闭的VP/风险/根状态不能通过公开测试构造器任意变造，本批不虚称逐项伪造测试已覆盖。地图fixture按正式邻接确认Angola→Botswana→Zimbabwe，额外Italy影响力用于验证France费用变化。Task3宿主投影/界面/装配/Player待完成。
