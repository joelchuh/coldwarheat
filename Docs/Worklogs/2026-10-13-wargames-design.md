# 战争游戏事件设计（2026-10-13）

基于7648dd9建立[Wargames三选择与终局契约](../Wiki/wargames.md)。英文卡图#100和FAQv5 p17–18明确：DEFCON2时可给对手6VP并结束、给6VP继续、或者都不做；不能把中间选择省掉。给分继续仍检查完整VP批次后的正常自动胜利。结束跳过最终地区/中国牌计分，也不转入回合末军备或扣留计分牌审核。

DEFCON3–5为事件未发生，Discard而非星牌移除；DEFCON2下DeclineBoth成功但零转移并移除，为FAQ used条件与§5.2的具名推导。GiveSixAndEnd后恰零仍在任何送分/牌区/根槽变化前RequiresRuling，用户尚未确认Draw解释；Continue恰零可继续，DeclineBoth恰零不主动结束。没有采纳二手或数字版规则。

来源只覆盖已核实普通中立RootEvent/HeadlineEvent；Neutral不能执行五年计划的US关联子事件，未知嵌套/父收尾停止。源实体、完整VP提供者和根slot清理证据先验；同草稿完成必要收尾后才写Finished。原CommandId收据返回完整旧结果并保留后来current，真实鉴权/持久化/事务仍待实现。

原178作者及独立检查通过，但46独立探针中两例发现重复CardId或同源frame第二实体仍被接受。先补2契约＋2参考反例亲见RED，再把基础牌组CardId/Instance唯一和源frame恰一实体用于source_bundle、fresh与历史receipt领域校验。修后182=19数据＋73契约＋90参考独立通过；另128独立探针全过，未重复计入设计总数。

整合第一次根据猜测Wiki名失败，随后按胜负模块descriptor定位risk-victory.md；不完整原候选保留，正式候选用独立v2目录。首轮全回归发现PROJECT-WG1来源分类超出现有cards-v2枚举：仅全球effect_contract条目映射为Pending，保留详述推导和未定零分的locator；参数自身的详细ProjectDerivationOrEngineeringBoundary不变，没有假称官方Verified或放宽全局契约。

主验实际182新＋3062旧＝3244离线检查；42Schema/45数据集/21planned模块/247JSON/177参数引用。103印刷牌字段未变，仅Wargames新增绑定；84 Bound/18 Unresolved，102处理器planned。10运行验收未执行，游戏runtime0。13说明检查和72工具测试独立，本批未重跑。历史读取器只适配全局绑定库存与相同文件overlay，不改规则期望。

结果见[验证报告](../Design/wargames.validation.json)。358受跟踪文件作为复审基线，独立整合审阅通过后核对候选/磁盘/暂存字节再常规提交并验证远程。作者到整合统一LF，原始SHA按实际字节记录。

最终独立整合审阅ReadyYes：新182独立复跑，358基线SHA/9报告SHA/23文件清单、247JSON/42Schema/45数据集/21模块42依赖无环/177参数引用/84效果契约引用/183相对链接核对。103印刷字段未变，仅WG绑定/效果契约改变；PROJECT-WG1全局Pending且locator保留。该轮未重跑旧3062或另一审阅者128探针，10runtime仍未执行；作者JSON仅换行归一，描述去临时report_status，Wiki正式化。
