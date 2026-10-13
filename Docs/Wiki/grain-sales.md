# Grain Sales to Soviets（GS1）

description：#67随机取得苏方普通手牌、美国私有选择、实体牌授权、父子帧与有限消费者边界。状态：verified-subset参数与契约设计；C#/Unity、真实随机、运行鉴权、存储和网络均待实现。

美国从USSR普通Hand随机取一张，包含计分；中国牌、保留头条、解析中及其他牌区均不在候选。权威先核对完整实体库存与普通Hand索引，不接收客户端缩小候选。非空池按稳定CardId排列，有限检查器核对独立保存的原状态、池、索引、结果及RNG前后证据；单候选仍一次逻辑抽样，空池不耗随机。这是证据检验，没有实现均匀随机算法。

抽中实体移到独立InResolution选择帧，美国私有察看后play或return。return恢复同一实体到苏方Hand；空手和return都交接美国使用Grain Sales本身OPS，预算从共享CardDefinition.printed_ops再经美国的共享修正读取，不在本模块另存印刷2。来源OPS仅完成报价与WaitingOperations契约，普通投放/政变/调整尚未接入，不允许太空；头条影响力起点仍须共享提供者证据。未支持的play保留已接受随机结果与选择等待，不重抽，也不擅自强制退回。

实体借牌可走普通合法用牌路线，包括计分和符合资格的Space，即使在头条；FAQ例外随实体牌授权传递，不能扩展到其他事件赠予OPS。头条抽到UN必须return。Defectors只取消确实尚未执行的USSR头条，不撤销已执行结果；有限模型只接入美国GS头条、独立苏方保留目标的情形，未知来源停下。

RootSlot、RootPhasingPlayer、父/子Frame与Resolution、SourceEntity与SelectedEntity分开；ActualCardUser与OperationsActor固定US，EventController按目标和路线确定，Viewer独立。USSR发起GS时即便美国实际用牌，普通DEFCON失败仍归原RootPhasing；古巴危机按另一个共享ActualCoupActor策略，当前不实现政变。源GS在子牌和根剩余工作完成前一直InResolution。正常成功时GS非星弃置；借牌按真实事件成功、blocked或Space路线决定去向，不统一移出。

能力矩阵见参数consumer_matrix。有限离线实际计算仅：Sadat事件共享Egypt清苏加美；SEA按共享七国控制权重计分；NATO实体Space按共享资格/次数/骰阈值/先后VP；Defectors头条时间窗。它们只是新来源的有限参考消费者，不是生产handler。其他已有Bound卡也不因此获得GS授权。普通Operations、UN配牌、Missile Envy、Star Wars递归、Ask Not抽牌、WWBY/Flower钩子及未列计分/Space均ProtocolOnlyPending。未知活动效果亦停止，不把缺失引擎当作规则禁止。blocked事件的弃牌策略已有规则依据，但本批未接入其消费者；布尔blocked回调不能触发清理。

成功play保存Child等待，再接收绑定请求/角色/完整原状态的结果。有限提供者从共享规则独立算出结果，并在外部可信夹具中留锚；完成命令不能用布尔值、card_id或重算hash自证合法。坏回调只拒绝当次，不回滚已接受选择/移牌/随机。保存恢复保留原等待与锚，父根仅在无剩余工作时完成一个slot；USSR尚有根OPS时保留源并交回既有根流程。

立即终局规则已知，但源与子牌的精确清理顺序仍是待定工程政策；有限消费者预测将终局时在执行前停止为TerminalCleanupPolicyPending，保留已接受等待。它不是已运行的终局结算，不宣称丢弃/移出顺序获官方确认。外部later Finished（包括Draw）可以承载历史重放；所有新动作和当前私有查询拒绝Finished，Active/Draw非法，结束后不执行剩余OPS。

收据绑定完整命令、角色、实体、顺序、原状态、数据版本及独立历史锚。旧accepted重放返回原结果和原时点玩家投影，保留后来live状态，不刷新手牌或私有察看。USSR可看自己的手牌，但不因本事件得到US未选手牌；旁观者在play前不能见选中身份，所有视图均不含权威池/RNG/原始收据。命令ID集合从独立锚定历史交叉重建；收据键、命令ID及结果last_command_id必须一致，任一历史表缺失均停止，不能清空缓存后复用旧ID。真正的会话授权、保存与网络仍待运行验收。

来源：[GMT英文卡图p10#67](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)、[R2015 §§2.2,5.2,6.4.5,7.4,8.1.3,9](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)、[FAQv5 p7、pp13–14](https://www.gmtgames.com/nnts/FAQv5.pdf)。Verified规则、TextualDerivation普通清理推导及PROJECT-GS1工程约定分开；终局精确清理仍Pending。采用work/nested-source-edge-research-20261013研究，不引用旧版FAQ冲突条款。

入口：[参数](../../Data/Cards/deluxe-2015/grain-sales.parameters.json)、[描述](../../Data/Descriptors/grain-sales.data.json)、[Schema](../Contracts/grain-sales.schema.json)、[案例](../Design/grain-sales.cases.json)。既有契约：[嵌套来源](nested-card-sources.md)、[普通手牌](hand-events-batch1.md)、[行动](operations.md)、[太空](space-race.md)、[Defectors](defectors.md)。planned_entrypoints只定位未来接口，相关C#文件不存在。登记与整合验证见[报告](../Design/grain-sales.validation.json)；有限模型通过不代表实际服务已经实现。

共享父子帧与职责见[借牌模块](nested-card-events.md)及其[模块描述](../Descriptors/nested-card-events.module.json)。
