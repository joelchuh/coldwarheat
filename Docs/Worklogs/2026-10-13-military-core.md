# 军事行动基础服务实现记录

基线737bd61，feature/military-operations-core。上一批DEFCON推送成功，首次远端核验TLS中断；只读重试确认本地/远端737bd61dfd3d79b5ff363f046a3330332a5d7e73相等。

Ruling: 独立State持有本回合信用与评估收据，重复评估计划显式输出VpDeltaToApply=0，同时保留原Assessment净值供审计；未来宿主仍须把VP、State及步骤游标原子提交。当前没有完整存档、命令认证或整局胜负，不把纯计划冒称已接入。

Task1：先写可编译桩和测试验证RED。

Task1：military-core-red 881项中41新增失败，全部NotImplementedException；green为881/881零跳过，XML SHA9f0965ff502ca773dc7e4dd49cff597352f2857513369c40109465ce35f2df9d。实现信用幂等、完整事实冲突、非负累计、受益方与纯评估/归零计划；重复评估只保留原审计净值，VpDeltaToApply为0。

Task2：military-loader-red 911项中30新增失败，全为桩异常。新回合Create读初值并清空本回合信用，旧回合信用拒绝；新回合推进本身仍由未来宿主负责。

Task2 GREEN：911/911零跳过，exit0；XML SHA a5176b5c0d0dd68d449fc714167ec6646c5fa92bf2aedfd4ec82102c435b14b6。共享军事JSON未修改。模块/data描述与目录状态同步，Wiki/索引和README明确源码911与旧播放器762的区别。108描述/34模块/68数据/6脚本，路径/符号/description与模块/脚本status校验一致。

本批未新增工作脚本、未改UI或构建播放器。等待整批独立审阅和授权Git同步。

Final review: 独立审阅通过，无Critical/Important；4份XML及源码/数据/描述再次核验一致。Final: minor (deferred): 现有kind冲突用例先因不匹配basis被拒绝，后续可补WarEvent→DirectEventFixedCredit且同VerifiedCardParameter来单独覆盖同ID不同kind路径；实现本身已比较全部事实。

Final: Ruling: 身份资格、宿主状态/VP/游标原子提交、持久化防重和UI不在此批范围；保留明确限制，避免把纯计划当已运行整局。同步按已授权工作流执行，以Git远端核验结果为准。
