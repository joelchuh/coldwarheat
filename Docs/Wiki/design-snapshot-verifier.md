# 设计快照的只读核验

description：检查候选清单、报告SHA256与发布后副本；不执行卡牌逻辑或Git操作。

入口：[脚本描述](../../Tools/Descriptors/verify-design-snapshot.script.json)→[核验脚本](../../Tools/Scripts/verify-design-snapshot.ps1)；[测试描述](../../Tools/Descriptors/verify-design-snapshot.tests.script.json)→[38项真实文件测试](../../Tools/Tests/verify-design-snapshot.tests.ps1)；[验证记录](../Design/design-snapshot-verifier.validation.json)。本机PowerShell 7.6.5实测；不是C#/Unity或游戏运行实现。

已有四个不同实际批次重复使用相同的清单/报告/落盘核对，达到workflow-policy门槛；这次只固化稳定的只读部分。ProjectRoot与CandidateRoot从参数提供，不硬编码个人目录；报告JSON是已完成主校验的共享产物，脚本逐项查它的validated_sha256，不复制任何卡牌/规则数值。

## 调用与结果

```powershell
pwsh -NoProfile -NonInteractive -File Tools/Scripts/verify-design-snapshot.ps1 `
  -ProjectRoot 'E:/TwilightStruggle' -CandidateRoot 'C:/work/candidate' `
  -ManifestPath 'C:/work/files.json' `
  -ReportRelativePath 'Docs/Design/example.validation.json'
```

示例目录均为调用参数，工具不创建它们。清单是非空JSON相对文件数组，精确列出候选所有文件。现代报告必须有new_cases_passed/previous_cases_passed/total_cases_passed、逐项passed=true唯一ID结果及非空validated_sha256。依赖文件若候选没有，按同一相对路径读ProjectRoot。只比较这些已列摘要；未列摘要文件的领域正确性仍依赖主验证/审阅。

正常返回退出0及verified、文件/摘要数、reported_cases与scope。reported_cases仅转述报告中已核对的计数，不表示脚本重新执行了这些案例。复制到项目后加CheckPublished，逐个比较清单文件的SHA256内容；两个根必须不同。失败退出1及稳定error_code，不尝试修复、复制或扩大范围。

## 符号、流程与边界

Get-CheckedRelativePath检查相对路径与根边界；Assert-NoReparsePoint拒绝目录链接/联接与其父路径；Read-StrictJson/Assert-UniqueJsonKeys防止JSON同名键覆盖；Assert-ReportShape验证摘要格式、数量与通过记录。主入口按清单精确覆盖候选目录，再核对报告摘要，最后可选核对发布副本。

越界、盘根、重复清单/路径别名、缺失文件、额外文件、JSON歧义、过期摘要与副本不一致均停止。它不检查Git暂存、HEAD、远端或审阅是否发生，也不重复规则模型；Git前仍按既有流程核对暂存字节/差异，push后另核对远端。工作期间要求无并发写入；文件系统没有被锁定，不把多次读取当作原子快照证明。

测试用新的自有目录；省略FixtureRoot自动在系统临时目录建新GUID子目录。TestCases只接受固定唯一ID，不把名称当任意路径或脚本。测试创建文件/联接是可见副作用，不删除任何目录；已存在根和链接父路径在写前拒绝，避免覆盖用户文件。核验器本身只读，测试逐项确认其输入内容未变。

红→绿证据覆盖缺少实现、过期摘要、九类路径/清单、发布副本、JSON/报告、测试根保护。最终38/38通过；另实测已同步计分批26文件/10摘要的CheckPublished。1129旧规则离线检查另记录，38脚本运行检查不混成新的游戏通过数量。独立审阅合入Yes：路径守护Important与名单文字Minor已修复；原37项独立通过，修复后4条边界独立复跑/7摘要核对，不冒称独立全38或1129规则重跑。源：[工作流策略](../Workflow/workflow-policy.json)。

本地常规盘符路径限定在文件系统访问前执行；UNC/扩展设备路径拒绝，重解析祖先按从根到叶核对，避免先经链接访问后才拒绝。两项扩展本地路径拒绝例已验证。

审阅修复：测试入口现与主核验器一致，在任何文件探测前拒绝不支持路径，并从根到叶检查祖先；扩展新FixtureRoot拒绝时无夹具产生。所有输入由调用者先确认物理本地盘，不用SMB映射盘/远程占位文件；盘符检查不是网络隔离证明，脚本不执行Git/HTTP网络命令。
