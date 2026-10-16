# Player Payload Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: executing-plans inline；整批一次fresh只读审查。用户自主授权替代常规批准。

**Goal:** 构建和核验共用一份载荷映射，提供可靠可发现的只读验收脚本。
**Architecture:** Infrastructure封闭清单/受限生成复制；Editor只调用；Python标准库验收独立读取同清单及工程策略。
**Tech Stack:** C#/NUnit/Unity6000.6.0f1，Python3.12标准库，JSON描述。
**Spec:** Docs/Architecture/player-payload-verification.md

## Global Constraints
- 保留两项用户未跟踪文件和全部旧Player；当前checkout新feat分支。
- 1MiB清单、256条、16深度、源每份64MiB/合计256MiB；纯构建数据，不改正式规则值。
- 路径封闭、预检全量后才复制；不删除、不联网、不自动Git；磁盘写失败不得宣称完整事务。
- 脚本stdout报告不自报source_commit，不把marker当可信构建证明。

## Review Focus
1. source/destination大小写碰撞、DOS设备名、段尾点空格、UNC/盘符、reparse/junction。Task1/2负例。
2. 缺失后半份源不应先覆盖前半份生成文件；Task1真实文件测试。
3. 旧构建多一份载荷/错误字节/少程序集不得通过；Task2真实CLI测试。
4. 超限/中途输入变化/未知JSON字段/重复键不能静默默认；Task1/2测试。
5. marker不绑定源码提交；报告声明限制，最终真实构建记录外层提交及新脚本结果。

## Task1 — 单源清单与构建复制
Create Tools/Configuration/player-payloads.json、Docs/Contracts/player-payloads.schema.json、Infrastructure/DataLoading/PlayerPayloadManifest.cs、PlayerPayloadPublisher.cs、Tests/EditMode/PlayerPayloadManifestTests.cs、PlayerPayloadPublisherTests.cs（C#均Assets/ColdWarStruggle下）。Modify Scripts/Editor/DemoBuild.cs。
Interfaces: PlayerPayloadManifest.Load(string json).Entries；PlayerPayloadPublisher.Synchronize(string projectRoot)。
- [ ] 从现存66条Copy提取清单，一次性核对完整映射不变；先写stub/真实解析与文件系统测试。
- [ ] 原生RED：新测试因stub失败，旧3457保持。
- [ ] 实现不可变描述和预检复制；保留DemoBuild业务验证，新增BuildPayloadManifestPlaytest独立目标。
- [ ] 原生GREEN，保存XML和本任务源码SHA，提交。

## Task2 — 只读核验脚本
Create Tools/Configuration/player-verification.json、Tools/Scripts/verify-player-payloads.py、Tools/Tests/verify-player-payloads.tests.py；配套Tools/Descriptors两份script JSON、Data/Descriptors两份data JSON、Docs/Descriptors模块JSON、Docs/Wiki/player-payload-verification.md、Docs/Testing/player-payload-verification.validation.json；更新三catalog及Wiki/index。
Interfaces: CLI --project-root ROOT --player-directory REL --build-log REL；stdout JSON 0/1。verify(root,player,log)返回只读证据。
- [ ] 先写CLI stub和真实临时文件成功/坏路径/JSON/超限/错字节/缺文件/多文件/日志/程序集/实际junction测试；保存fixture，RED后实现。
- [ ] GREEN，复用既有元数据工具并更新description、工作记录、报告；不新增通用任务运行器。
- [ ] 一名fresh整批审查；Important一次定向RED/GREEN修复，无第二轮盲审。
- [ ] 全量原生通过后独立构建，用新脚本核验实际载荷/程序集/log；记录构建提交和实际限制。
- [ ] 精确提交/push/ls-remote验证，进入下一卡牌任务。
