# Final Scoring Foundation Implementation Plan

> **For agentic workers:** Use executing-plans inline, test-driven-development and one final independent whole-branch review.

**Goal:** 纯规则的六地区+中国牌最终汇总及胜负检查点，正式共享数据加载。
**Architecture:** FinalScoringService复用RegionScoringService，封闭只读结果交VictoryService；基础设施验证JSON并建立只读定义。
**Tech Stack:** Unity6000.6.0f1、C#、NUnit EditMode、Newtonsoft JSON，沿用现有依赖。
**Spec:** Docs/Design/2026-10-16-final-scoring-foundation.md

## Global Constraints

纯Core无Unity；共享数据唯一；保护用户文件；无活动计分效果；无宿主/UI自动接线；例行Git已授权。

## Review Focus

1. 六区内部计算不接受部分结果，SEA不得重复。
2. 地区越过20后反转，欧洲控制与数字总和优先级。
3. 大数累计顺序独立、真实溢出拒绝。
4. JSON布尔/整数/版本/来源和中国牌目录种类不得降级默认。
5. 不可变结果与宿主资格边界诚实，不能以本服务声称整局完成。

## Task 1: Core最终汇总和胜负接缝

Files: Core/Rules/FinalScoringDefinition.cs、FinalScoringService.cs（新）；VictoryDefinition.cs、VictoryEvidence.cs、VictoryService.cs（修改）；Tests/EditMode/FinalScoringTests.cs（新），路径前缀Assets/ColdWarStruggle/Scripts或Assets/ColdWarStruggle。
Interfaces: FinalScoringDefinition(RegionScoringDefinition,IEnumerable<string>,CardId,int)；CalculateUnmodified(FinalScoringDefinition,ScoringSnapshot,ChinaCardState)→FinalScoringResult；FinalScoringCompletedEvidence(string,string,int,FinalScoringResult)。Task2构造相同definition。
- [x] 写行为测试与可编译的NotImplementedException入口，覆盖验收中的核心/胜负边界。
- [x] 运行run-unity-editmode.ps1 -RunId final-scoring-core-red。Expected:新增行为失败，旧测试仍通过，不以编译失败代替RED。
- [x] 实现最小核心与胜负分支。
- [x] 运行-RunId final-scoring-core-green。Expected:全部通过，0失败/跳过。
- [x] 更新进度并提交。

## Task 2: 数据契约、加载与检索信息

Files: Docs/Contracts/scoring-card-parameters.schema.json、Infrastructure/DataLoading/FinalScoringDefinitionLoader.cs、Tests/EditMode/FinalScoringLoaderTests.cs（新）；Docs/Descriptors、Data/Descriptors、Docs/Wiki和Worklogs（更新）。
Interfaces: LoadSubset(string regionJson,string regionSchema,string chinaJson,string chinaSchema,MapDefinition map,CardCatalog cards)→Task1 definition。
- [x] 新测试先覆盖正式文件成功、非硬编码参数、结构/来源/版本/目录与范围错误；可编译加载入口暂抛NotImplementedException。
- [x] 运行-RunId final-scoring-loader-red。Expected:新增行为失败，Task1保持绿。
- [x] 实现schema+加载器，更新模块JSON/索引/Wiki/验证报告。
- [x] 运行-RunId final-scoring-loader-green。Expected:全套通过；运行validate-project-metadata.py --project-root E:/TwilightStruggle，Expected:0错误。
- [x] 提交Task2。Expected:只包含本批路径，用户文件保留。

## Final whole-branch gate

- [x] 发起一次新上下文完整分支审查；重要问题一轮RED/GREEN修复；记录所有裁决。
- [ ] 保存worklog与真实证据，核对diff与用户未跟踪文件，推送命名分支并核对远端SHA。此次不重建播放器：可执行文件行为未接入新核心。

统一测试命令：& E:/TwilightStruggle/Tools/Scripts/run-unity-editmode.ps1 -ProjectRoot E:/TwilightStruggle -UnityEditor E:/unity6.6/6000.6.0f1/Editor/Unity.exe -RunId <above> -TimeoutSeconds 600。
