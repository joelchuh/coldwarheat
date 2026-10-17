# Player 载荷构建与核验

description：定位构建所需的数据映射、生成复制和只读验收；不展开游戏规则。

先读 `Docs/Descriptors/player-payload-verification.module.json`，需要运行核验再读 `Tools/Descriptors/verify-player-payloads.script.json`。两个数据集分别为 `workflow.player_payloads` 和 `workflow.player_verification`，从 `Data/catalog.json` 定位。

## 调用流

`DemoBuild` 业务验证 → `PlayerPayloadPublisher.Synchronize` → 从 `Tools/Configuration/player-payloads.json` 加载不可变映射 → 全量路径/大小/源内容预检 → 仅更新有变化的生成文件 → Unity 构建。

`verify-player-payloads.py` → 共享映射与工程策略 → 比对 Player 精确载荷集合 → 逐份源/目标 SHA256 和字节数 → exe/五个项目程序集/日志 → 末尾复核读取的文件状态 → stdout JSON。

调用示例（可信工程终端）：

```text
python -B Tools/Scripts/verify-player-payloads.py --project-root E:/TwilightStruggle --player-directory Builds/PayloadManifestPlaytest --build-log TestResults/player-payload-manifest-build.log
```

成功退出0，失败退出1。脚本不写报告；调用方保存输出。测试入口 `Tools/Tests/verify-player-payloads.tests.py` 会创建并保留独立临时样本，副作用见其描述。

## 数据与边界

清单只记录来源路径/目标文件名；例如本地化源文件可映射为 `names.json`。新增正式载荷只更新这一份清单，仍由现有业务加载器校验规则含义。工程核验策略单独保存文件名、程序集和读取限额，不把这些值散落到每个工作脚本。

清单为封闭schema1，最多1MiB/256项/16层；路径仅允许已批准的数据/契约/第三方声明目录，拒绝重复键、大小写碰撞、绝对路径、设备名与重解析点。C#和Python均拒绝非标准JSON扩展。

生成目录固定为 `Assets/StreamingAssets/ColdWarStruggle`。已有未知文件/目录使复制停止，Unity `.meta` 保留；没有自动删除。全部读取预检先于写入，但写磁盘失败仍可留下部分生成文件，需修复后重新构建。

核验只支持可信本地稳定输入。SHA与成功标记证明所检查的文件内容/标记符合约定，不能独立证明Git提交来源、Unity实际执行过程、游戏规则或UI正确；构建提交由外层工作记录另记。不承诺抵御恶意并发路径替换。

## 验证与维护

实际测试、哈希、构建和审查见 `Docs/Testing/player-payload-verification.validation.json`；工作记录见 `Docs/Worklogs/2026-10-17-player-payload-verification.md`。脚本固化依据为早期牌、太空事件、奥运会三个独立批次分别58/61/66份载荷核验，不把同一批重试当新需求。

按需读取代码：映射语法 → `PlayerPayloadManifest.Load`；生成写边界 → `PlayerPayloadPublisher.Synchronize`；Player证据 → Python `verify`/`FileEvidence`。不需先读取整个游戏工程。
