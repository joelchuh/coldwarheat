# Empty Hand Actions Implementation Plan

> Use executing-plans inline, test-driven-development, and one fresh whole-branch review. User authorizes autonomous development and routine Git push.

目标：普通手牌用尽时，玩家可逐次跳过自己的普通行动；中国牌仍可自愿使用。依据 R2015 §4.5D、§5.1、§9.8。这是既有回合模块的有界扩展，不引入新架构或规则解释。

基线 eee8ba7；复用用户指定 E:/TwilightStruggle 和现有 Unity 导入，分支 feat/empty-hand-actions；保留无关未跟踪文件。上一批 2567 个原生测试通过，后续仅在改动后运行验证。

契约：SkipEmptyHand 由固定身份会话提交；仅 ChooseCard、当前普通名额、零普通手牌、没有事件/根/部署/回合末待处理时允许。不能用“无合法动作”代替空手判定。一次接受消耗一个普通行动名额，CompletedActions/CompletedFor 的含义扩为已消耗名额（完成根或合法跳过）；不改变卡牌、中国牌、随机状态或风险轨道。新 SkipOrdinarySlot 接口区分跳过与完成出牌。可选太空名额继续使用 DeclineExtraActions。每次重新检查手牌，不缓存整回合永久停用状态。

1. [x] Core + session RED/GREEN：不可变进度，命令门禁、回合推进、重新获得手牌、中国牌自愿、额外行动衔接、重复/旧版本/冲突/溢出。
2. [x] Presentation + fixture RED/GREEN：空手按钮和说明，当前方与私密投影；人工近回合末场景、独立 Player 构建入口，正式与打包数据一致。
3. [ ] 整批审查、原生测试、元数据、独立构建和实际点击；同步 Wiki/JSON/工作记录，提交推送并核对远端 SHA。

边界：不实现陷阱事件、强制出牌事件或未知卡效果；后续加入这些状态时必须接入同一门禁。展示层不判断规则、不改变国家或卡牌区。
