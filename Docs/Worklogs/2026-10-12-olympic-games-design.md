# 奥运会事件设计（2026-10-12）

基于4fb5e63，英文卡图#20及R2015核战示例核对后建立[奥运会分支与继续流程](../Wiki/olympic-games.md)。对手选择参加或抵制。参加时双方各掷一骰，主办方加2，高者获2VP；平手必须重掷，与峰会的平局结束不同。抵制先DEFCON-1并检查根PhasingPlayer核战，未终局才给Sponsor可选普通视同4OPS。

每对骰、RNG位置、结果/VP、继续收据共同提交。连续平手可按计算片段让出执行，保存内部继续点；片段预算不是游戏重掷上限，不强定赢家，不允许重选抵制或人为放弃。共享普通OPS预算、军事信用、条约和CMC限制；正常根影响力资格为实际Actor根轮起点，头条未核实起点仅阻止Influence。

初审135案例通过，但5个独立反例发现2个缺口：InternalRoll没有重验DEFCON/终局轨道，正常根允许用其他具名快照替代根起点。先加入反例亲见RED，再增加合法未变initial_defcon、非终局initial_vp及phase/VP/winner一致性，正常根限定RootActionRoundStartActor或RequiresRuling。合法最终双方19到21收据仍可读，后续掷骰BranchLocked。两个新增RequiresRuling正例最初误用了别批proof字段，按本契约proof_id修正输入后通过，未更改规则迎合错误输入。

修后151=25参数＋65契约＋61参考通过，10运行验收未跑。原审阅者独立151加18额外探针复查ReadyYes；18探针不重复计入。源实体/牌区/来源控制者、不可变链和抵制先核战边界通过，不能把合成proof与参考哈希说成真实鉴权或持久化。

主验实际151新＋2678旧＝2829离线设计检查；40Schema/43数据集/21planned模块/237JSON/161参数引用。103印刷牌事实未变，仅奥运会新绑定；80 Bound/22 Unresolved，全部102处理器planned。数据描述静态entrypoint为路径及null symbol，不代表运行代码。13说明检查与72工具测试独立，本批未重跑。

结果见[本批验证报告](../Design/olympic-games.validation.json)。344受跟踪文件导出为整合复审基线；复审完成后核对候选/磁盘/Git暂存字节并常规提交推送。未创建C#/Unity实现，特殊来源影响力起点仍待核实。

整合另经独立审阅ReadyYes：新151通过，344基线/8报告摘要、237JSON/40Schema/43数据集/21模块42依赖无环/161引用/181最终文档链接核对。只奥运会1绑定改变，作者params/schema/cases字节一致；descriptor仅去临时report_status，Wiki正式化。LI1/RO1历史读取的十份文件均匹配既有审阅SHA。此轮未重跑旧2678或此前18复探，10运行验收未执行；无未处理问题。
