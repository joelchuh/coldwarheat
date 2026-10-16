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

Task3会话子批已验证：RED 3428/3415/13（11个未实现、1个查询无事件预算、1个内部身份异常类型未实现），GREEN 3428/3428，零失败/跳过；XML 70dcdb61d8d2a1b0f1489f6f7684f673b7a5763b6d7295c9861e1eed23f3f605。公开DTO、Host单对pump、控制器选择/重发/用途与界面接线已实现；ActualPlayer仍未构建和操作，不把界面代码通过编译当成目视验收。结果投影从最终PlayerView取DEFCON/终局，避免抵制后政变仍显示旧Flow的DEFCON。下一步四个共享fixture、生产装配保留、元数据与独立Player。

Task3装配子批：19个fixture/装配用例RED均NotImplemented；首GREEN 3446/3447，回归发现War注册复制遗漏Olympic，修复后3447/3447，零失败/跳过。XML b1225ce859b026ce01fb8ff0d139b73ec286421fd212f250afa90ea1fb095771。早期到头条装配、后期IndependentReds/Blockade/FYP/War/SpaceEvent均保留Olympic；人工DEFCON在场景定义中单独绑定，正式限制参数保持。新增JSON摘要/Wiki/入口和构建配置，未新增脚本。审查及独立Player待完成。


整批 fresh 审查（2036360..a7ad17c）发现1项Important、0 Critical/Minor：选定投放/调整用途后，wrapper允许任意更高版本、范围内SpentOps及调整RNG，公开构造GameState可保留原Practice/Operations却替换进度。固定玩家会话不能提交任意GameState，但这违反本批明确的伪造上下文约束。接受修正：绑定当前版本、事件序号、已花点数、地图与RNG，每次合法普通投放/调整原子推进绑定。增加两模式×五种篡改的10例，先跑RED；不修改正式规则解释。此为一次审查后的定向修复，非新功能或第二轮全量审查。

实际Player（源码a7ad17c）已构建，66份StreamingAssets与源字节一致。已鼠标验证美国主办+2 VP、苏联主办-2 VP、美国参赛命令重发不重复结算，以及DEFCON2由美国抵制苏联主办后核战、判苏联负；F12截图保存在本地DemoCaptures。三OPS将在修复后最终Player继续验收。平手续跑目前为自动测试证据，不冒称实际窗口已跑到平手。

审查修复完成：RED 3457/3448/9（投放RNG替换原本已拒绝，其余9例复现缺口）；GREEN 3457/3457，零失败/跳过/不确定，XML 1fb89b3e95d11815c283ec16df40affef9d414c511c14355f5035c06c4a35135。GameState普通OPS提交同步构造新的Olympic wrapper；原根可达性快照保持固定，当前快照仅用于授权核对。审查Important已用定向回归关闭；未声称再次独立审查。最终Player重建与OPS鼠标验收继续。
