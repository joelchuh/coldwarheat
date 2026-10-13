# 项目元数据只读校验工具实现计划

> executing-plans inline执行；一次整批独立审阅，正式脚本带JSON描述。用户持续工作授权有效。
**Goal:** 固化已多次使用的描述/catalog/入口/数据依赖一致性验证，减少每批临时重写检查逻辑。
**Spec:** Docs/Workflow/workflow-policy.json及Docs/Wiki/workflow-and-data.md。
**Architecture:** Python标准库只读CLI，读取共享配置、现有workflow-policy必备字段和三个catalog；返回有限JSON报告与退出码，不执行描述指向的代码。
**Tech Stack:** Python3.11+标准库；本机已有Python运行时。测试使用新临时fixture目录，保留产物。

## 脚本化依据

不同实际使用场景：2026-10-13-deterministic-random、initial-card-deal、dealt-setup-demo、defcon-core、military-core、victory-core的Worklogs与实际验证报告。至少三个独立批次需要相同输入/步骤/结果，且下一批仍要同步JSON/Wiki/代码；不是把同一失败重试计数。DEFCON批次还发现目录status与描述不一致，说明需要持续门禁。

## 边界与输入

- 只接受已存在的本地绝对ProjectRoot；逐级拒绝符号链接/重解析点，读取目标必须在其内。路径穿越、绝对子路径、未知文件和重复JSON键失败。
- Tools/Configuration/metadata-validation.json集中保存三类catalog路径、描述目录、业务类型/id字段及输入上限；为该配置增加data描述/catalog条目。
- 必备字段从既有Docs/Workflow/workflow-policy.json读取，不复制另一份业务字段表。
- 核对catalog ID唯一、descriptor ID或dataset_id匹配、description一致；module/script的status一致，data的catalog业务状态与descriptor可读状态分别校验非空，不误认为同一语义、描述文件未漏登、entrypoints真实文件、C# symbol词法存在、data_dependencies可解析。
- planned_entrypoints允许未实现，不作可调用入口；不会执行脚本/代码、写Git、访问网络、修改输入或自动修复。
- C#符号检查只承诺词法定位，不是编译或语义认证；完整编译仍靠Unity测试。单次报告只输出计数/必要失败，不转储全部描述正文。
- 工具针对可信项目元数据，不承诺抵抗校验期间并发替换/硬链接；调用期间路径与文件保持稳定。入口文件大小也受限；计划符号不检查。
- JSON类型/深度/字节数及描述数量有界。配置自身用固定小型启动上限读取，配置之后的读取遵从其共享限额。

## Task 1：只读检查器与真实fixture

- [x] 新增Tools/Scripts/validate-project-metadata.py、Tools/Tests/validate-project-metadata.tests.py及共享配置；先行为RED再实现GREEN。
- [x] 测试成功且输入摘要不变；缺必备字段、ID/description/status冲突、漏登/重复、缺入口/符号、数据依赖未知、重复JSON键、路径逃逸/非绝对根/不存在根/超限/畸形类型拒绝。
- [x] 测试planned_entrypoints允许缺文件；测试不执行入口；含空格路径可用；链接边界单独报告实际验证方式。
- [x] 所有测试使用新临时根，保留失败产物，不清理无关文件。

## Task 2：描述与工作流接入

- [x] 两份script描述、配置data描述、对应catalog；Wiki说明命令和错误边界，Worklog记录真实复用依据与实际结果。
- [x] 运行工具核验本仓库全部元数据；只读模式前后Git状态/源文件hash一致。
- [x] 独立整批审查并修复重要问题；不运行Unity（不改游戏源码）。
- [ ] 交付步骤：提交、快进main、push并核对远端。

## Review Focus

1. 外部目录或链接不能借descriptor路径绕出根目录。
2. JSON重复键/大小/深度异常明确失败，不能给通过。
3. 元数据只定位，不形成执行授权或eval入口。
4. 检查从共享策略取必备字段；脚本与测试都有可查描述。
5. 正式脚本真实复用依据与测试证据，不以测试通过冒充C#编译或全部规则验证。
