# 可恢复随机状态实现计划

> 执行方式：executing-plans，当前会话实现、一次独立审阅。基于用户已授权的纯C#、版本绑定、隐藏信息与持续开发约定。

目标：为后续洗牌和骰子提供可注入、可复制、可精确恢复的随机源。本批不把随机源接入GameState，不实施发牌/完整存档或联网。

设计依据：AGENTS第8节，Docs/Wiki/architecture.md第7节，Docs/Wiki/setup.md的固定洗牌输入和恢复约定。算法取PCG32 XSH-RR，参考作者的[最小实现文档](https://www.pcg-random.org/using-pcg-c-basic.html)和[算法说明](https://www.pcg-random.org/download.html)。实现为本项目C#接口；明确版本pcg32-xsh-rr-v1。

## 设计

- Core/Random下建立IRandomProvider、不可变RandomSnapshot、Pcg32Random。Capture返回算法ID、64位状态、奇数流增量和原始输出计数；Fork返回独立副本。
- Seed接收64位seed及有效63位stream，按PCG初始化；高位stream不静默截断。两个内部初始化步骤不算游戏随机输出。
- NextUInt32用于兼容性与采样基础；NextInt(minInclusive,maxExclusive)支持完整int跨度，通过拒绝采样消除直接取模的区间偏差。宽度1也消费一次原始输出，保证契约简单明确。
- 所有失败（非法区间/快照/计数溢出）保留原状态。NextInt先在局部状态采样，成功后一次更新；规则命令另通过Fork隔离，宿主只在整个事务成功后采用副本。
- Infrastructure/Serialization/RandomSnapshotJson只转字符串，不读写路径。精确字段白名单；schema_version=1；三个ulong使用固定16位小写十六进制字符串以防JSON数值精度丢失。未知算法/版本、偶数增量、格式异常拒绝。不进入玩家公开视图或日志。
- 该生成器服务确定性游戏模拟，不作密码/鉴权用途；未来服务器仍由权威宿主管理内部状态。首次种子的产生属于外围，不调用时间或Unity随机。

## Task 1：随机源

- [x] 先写Pcg32RandomTests并运行RED：作者seed42/stream54的六个已公布输出、恢复连续性、Fork隔离、范围/单值/大跨度、拒绝采样计数和失败不变。
- [x] 实现最小纯C#算法与快照；运行回归。

## Task 2：精确状态编码与收尾

- [x] 先写RandomSnapshotJsonTests：往返、最大整数、未来版本/算法/额外字段、错误hex/重复键、偶数增量拒绝；观察RED再实现。
- [x] 更新模块JSON、Wiki/索引、来源、验证报告与工作记录；实际回归后独立审阅并Git同步。

## Review Focus

1. 原始输出与有界采样的计数不同；拒绝样本也要被保存，恢复不能重播。
2. 区间差值在long/uint计算，不能因int溢出或直接取模引入错误。
3. 溢出发生在拒绝采样中间时整个调用不发布部分状态。
4. Fork和Capture无共享可变数组，源与副本不会互相推进。
5. JSON不把64位随机状态转为浮点数，未知版本不能默认还原。

基础提交c3998b667129836c1ffe4eb0fa0b1f7c1536e8de；实施前634项测试；本批最终674项通过，独立审阅通过，进入Git同步。
