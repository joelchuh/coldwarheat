# 首个 Unity 影响力 Demo

description：用户批准整批编码后的实际工程、审查修复、运行验证及交付边界。

## 授权与实现

用户在认可单人操作双方、允许查看双方信息的方向后，要求“先看看整批完成后的第一个demo”，已满足编码前沟通节点。随后要求继续优化到剩余额度 10%。本批创建 Unity 工程，完成已批准首批核心计划，并增加可点击的诊断界面及 Windows 构建。

工作分支 `feature/first-influence-demo`，起点 `23314ef877630783c0c76baad9de01cea0e907ce`。使用用户指定的 `E:\TwilightStruggle` 干净目录；当前聊天工作区在 C 盘，原生 worktree 工具无法指定此外部仓库，因此采用原目录独立分支，没有冒称已隔离物理工作区。

已实现 Core 不可变状态/地图、普通影响力规划、Application 固定身份/幂等/版本/投影、Infrastructure JSON 校验、Presentation 控制器和 IMGUI 诊断界面。每个逻辑模块有 JSON 描述，数据有独立描述并登记目录。没有新增通用 Git 或自动工作流 CLI 脚本；Editor 构建模块是必要产品工程入口，副作用和边界见其描述。

## 环境与验证

- 编辑器：Unity 6000.6.0f1 (`f7f8ed4d1e24`)；C# 9 编译实际成功。
- Test Framework 1.8.0；Newtonsoft JSON 包 3.2.2；NUnit 2.1.0。准确解析值保存在 Packages/packages-lock.json。
- Core/Application/Infrastructure 编译依赖已独立检查，不含 UnityEngine。未引入 URP、联网、数据库或未用插件；画风仍可更换。

| 阶段 | 实際观察到的 RED | GREEN |
|---|---|---|
| 定义与地图 | 21 项未实现失败 | 21/21 |
| 影响力规则 | 12 项针对规则失败 | 合计 33/33 |
| 会话与投影 | 8 项针对会话失败 | 合计 41/41 |
| 演示数据与费用注入 | 原 41 通过、新 3 失败 | 44/44 |
| UI 控制器 | 3 项未实现失败 | 合计 47/47 |
| 独立审查回归 | 50 项中 47 通过，缺拓扑、替换国家、缺构建回调 3 项失败 | 修复及增加 profile/副本测试后 52/52 |

最后完整报告：`TestResults/first-demo/review-green.xml`，2026-10-13 08:44:39 UTC，52 total / 52 passed / 0 failed。所有日志和 XML 是本地忽略的验证产物；精简结构化记录见 [first-demo.validation.json](../Testing/first-demo.validation.json)。

首次沙箱 Unity 启动遇到授权服务崩溃；改用获准的原生批处理后成功。依赖程序集名和一个 C# 名称遮蔽编译错误已修复。这些环境/编译失败没有计为规则 RED 或测试通过。

## 独立审阅

独立 reviewer 用全新上下文审查全部源码与真实 XML，发现两项 P2：只按 84 个国家数量判断地图完整性、标准 Unity Build 可能绕过数据刷新。均先复现失败再修复，并经同一 reviewer 复审通过。

1. 独立批准 profile 锁定已审核地图 0.1.0，递归排序对象键后以紧凑 UTF-8 JSON 算 SHA256。删光邻接、等量替换 Canada 均拒绝。profile 不复制业务事实，构建不自动改批准值；这不是签名安全机制，也不是完整规则包审核。
2. `IPreprocessBuildWithReport` 接入校验及六文件复制，测试模拟过期和缺失副本。Bootstrap 的编辑器与播放器路径均读取相同批准 profile。

另补充创建新场景前保护未保存场景。小型 DTO 按职责聚合，投影用例并入 SessionTests；没有为了匹配计划示例而拆出空文件。

## 构建与窗口验收

Windows 构建输出：`Builds/FirstDemo/ColdWarStruggleDemo.exe`。初次构建及修复后构建日志分别为 `build.log`、`build-reviewed.log`。构建生成实际程序集及依赖，输出目录不入 Git。

computer-use 初次应用访问授权超时，用户随后确认允许。Demo 已启动且进程 Responding，Player.log 未出现业务异常；但工具无法激活窗口，返回 `failed to activate captured window`，捕获内容不是游戏界面。已停止使用失效坐标；鼠标操作/视觉验收尚未完成，不宣称通过。修复构建前仅关闭了路径匹配的本任务旧 Demo 进程。

## 范围与后续

两个练习都是人工局面；一个窗口、美苏两个固定身份、同一权威状态。国家以分区卡片显示；示例手牌标签不可出牌。102 个卡牌处理器仍 planned，完整回合、初始布置、随机、计分胜负及正式 IMapView 未接入。

后续优先实现已设计的事件选择/影响力效果，并继续改善演示操作和地图展示边界。最终画风由用户后续决定。Git 的本地提交与远程 SHA 在执行后另行核验，不从插件连接状态推断已推送。

Unity自动序列化的.meta/.unity/.asset空字段保留原格式；.gitattributes只对这三类文件排除行尾空格告警，手写C#/JSON/文档仍正常检查。暂存差异检查通过，六份打包JSON与权威源逐字节一致，描述必要字段及真实路径/符号检查通过。
