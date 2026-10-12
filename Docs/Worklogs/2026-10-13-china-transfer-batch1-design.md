# 中国牌事件转交第一批（2026-10-13）

基于 b85e40d，建立[文化大革命、尼克松与乌苏里江](../Wiki/china-transfer-batch1.md)三牌的持有者分支、转交面向、固定VP/亚洲分配、独立ChinaState及根父帧收尾。英文卡图与规则/兼容FAQ核对日期为10月12日；整合及最终检查在10月13日。

判定看当前holder，自己持有FaceDown仍走奖励分支而不翻面；这是卡文条件推导，非专门FAQ。事件转交不构成实际China使用，不取消Formosan，不插入行动或OPS。Ussuri选择亚洲完整4点、每国最多2，无普通OPS邻接/敌控费用。地图、VP、China使用和五年计划事实通过共享引用检索。

初审原171项通过，但独立反例发现三类问题：FYP父事件控制者误用根行动方且漏合法对手父事件；Active核战轨道仍能执行；已完成父来源与父子相同frame/resolution可重复处理。先补29条参考反例，旧投影23条失败，再修正并加入参数/契约案例，共208=22数据＋40契约＋146参考。FYP父controller固定US，USSR行动根父provider为OpponentCardEvent；合法剩余OperationsAfterChild只保留指定USSR根路径，终局同草稿截断并收尾。父已成功、父子身份复用及Active DEFCON1均拒绝。

208版复查又发现同一源/父结算帧可附加第二张牌，导致已完成帧遗留实体。新增23个定向参考反例亲见21个旧逻辑失败，随后统一首次结算、Ussuri新选择与原收据结果中的源/父帧唯一实体约束；合法FYP父子各一张、延迟OPS与后来current重试仍保留。最终233=23数据＋40契约＋170参考。

整合首跑发现cards-v2的三处source_refs缺verification字段，仅为已目视的英文卡图来源补Verified；第二跑发现Data目录必须使用descriptor的检索description而非dataset正文description，修正目录条目。没有放宽契约或改变规则期望。

实际主验233新＋2829旧＝3062；41Schema/44数据集/21planned模块/242JSON/173参数引用，103印刷牌事实未变，仅三牌新增绑定。83 Bound/19 Unresolved、102处理器planned。10项本批运行验收未跑，无C#/Unity游戏实现或真实鉴权/持久化/并发事务验证。13说明检查与72工具测试单列，本批未重跑。

历史验证临时读取器仅适配已绑定库存与相同文件overlay路径，不改规则案例期望；奥运会读取器增加stage→baseline回退，CT1集成读取器仅把严格五文件作者目录限制适配为整合overlay。旧历史验证报告未覆盖。原命令重试返回原完整收据并保留后来current；新选择仍验证当前等待点。

结果见[本批报告](../Design/china-transfer-batch1.validation.json)。独立复审与整合审阅通过后，核对候选/磁盘/暂存字节，常规提交并验证远程。未知未来影响力provider/父链仍RequiresRuling，未启用可选Chinese Civil War。

最终独立审阅ReadyYes：作者233与整合233分别通过，仍为233个唯一案例；51额外探针全过，原四类P2闭环。351基线SHA/8报告SHA/22文件清单、242JSON/41Schema/44数据集/21模块42依赖无环/173参数引用/83效果契约引用/194相对链接核对。103印刷字段未变，仅三绑定新增。三作者JSON经CRLF到LF规范化后文本与JSON相等，原始SHA不同；descriptor只去临时report_status、Wiki只正式状态/计数/报告链接。旧2829与运行服务未在该审阅重复执行；10本批runtime未跑。
