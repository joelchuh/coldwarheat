# 审阅基线导出工作流（2026-10-12）

遵守AGENTS22–23，仅在隔离C工作目录准备工作流工具候选；未写E项目、未提交或推送，未实现游戏代码。persistent2、UN、Trap、Junta四个实际基线的HEAD/清单项数经读取核对，269/276/284/295分别对应不同设计批次，满足至少三次稳定复用条件；不把重复测试次数计入复用证据。

新增[导出器和协议](../Wiki/export-review-baseline.md)、导出及测试描述、真实PS/Git测试。显式传ProjectRoot/new Destination/GitExecutable/ExpectedHead，只复制受跟踪普通工作文件字节；双向SHA、HEAD/clean、索引/配置与清单复核后写完整清单。路径/链接/元数据/特殊索引状态拒绝，Git读操作禁用外部全局配置、fsmonitor和可选索引写入。失败产物保留，无目标覆盖、清理、检出、提交或网络命令。

测试先于实现：初次入口以“导出实现尚不存在”失败，随后写实现。首轮30项23通过、7失败：4个junction创建被沙箱拒绝，1个保留名fixture被Git自己的protectNTFS先拦截，锁定文件和并发修改两项则比预期更早触发clean检查。核对实际Git输出后，将锁定放到已观察开始标记之后，明确并发脏状态的预期错误，合成索引仅在fixture通过core.protectNTFS=false创建。经允许的C-only链接测试运行，7项修正场景全部通过。BOM配置include追加边界测试也通过，未为它改生产实现。

完成31项整轮后，新增外部excludes/attributes边界，先观察到外部excludes隐藏源未跟踪文件导致误通过的回归失败；生产调用将两个core文件路径强制指向空设备，并禁用系统attributes。最后完整运行33项，全部通过、0失败，PowerShell 7.6.5；详情见[验证报告](../Design/export-review-baseline.validation.json)。真实Git fixture创建、提交仅发生在新临时目录，源和Git字节不变断言覆盖成功及可比较的失败场景。生产脚本无Git写入。并发/复制失败保留无成功清单的半成品。所有临时产物保留；junction测试需要沙箱外许可。

这些是工具自测，不是独立完成稿审阅，不增加游戏规则检查数。独立审阅、项目目录登记/集成和发布仍待外层流程处理。未重跑未改变的38项快照工具，也未运行C#/Unity。其他操作系统未实测；检查不是原子快照；不支持worktree/gitdir文件、bare、子模块、过滤器/配置include等明确限制见完整描述。

独立审阅随后确认P1：普通`.git`目录仍可通过`commondir`读取未检查的common/config，执行其clean filter；原33项不能覆盖该入口。新增真实Git回归先于修复：默认沙箱阻断filter，表现为SOURCE_NOT_CLEAN；在许可的新C-only fixture中复现filter sentinel出现且导出成功清单产生，明确证明边界破坏。仅增加`.git/commondir`存在时的预检拒绝，位置在任何Git子进程之前，返回UNSUPPORTED_REPOSITORY；不解析或追随目标，不扩展支持共享工作树。新增测试同时断言filter未执行、源与common元数据不变、目标未创建。修后完整34项全部通过、0失败；两个脚本新SHA记录在验证报告。描述和Wiki已更新；独立审阅者复查仍待外层流程安排，此处不自称审阅关闭。

完成稿由同一独立审阅者复查为ReadyYes，无未处理Critical/Important/Minor。审阅者此前独立通过原33项，本轮独立运行新commondir回归1项并重跑原绕过探针，均确认拒绝且未执行filter、未写目标、源和common字节未变；没有声称本轮再跑完整34项。主验完整34项通过，两个脚本哈希及描述路径复核通过。

本次在136a538规则基线上整合4项正式工具目录、Wiki索引与工作流入口，共10个文件；核对65个候选相对链接。工具报告新增统一new/previous/total计数字段供只读快照核验，计数仍仅代表34项工具测试；游戏设计保持1884项离线检查、71 Bound/31 Unresolved、102个处理器planned。未重跑未改变的38项旧工具测试或游戏设计模型，没有C#/Unity实现。普通提交后核对远程main和本地HEAD。
