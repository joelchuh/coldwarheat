# 两牌额度影响力移除

日期：2026-10-13。沿用持续优化及常规Git授权；从f4223ce建立feature/budgeted-influence-removal。

落实已确认EI2-RULING-01：社会主义政府/苏伊士危机共用容量、每国上限与完整额度选择。纯Core不依赖Unity；共享参数没有复制进UI。选择类型隔离国家集合和数量分配；非法输入不产生NextState，零容量仍完成，顺序规范化且大数汇总不溢出。新增Loader只返回两牌，保留铁娘子引用但不虚构活动状态。

实际红绿记录：Core新增36项，206项中33项在实现前失败、实现后206全过。Loader新增24项先因未实现失败；第一次实现后228/230通过，2项揭示现有ContractJson缺少oneOf。新增5项oneOf测试后232/235通过，再补齐恰一匹配与所有分支预检后235/235通过。所有运行均Unity6000.6.0f1真实EditMode，完整XML摘要位于Docs/Testing/budgeted-influence-removal.validation.json及忽略的TestResults/UnityRuns。

独立审阅：通过，无Critical/Important/Minor问题；只读核验XML与哈希，未重复运行Unity。文档与JSON同步；不将固定工程操作另写新脚本。

范围限制：额度选择尚未接入宿主与UI，旧Demo没有注入新定义；本批未重建播放器或进行鼠标验收。东欧动荡、完整持续效果、出牌/牌区、存档和完整回合仍待实现。下一批接通数量选择命令与可撤销本地草稿，再增加两个人工练习。

Git同步：本记录随本批代码提交；按已授权流程快进main并推送，最终本地/远程SHA以实际工具核验为准。
