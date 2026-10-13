# 初始影响力部署实现计划

> 执行：executing-plans；延续已批准的持续开发范围，由主代理实现，完成后独立审阅。

目标：将已确认的开局影响力规则实现成可复用的纯C#阶段，并通过已有权威会话与地图显示测试。设计依据：Docs/Wiki/setup.md、R2015 §3.1–3.3；不表示完整开局可运行。

## 边界与接口

完整开局先发牌，再苏联固定/自由部署，最后美国固定/自由部署。本批只消费已经准备好的测试状态，终点为DeploymentComplete，不设置SetupCompleted、不推进TurnStart，不生成普通手牌或中国牌。

- `SetupDeploymentDefinition`保存只读有序阶段，与`MapDefinition`绑定。数值来自既有setup.json；子区名单从地图查询。
- `SetupDeploymentLoader.LoadSubset(json,schema,map)`校验受支持结构、来源、国家和子区，返回部署子集。其他轨道/牌堆引用不解析，不能返回完整RulePack。
- `InitialDeploymentFlow.Begin(state,definition,id)`只接受零影响力、零OPS、无事件/已有部署的准备状态；原子应用苏联固定布置并创建选择。
- `InitialDeploymentFlow.QueryDecision(state,actor)`返回当前身份可用的目标与预算。
- `InitialDeploymentFlow.Resolve(state,actor,decisionId,allocations)`验证非空/上限128/正整数/不重复/合法子区/总量；成功一次提交本方分配及下一方固定布置。最后只标记部署完成。
- 复用`InfluenceMutation`的私有事务缓冲；保持手牌、回合上下文、GameId和OPS，连续事件序号，版本只加一次。失败无任何局面变化。
- `GameState.Deployment`独立于事件及OPS Actor；普通投放和事件在部署状态存在时拒绝，未来正式阶段编排显式交接。

## Task 1：规则、数据读取与回归

文件：Core/Rules/SetupDeploymentDefinition.cs、InitialDeploymentFlow.cs；Core/State/SetupDeploymentExecution.cs；Infrastructure/DataLoading/SetupDeploymentLoader.cs；Tests/EditMode/SetupDeploymentTests.cs。已有GameState、InfluenceMutation和行动/事件门禁只作必要接线。

- [x] 写实际国家固定表、苏先美后、东西欧重叠/加拿大、集中部署、非法分配、幂等阶段标记、溢出原子性、手牌/时代保持、旧行动门禁与数据坏输入测试。
- [x] 运行run-unity-editmode.ps1，先看到缺少实现的失败；实现后全量零失败。
- [x] 更新setup模块/数据JSON、Wiki、工作记录和验证报告；保持完整开局planned。
- [x] 独立审阅，修复必要发现；审阅差异后常规提交/推送并核对远端。

## 后续独立批次

固定身份部署命令、去重收据、私有草稿与地图练习集成各在规则批次通过后展开；复用当前GameSession和IMapView，不创建第二套权威局面。

## Review Focus

1. 已完成部署能否被普通OPS或事件绕过；直接规则入口与宿主均须检查。
2. 自由部署不套用稳定度、敌控、邻接限制；固定布置不能覆盖已提交另一方的影响力。
3. 下一方固定布置及阶段推进必须同事务；溢出不得留下半完成方案。
4. 修改输入数组不能改变已构造的定义/结果；其他子系统复制GameState不能丢部署标记。
5. 子集加载与完整规则包门禁严格区分；人工手牌不伪称真实初始发牌。

## 执行记录

基线962da31；独立功能分支feature/setup-deployment。预检：定义→Flow共享MapDefinition，Flow→GameState保持手牌与Turn；普通行动门禁与事件门禁均消费新Deployment标记，无循环程序集依赖。

Ruling：本批实现部署子集而非完整SetupFlow；原因是正式发牌/卡牌处理器注册仍未完成。测试入口会明确标fixture，避免跳过规则包门禁；代价是不能凭这批代码开始整局。

完成：49项新增测试及全量516/516；独立审阅无Critical/Important，修正文档排序描述。完整发牌/会话部署命令/地图接线不在本批。
