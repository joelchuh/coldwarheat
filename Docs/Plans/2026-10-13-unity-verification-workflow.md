# Unity EditMode 验证工作流实施计划

> For agentic workers: 使用 executing-plans 在本会话实施，test-driven-development 先验证失败；完成后用 requesting-code-review 独立审查。主代理实现，不拆分互相依赖的脚本任务。

**Goal:** 固化重复发生的本地Unity测试流程，提供只读预览、新目录运行、真实XML核验和小型JSON摘要，不自动做Git或修改规则。

**Architecture:** 工作脚本负责受限启动与进程生命周期；辅助模块负责路径/版本/配置和NUnit XML的确定性验证；稳定选项放共享JSON，描述JSON用于检索而不作为执行权限。

**Tech Stack:** Windows PowerShell 7.6.5、.NET ProcessStartInfo.ArgumentList、XmlReader禁止DTD、项目已安装Unity 6000.6.0f1。

**Spec:** `Docs/Workflow/workflow-policy.json`、`Docs/Wiki/workflow-and-data.md`，以及四次独立实际工作记录：first-demo、first-influence-event-planners、influence-event-sessions、event-demo（2026-10-13）。不是把一次失败的多次重试计为复用依据。

## Global Constraints

- 仅在调用者明确提供并信任的本地项目、Editor上运行；核对ProjectVersion与exe产品版本/修订。拒绝网络路径、盘根、目录链接和路径逃逸。
- Preview只读；执行仅使用固定参数，没有任意Unity参数、脚本、命令文本或网络/Git动作入口。
- 每次输出写入项目TestResults/UnityRuns/<RunId>全新目录，不覆盖旧XML或日志，不自动删除失败产物。
- Unity本身会导入项目并写Library/Temp/生成meta，可能联系本地许可服务；脚本不是只读游戏验证或沙箱，真实执行仍需宿主权限。
- 已有Unity Editor进程时拒绝执行，不关闭用户编辑器；超时只终止本次启动的Editor，不关闭共享许可服务或其他进程。
- XML必须来自本次新目录，DTD禁用，有大小上限；根计数与实际test-case结果一致且全部Passed才能报告成功。非零退出、缺失/损坏/零用例/跳过/失败/不确定结果都不能报告通过。
- JSON参数与输出有明确边界，失败保留日志和摘要；不自动重试、不自动提交推送。

## Review Focus

- 旧输出或目录重解析点：运行前拒绝，无覆盖；不是防御恶意并发替换路径的系统沙箱。
- Unity退出0但XML缺失或计数伪装：分别验证退出码、实际test-case、过滤命名空间、总数与状态。
- 参数含空格或shell字符：通过ArgumentList传递字面量，不拼shell文本。
- 版本不符/已有Editor：启动前拒绝，不升级项目、不结束已有进程。
- 超时/异常：仅处理本次子进程，记录失败，保留产物，不将部分执行当成功。

## 任务1：受限运行与可验证摘要

文件：新增 `Tools/Configuration/unity-editmode.json`、`Tools/Scripts/unity-editmode-support.psm1`、`Tools/Scripts/run-unity-editmode.ps1`、`Tools/Tests/run-unity-editmode.tests.ps1`；脚本及测试描述放 `Tools/Descriptors`，配置描述放 `Data/Descriptors`，同步两种catalog及Wiki。

接口：入口 `-ProjectRoot <absolute> -UnityEditor <absolute exe> -RunId <safe slug> [-TimeoutSeconds 180] [-Preview]`。共享配置固定平台EditMode、过滤ColdWarStruggle.Tests和相对输出根。支持模块导出New-UnityVerificationPlan、Read-UnityTestResult供工作脚本及合成文件测试复用；不在导入时执行。

- [x] 写真实文件系统正负例，先运行失败：预览、新输出、已有输出/危险RunId/链接/版本不符、正确及矛盾XML、失败/跳过/零用例/DTD/缺失报告。
- [x] 实现确定性路径/版本/配置与XML核验，生产脚本不包含内嵌业务数据；合成测试不启动Unity。
- [x] 实现预览和唯一一次进程运行，结构化输出日志路径与真实摘要；超时只终止自己的进程。
- [x] 跑合成测试，再用真实项目做一次正向集成；另核对拒绝已有目录且不修改它。实际通过游戏测试数与脚本测试数分开记录。
- [x] 更新JSON描述、脚本化证据、Wiki、工作日志；独立审查修复后常规Git同步并核对远端。

本工具只固化验证流程，不实现新的游戏规则或自动形成运行预算消耗任务。

完成：31项工作流测试、149项真实Unity集成及重复RunId不覆盖验证通过；独立复审无剩余问题。Git由独立授权流程同步。
