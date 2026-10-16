# 播放器载荷清单与核验

> description: 构建和只读验收共享文件映射，避免每批从C# Copy语句临时提取；不改变游戏规则。

用户授权自主开发及Git同步，并要求重复>=3次且会复用的稳定流程配套script/JSON描述。独立使用证据：战争、太空事件、奥运会三个Player批次均核对共享载荷字节及程序集SHA；同批重试不单独计数。

采用单源清单：Tools/Configuration/player-payloads.json保存现有66条source/destination；含schema_version=1、dataset_id=workflow.player_payloads、description、entries，不含游戏规则值或动态哈希。清单destination为平面文件名，source仅Data/或Docs/Contracts/、Docs/References/ThirdParty/。源/目标均忽略大小写判重，禁止绝对/UNC/反斜线/空段/点段/冒号/设备名/段尾点空格。字符限定ASCII字母数字、点、下划线、连字符，各段1–128。entries为1–256，JSON最多1MiB、深度16；字段封闭且拒绝重复键/小数schema版本。正式Schema供资料检索；C#及Python按相同封闭契约检查，不承诺通用JSON Schema实现。

Infrastructure新增PlayerPayloadManifest.Load(json)，返回不可变Entries(Source,Destination)。PlayerPayloadPublisher.Synchronize(projectRoot)先读取清单、预检所有源及目标，再复制到固定Assets/StreamingAssets/ColdWarStruggle。源每份<=64MiB，总预读<=256MiB；读前后核对长度/mtime，不允许输入并发变化。所有既有路径组件拒绝reparse/symlink；目标目录缺失可创建，既有未登记载荷拒绝（Unity .meta独立忽略），不删除旧文件。源数据全部读取成功后才开始写；磁盘写失败仍可能留部分生成文件，明确失败且不得构建/验收，重新运行相同清单可重试。只写与源不同的生成目标。DemoBuild保留现有完整业务验证，只替换66条Copy为Publisher调用。清单改变不自动授予任何规则审批。

只读Tools/Scripts/verify-player-payloads.py接受--project-root绝对可信根、--player-directory项目相对子目录、--build-log项目相对日志。从共享清单读取映射，从Tools/Configuration/player-verification.json读取本项目exe/data/StreamingAssets/程序集名及成功标记/文件界限；输出stdout JSON，成功0/错误1，不自动保存、修复、删除、运行Unity/Player/Git或联网。拒绝所有读取路径中的链接/重解析点、越界、缺失/重复/大小写冲突、超限、额外载荷、源/构建字节不一致、缺程序集或日志无成功标记。哈希使用有界流式读取，检查读前后元数据稳定；每份源/目标SHA一致后输出映射和哈希、exe/程序集/log/清单/策略SHA。

日志标记和文件哈希只能证明当前读取文件符合该清单，不能独立证明二进制由某Git提交构建、无恶意代码或游戏正确；报告不猜source_commit。构建调用方记录真实提交、构建过程和实窗证据。明确稳定输入假设，不宣称抵抗恶意并发换链。

核验脚本、测试脚本、清单/策略各有description与catalog登记；Wiki记录调用边界/flow。不增加技能或新的数据库。先做坏输入/路径/缺文件/字节差异真实CLI测试，再实现；使用现有元数据检查。最终全量原生测试、一次整批fresh只读审查、独立Builds/PayloadManifestPlaytest构建，并用新脚本实际核验全部66份；旧OlympicPlaytest保留。无需再重复奥运会游戏路径；只读核验不冒充UI验收。
