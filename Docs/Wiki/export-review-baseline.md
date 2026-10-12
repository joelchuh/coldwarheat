# 审阅基线导出

> description: 将已知干净HEAD的受跟踪工作文件导出为带SHA256的独立审阅基线，定位输入、停止条件和真实工具测试。
> 状态：PowerShell工具候选已实现并在Windows PowerShell 7.6.5通过34项真实Git/文件系统测试；独立完成稿审阅、项目集成和发布由外层流程处理。游戏C#/Unity仍未实现。

先读[导出描述](../../Tools/Descriptors/export-review-baseline.script.json)；测试另见[测试描述](../../Tools/Descriptors/export-review-baseline.tests.script.json)。描述不授予执行权限。入口分别是`Tools/Scripts/export-review-baseline.ps1`与`Tools/Tests/export-review-baseline.tests.ps1`，只依赖PowerShell 7和显式传入的Git。

## 使用

在调用者已经核对路径、HEAD及本次导出范围后执行。以下变量由调用者明确赋值，示例没有本机个人路径，也不自动取得并接受当前HEAD。

```powershell
pwsh -NoLogo -NoProfile -NonInteractive -File ./Tools/Scripts/export-review-baseline.ps1 `
  -ProjectRoot $sourceRoot -Destination $newBaselineDirectory `
  -GitExecutable $trustedGitExecutable -ExpectedHead $reviewedFullHead
```

`Destination`必须不存在、父目录已存在，并与源目录互不包含。源必须为拥有真实`.git`目录的普通本地仓库。输入使用绝对路径，不接受根目录、UNC/设备路径、目录链接和路径别名。Windows额外要求Fixed盘；调用者仍须确认没有网络存储或远程占位文件。

先核对HEAD、tracked inventory和clean状态，才创建目标。`git ls-files --stage -z`确定范围，只接受普通文件。复制实际工作字节，不做Git checkout或换行转换；每个复制文件计算SHA256，再对源与目标重验。最后重验HEAD、clean状态、原始索引/配置摘要和两次文件清单。`git status`包含非忽略未跟踪文件，因此此类文件会使源被拒绝；忽略文件不复制。

全局/系统Git配置与继承的`GIT_*`环境被隔离，fsmonitor关闭，可选索引写入关闭；系统attributes关闭，`core.excludesFile`与`core.attributesFile`强制指向空设备，避免间接读取根外路径。仓库内`.gitignore`/`.gitattributes`仍适用。生产脚本只有`rev-parse`、`ls-files`和`status`三个Git子命令，每次30秒超时，无自动重试。`.git/commondir`会让Git转向另一份元数据/配置，因此在任何Git子进程启动前以`UNSUPPORTED_REPOSITORY`拒绝，不解析或追随目标。本地include、filter、extensions、config.worktree与对象alternates也拒绝；不支持linked worktree、bare、子模块、符号链接、特殊索引flags和未合并条目。依赖外部忽略或换行配置的仓库可能显示不干净，此时停止，不临时改源配置来绕过门禁。

## 完成和失败协议

目标先创建`.baseline-incomplete.json`开始标记。这个标记永久保留：它说明“没有有效成功清单时一律视为未完成”，并不单独决定最终状态。全部检查通过后，脚本先写`.baseline-success.pending.json`，再不覆盖地重命名为`.baseline-meta.json`。

只有退出0、成功清单能解析且`complete=true`、HEAD符合预期、`copied_files`等于SHA表项数，并逐文件核对SHA256，才可把目标当作有效基线。清单和开始标记是额外元数据，不算复制文件数。失败返回非零及`error_code`；已产生的目录和文件全部保留，不恢复、不删除、不在原目标上重试。开始标记写入本身失败时可能只留下空目标，仍因没有有效成功清单而无效。修复外部原因后选新的目标路径。

该工具只保证文件字节，不复制ACL、时间戳或Unix执行位。HEAD标记不是提交内容校验签名，Git允许的换行规范化与忽略规则照常存在。检查期间必须无并发写入；真实并发变更测试证明已覆盖的变化会停止，但工具没有操作系统级快照隔离，不能防御恰好发生在检查间隙的恶意替换。Linux/macOS实现路径未实际执行，不能宣称跨平台测试通过。

## 脚本化和测试证据

满足[结构化策略](../Workflow/workflow-policy.json)的重复工作流条件：persistent-effects2、UN、Trap、Junta四个独立批次已实际导出269、276、284、295文件的基线，四份本地基线清单的HEAD分别与对应工作记录一致。完整记录在描述的`scriptification_evidence`，未将失败重试当成复用次数。

测试入口创建自己的临时Git仓库，禁用系统/全局配置，不写实际项目或全局Git配置。34项覆盖二进制/Unicode/空格路径字节、HEAD和脏状态、已有及嵌套目标、路径/元数据链接、特殊Git条目、隐藏index flags、配置边界、ambient Git环境、复制中失败和并发变化。新增commondir真实Git回归包含一份本地common/config及只会在fixture内写sentinel的clean filter；验证拒绝发生、sentinel不存在、源和common字节不变。成功及多数失败场景还核对源文件和`.git`内容没有被导出器改变。Windows链接创建权限不足会明确失败，不会跳过并当作通过。

```powershell
pwsh -NoLogo -NoProfile -NonInteractive -File ./Tools/Tests/export-review-baseline.tests.ps1 `
  -GitExecutable $trustedGitExecutable -FixtureRoot $newOwnedFixtureDirectory
```

实测结果见[工具验证报告](../Design/export-review-baseline.validation.json)。测试为真实脚本运行，独立审阅另行进行；34项不计入规则参考模型检查数。既有38项设计快照工具未改、未在此候选阶段重跑。

[返回索引](index.md)
