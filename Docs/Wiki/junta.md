# 军政府：固定影响与可选免费行动

description：查Junta先选国加2、可选中南美政变/调整、源牌预算、模式与调整地区锁/保存继续位置。

状态2026-10-06：#47 Neutral2Mid非星号参数Bound，C#未实现。[描述](../../Data/Descriptors/junta.data.json)→[参数](../../Data/Cards/deluxe-2015/junta.parameters.json)、[Schema](../Contracts/junta.schema.json)、[案例](../Design/junta.cases.json)、[验证](../Design/junta.validation.json)。66Bound36Unresolved，102处理器planned。

复用CompositeEventResolver的有序步骤、InfluenceMutation与EventOperationsService。可信EffectController选正式Central/SouthAmerica并集中一个国家，固定增加2本方影响，不要求已有己方影响/控制/OPS邻接或敌控双倍；只一个国，不分配两国各1。完整选国合法后提交该影响步骤，再保存可选行动授权和继续位置，不把后续无效操作回滚已提交合法影响；也不能倒序先政变再放影响，FAQ明确先后重要。

可选Grant读取SourceCardPrintedOps（#47原始值从cards.json查），按ActorAtGrantCreation有效实例和既有ops_policy报价，不复制2作预算。只Coup/Realignment，不再投放Influence、不Space，不流入父OPS。可拒绝/提前结束调整放弃余量；模式在首合法接受尝试锁定，拒绝/查询不锁。Coup一次耗整份预算，coup_credit_kind=FreeCoup所以军事0；调整每次标准1OPS，失败也花，仍沿普通调整目标/掷骰/影响力算法。

两地区并集是候选范围，不意味着调整可跨区混做。FAQp10明确调整只能在其中一个地区，可不同于放影响的地区：首合法Realignment的region锁本grant，后续只能该region；国家可换，地区不能换。ModeLock和RegionLock是两个独立字段，失败但合法接受的尝试仍锁，非法回应不改锁/预算/随机。当前目标/合法性逐次验证，不预先缓存后续骰子或坐标。

普通DEFCON、条约、危机、骰修正照常；不从拆墙复制欧洲地理豁免，CA/SA正常地区本就不被基本DEFCON禁。CMC致命Actor先判、不消费骰/军事/降级后续；SALT/LADS骰项及核潜艇按既有原因/实际Actor策略处理。战场free coup仍可能DEFCON降级/核战，原RootPhasingPlayer保留普通核战责任，ActualActor决定危机和自身修正。未注册来源/未知限制先停止；不会为Heading等来源另造根名额。

一次实际cardUse和一个根slot包含选国、固定影响、可选授权、所有子操作与正常非星号弃牌。保存InfluenceCommitted、GrantId、definitionDigest、actor、root/frame/phasing、预算及锁/继续位置与命令收据；重试不再次加点/授预算/掷已完成骰。当前事务错误回滚当前步，原已提交步骤保留；结束GameEnded只截断未执行操作，终局记账遵守[编排同事务规则](turn-flow.md)，不能改已提交Finished。

方法生成计划由编排提交，不与可变服务循环调用或等待UI。实际鉴权/原子事务/完整政变调整/源码/存档尚未运行，表现只收公开增量和授权说明。依据[英文卡图](https://www.gmtgames.com/nnts/TS_Cards_Deluxe.pdf)p8 #47、[R2015](https://www.gmtgames.com/nnts/TS_Rules-2015.pdf)§6.1–3/7.4/8.1/8.2.5、[FAQ](https://www.gmtgames.com/nnts/FAQv5.pdf)PDFp9–10 #47/CMC、p12事件头条行动。

主13数据＋34有限参考＝47新项，1672旧回归，共1719通过；34Schema/37data/21模块、112指针。6runtime未执行，13note/38Tools另层；独立完成稿Ready Yes，无Critical/Important/Minor；13data34ref=47、34Schema meta/目录与DAG/112指针及9SHA独立核对，6runtime未运行。
