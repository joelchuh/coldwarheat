# 晚期手牌事件设计（2026-10-13）

基于a67a3de，新增[伊朗人质、恐怖主义与Ames Remix](../Wiki/late-hand-batch1.md)。人质清除伊朗US影响后给USSR加2，并记录永久双弃条件；恐怖主义只从对手普通Hand无放回随机弃牌，整批验证与RNG一次提交；Ames先接受本回合私有揭示再等苏方选一弃牌，后续当前视图与原历史收据分开。FAQ已明确旧Ames排序规则不适用Remix。

没有将空手/短缺默认为最大可完成量，也不自动采用其他牌的解释。Terrorism短缺、Ames初始空手及新ID重复活动实例仍RequiresRuling。晚期三牌都不是US关联，不能走五年计划US子事件路线。共享卡牌/地图/核战/VP/China/Hand事实通过引用获取。

独立审阅最初复跑161全过，但额外71探针出现4失败，对应2项P2：CurrentView只检查Finished，仍允许Active且核战/终局VP/winner状态泄露手牌；Hostage与Ames不同卡可共用ResolutionId。作者增加10回归先观察9 RED与1合法多阶段GREEN，修为新查询共用合法活动状态检查、跨活动事实/实例一致绑定Card/Effect/controller/turn。同牌合法多阶段保留，StepId不透明；历史合法收据仍允许后来current已结束，不借新查询校验重算历史。

修后173=12数据＋33契约＋128参考通过；主验173新＋3446旧＝3619，44Schema/47数据集/21模块/257JSON/200参数引用。仅三牌新增绑定，103印刷字段未变；89 Bound/13 Unresolved，102处理器planned。12运行验收未执行，游戏runtime0；13说明检查和72工具测试另计且本批未重跑。真实随机、鉴权、持久化、访问投影与事务服务待实现。

PROJECT-LH1在全局effect_contract标Pending并保留期限推导/边界出处；参数原始出处分类不篡改。作者文件归一LF，描述删除临时report_status。结果见[报告](../Design/late-hand-batch1.validation.json)，待修后独立复核与整合结构审阅通过再核对磁盘/暂存并正常提交推送。

修后独立ReadyYes：173独立复跑、原71探针恢复通过并增12正负边界，共83全过。整合372基线SHA/10报告SHA/25文件清单、257JSON/44Schema/47数据/21模块42依赖无环/200参数引用/89效果引用/201相对链接一致。103印刷字段不变，仅三绑定更新；读取适配器全部业务方法AST相同。独立整合未重复173、旧3446或runtime；无剩余发现。
