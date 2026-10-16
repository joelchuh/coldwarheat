# 独立红色政权运行时

基线649c955；分支feat/independent-reds。沿用用户指定的E:/TwilightStruggle与Unity导入；不改变两项无关未跟踪文件。用户授权持续自主开发、不再询问常规实施选择。

这是既有影响力事件流程的有界扩充：独立不可变MatchOpponentInfluenceEffect描述名单，核心只增加正差额；沿用CountrySet选择、固定US受益方、原子事件完成、真实出牌/对手OPS/头条消费者。参数从现有early-influence-batch3读取，仅接独立红色政权；不将经互会短缺边界当作已确认。相比复制逐卡逻辑或加入通用JSON脚本，选用具名纯C#效果和严格加载器，保持地图显示独立。

实施顺序：基础规划和加载器测试→事件/会话/头条及地图选择接线测试→构建和实际选择验收→描述/Wiki/新上下文审查/Git同步。完整规则来源与既有解释见Docs/Wiki/early-influence-batch3.md。本批不改变用户规则裁定。

reds-core-red首次没有XML：新增测试中的NUnit Using重载不明确，编译错误CS0121；补显式<string>后重跑reds-core-red-verified。首次编译失败不计作行为RED。

reds-core-red-verified：2629项/28个新增预期失败；reds-core-green：2629/2629。reds-play-red首次是测试编译错误（缺构造参数、错误属性名），不计行为RED；修正后的reds-play-red-verified：2645项/11个接线与fixture预期失败；reds-play-green：2645/2645，exit0、零跳过。全部摘要及XML哈希将归档到验证JSON。

构建载荷测试reds-payload-red为2648项/3个预期缺文件失败；接入Editor/Player/标准构建后reds-payload-green：2648/2648，exit0、零跳过。元数据152描述/79数据集/373符号通过。构建和真实Player验收仍待进行。
